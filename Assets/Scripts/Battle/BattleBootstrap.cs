using System.Collections.Generic;
using UnityEngine;

/// <summary>キャラクターの見た目バリエーション。</summary>
public enum SpriteShape
{
    Human,    // 丸頭の人型
    Dog,      // 犬（低くて横長・耳つき）
    Android,  // アンドロイド（アンテナつき）
    Spiky,    // 敵アンドロイド（尖った頭）
    Boss      // ボス（角つき・大型）
}

/// <summary>
/// シーン起動時のモード分岐ルーター。
/// Field モードなら 3D フィールドを、Battle モードなら現在のパーティと
/// 章に応じた敵編成でバトルを構築する。
/// </summary>
public class BattleBootstrap : MonoBehaviour
{
    void Start()
    {
        CharacterDB.EnsureBuilt();

        // ---- フィールドモード ----
        if (StoryManager.Mode == GameMode.Field)
        {
            if (GameManager.Instance == null)
            {
                var g = new GameObject("_GameManager");
                g.AddComponent<GameManager>();
            }
            if (GetComponent<FieldBootstrap>() == null)
                gameObject.AddComponent<FieldBootstrap>();
            return;
        }

        // ---- バトルモード ----
        EnsureManagers();
        EnsureCamera();

        var allies  = CreateAllies();
        var enemies = CreateEnemies();

        BattleManager.Instance.StartBattle(allies, enemies);
    }

    private void EnsureManagers()
    {
        if (BattleManager.Instance == null)
        {
            var go = new GameObject("_BattleManager");
            var bm = go.AddComponent<BattleManager>();
            bm.turnManager = go.AddComponent<TurnManager>();
        }
        else if (BattleManager.Instance.turnManager == null)
        {
            BattleManager.Instance.turnManager =
                BattleManager.Instance.gameObject.AddComponent<TurnManager>();
        }

        if (GameManager.Instance == null)
        {
            var go = new GameObject("_GameManager");
            go.AddComponent<GameManager>();
        }

        if (GetComponent<BattleEffectManager>() == null)
            gameObject.AddComponent<BattleEffectManager>();

        if (GetComponent<UIEffectManager>() == null)
            gameObject.AddComponent<UIEffectManager>();

        if (GetComponent<BattleVisuals>() == null)
            gameObject.AddComponent<BattleVisuals>();
    }

    private void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10f);
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.09f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    // ================================================================
    // 味方：StoryManager のパーティ構成から生成（前衛4＋後衛）
    // ================================================================
    private List<BattleUnit> CreateAllies()
    {
        var list = new List<BattleUnit>();
        int blessings = StoryManager.Blessings.Count;

        var frontX = new float[] { -7.4f, -5.7f, -4.0f, -2.3f };
        var backX  = new float[] { -8.0f, -6.5f, -5.0f, -3.5f, -2.0f };

        int fi = 0, bi = 0;
        foreach (var id in StoryManager.PartyIds)
        {
            var def = CharacterDB.Get(id);
            if (def == null) continue;

            Vector3 pos; Vector3 scale;
            if (fi < 4)
            {
                pos   = new Vector3(frontX[fi], def.shape == SpriteShape.Dog ? -1.45f : -1.1f, 0);
                scale = def.shape == SpriteShape.Dog
                    ? new Vector3(1.2f, 0.85f, 1f) : new Vector3(1.2f, 1.8f, 1f);
                fi++;
            }
            else
            {
                float x = backX[Mathf.Min(bi, backX.Length - 1)];
                pos   = new Vector3(x, def.shape == SpriteShape.Dog ? 0.65f : 0.8f, 0);
                scale = def.shape == SpriteShape.Dog
                    ? new Vector3(1.0f, 0.7f, 1f) : new Vector3(0.9f, 1.35f, 1f);
                bi++;
            }

            list.Add(MakeUnitFromDef(def, blessings, pos, scale));
        }
        return list;
    }

    private BattleUnit MakeUnitFromDef(CharacterDef def, int blessings, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(def.name);
        go.transform.position   = pos;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeCharacterSprite(def.shape);
        sr.color        = def.color;
        sr.sortingOrder = 2;

        AddOutline(go, def.color, false, def.shape);
        AddNameLabel(go, def.name, false);

        var unit = go.AddComponent<BattleUnit>();
        unit.Initialize(CharacterDB.CreateData(def.id, blessings), false);
        return unit;
    }

    // ================================================================
    // 敵編成：章のバトルIDに応じて変わる
    // ================================================================
    private List<BattleUnit> CreateEnemies()
    {
        var list = new List<BattleUnit>();

        switch (StoryManager.PendingBattle)
        {
            // 第2章：戦闘用アンドロイド×2
            case BattleId.Androids:
                list.Add(MakeUnit("戦闘用アンドロイドα", CharacterType.Android, UltimateType.SatelliteBeam,
                    "自爆特攻", hp:110, mp:30, atk:22, def:12, spd:20, mpCost:99,
                    isEnemy:true, SpriteShape.Spiky, new Color(0.6f, 0.35f, 0.35f),
                    pos: new Vector3(3.2f, -0.5f, 0), scale: new Vector3(1.3f, 1.7f, 1f)));
                list.Add(MakeUnit("戦闘用アンドロイドβ", CharacterType.Android, UltimateType.SatelliteBeam,
                    "自爆特攻", hp:110, mp:30, atk:22, def:12, spd:19, mpCost:99,
                    isEnemy:true, SpriteShape.Spiky, new Color(0.6f, 0.35f, 0.35f),
                    pos: new Vector3(6.4f, -0.5f, 0), scale: new Vector3(1.3f, 1.7f, 1f)));
                break;

            // 第5章：深淵のギャル・ルナ（中ボス）
            case BattleId.Runa:
                list.Add(MakeUnit("深淵のルナ", CharacterType.Gal, UltimateType.AbyssCall,
                    "深淵の視線", hp:280, mp:99, atk:28, def:16, spd:28, mpCost:0,
                    isEnemy:true, SpriteShape.Human, new Color(0.85f, 0.7f, 0.2f),
                    pos: new Vector3(4.8f, -0.2f, 0), scale: new Vector3(1.5f, 2.1f, 1f)));
                break;

            // 第8章：洗脳された親友リュウ
            case BattleId.Ryu:
                list.Add(MakeUnit("洗脳されたリュウ", CharacterType.BestFriend, UltimateType.SpaceRift,
                    "虚ろな空間斬", hp:340, mp:99, atk:36, def:22, spd:24, mpCost:0,
                    isEnemy:true, SpriteShape.Human, new Color(0.45f, 0.45f, 0.6f),
                    pos: new Vector3(4.8f, -0.2f, 0), scale: new Vector3(1.5f, 2.2f, 1f)));
                break;

            // 想定外の遷移（BattleId.None）ならフィールドへ帰す
            case BattleId.None:
                Debug.LogWarning("[BattleBootstrap] PendingBattle が None のままバトルが開始された。フィールドへ戻る。");
                StoryManager.ReturnToField();
                break;

            // 第11章 Xデー：厄災のカナタ＋護衛
            default:
                list.Add(MakeUnit("戦闘用アンドロイドα", CharacterType.Android, UltimateType.SatelliteBeam,
                    "自爆特攻", hp:120, mp:30, atk:26, def:14, spd:20, mpCost:99,
                    isEnemy:true, SpriteShape.Spiky, new Color(0.6f, 0.35f, 0.35f),
                    pos: new Vector3(2.6f, -0.5f, 0), scale: new Vector3(1.3f, 1.7f, 1f)));
                list.Add(MakeUnit("厄災のカナタ", CharacterType.Witch, UltimateType.AbyssCall,
                    "厄災", hp:450, mp:999, atk:34, def:22, spd:26, mpCost:0,
                    isEnemy:true, SpriteShape.Boss, new Color(0.75f, 0.15f, 0.25f),
                    pos: new Vector3(5.0f, 0.9f, 0), scale: new Vector3(2.0f, 2.7f, 1f)));
                list.Add(MakeUnit("戦闘用アンドロイドβ", CharacterType.Android, UltimateType.SatelliteBeam,
                    "自爆特攻", hp:120, mp:30, atk:26, def:14, spd:19, mpCost:99,
                    isEnemy:true, SpriteShape.Spiky, new Color(0.6f, 0.35f, 0.35f),
                    pos: new Vector3(7.4f, -0.5f, 0), scale: new Vector3(1.3f, 1.7f, 1f)));
                break;
        }

        return list;
    }

    // ================================================================
    // ユニット生成
    // ================================================================
    private BattleUnit MakeUnit(
        string unitName, CharacterType ctype, UltimateType utype, string ultimateName,
        int hp, int mp, int atk, int def, int spd, int mpCost,
        bool isEnemy, SpriteShape shape, Color color,
        Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(unitName);
        go.transform.position   = pos;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeCharacterSprite(shape);
        sr.color        = color;
        sr.sortingOrder = isEnemy ? 1 : 2;

        AddOutline(go, color, isEnemy, shape);
        AddNameLabel(go, unitName, isEnemy);

        var data = ScriptableObject.CreateInstance<CharacterData>();
        data.characterName       = unitName;
        data.characterType       = ctype;
        data.maxHP               = hp;
        data.maxMP               = mp;
        data.atk                 = atk;
        data.def                 = def;
        data.spd                 = spd;
        data.ultimateName        = ultimateName;
        data.ultimateType        = utype;
        data.ultimateMPCost      = mpCost;
        data.ultimateDescription = unitName + "の必殺技";

        var unit = go.AddComponent<BattleUnit>();
        unit.Initialize(data, isEnemy);
        return unit;
    }

    // ================================================================
    // 形状バリエーション付きスプライト生成
    // ================================================================
    private static readonly Dictionary<SpriteShape, Sprite> _spriteCache =
        new Dictionary<SpriteShape, Sprite>();

    private static Sprite MakeCharacterSprite(SpriteShape shape)
    {
        if (_spriteCache.TryGetValue(shape, out var cached) && cached != null)
            return cached;

        int w = 64, h = 96;
        var tex    = new Texture2D(w, h);
        var pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;
                pixels[y * w + x] = InShape(shape, nx, ny) ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
        _spriteCache[shape] = sprite;
        return sprite;
    }

    private static bool InShape(SpriteShape shape, float nx, float ny)
    {
        switch (shape)
        {
            case SpriteShape.Human:
            {
                bool body = nx > 0.18f && nx < 0.82f && ny < 0.70f;
                float dx = nx - 0.5f, dy = ny - 0.85f;
                bool head = dx * dx + dy * dy < 0.20f * 0.20f;
                return body || head;
            }

            case SpriteShape.Dog:
            {
                // 低い胴体 + 丸頭 + 三角耳
                bool body = nx > 0.05f && nx < 0.80f && ny > 0.05f && ny < 0.55f;
                float dx = nx - 0.78f, dy = ny - 0.62f;
                bool head = dx * dx + dy * dy < 0.19f * 0.19f;
                bool ear  = ny > 0.70f && ny < 0.95f
                         && Mathf.Abs(nx - 0.70f) < 0.06f * (0.95f - ny) / 0.25f + 0.02f;
                bool ear2 = ny > 0.70f && ny < 0.95f
                         && Mathf.Abs(nx - 0.88f) < 0.06f * (0.95f - ny) / 0.25f + 0.02f;
                bool tail = nx < 0.10f && ny > 0.45f && ny < 0.75f
                         && Mathf.Abs(nx - 0.06f) < 0.04f;
                return body || head || ear || ear2 || tail;
            }

            case SpriteShape.Android:
            {
                bool body = nx > 0.20f && nx < 0.80f && ny < 0.68f;
                // 角ばった頭
                bool head = nx > 0.30f && nx < 0.70f && ny > 0.68f && ny < 0.92f;
                // アンテナ
                bool antenna = Mathf.Abs(nx - 0.5f) < 0.03f && ny >= 0.92f;
                return body || head || antenna;
            }

            case SpriteShape.Spiky:
            {
                bool body = nx > 0.15f && nx < 0.85f && ny < 0.72f;
                if (ny > 0.72f)
                {
                    float ratio = (ny - 0.72f) / 0.28f;
                    float halfW = 0.22f * (1f - ratio);
                    return nx > 0.5f - halfW && nx < 0.5f + halfW;
                }
                return body;
            }

            case SpriteShape.Boss:
            {
                bool body = nx > 0.12f && nx < 0.88f && ny < 0.66f;
                float dx = nx - 0.5f, dy = ny - 0.78f;
                bool head = dx * dx + dy * dy < 0.22f * 0.22f;
                // 左右の角
                bool hornL = ny > 0.80f && ny < 1.0f
                          && Mathf.Abs(nx - (0.30f - (ny - 0.80f) * 0.4f)) < 0.045f;
                bool hornR = ny > 0.80f && ny < 1.0f
                          && Mathf.Abs(nx - (0.70f + (ny - 0.80f) * 0.4f)) < 0.045f;
                return body || head || hornL || hornR;
            }
        }
        return false;
    }

    // ================================================================
    // アウトライン・名前ラベル
    // ================================================================
    private static void AddOutline(GameObject parent, Color baseColor, bool isEnemy, SpriteShape shape)
    {
        var go = new GameObject("Outline");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(0, 0, 0.01f);
        go.transform.localScale    = new Vector3(1.08f, 1.05f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeCharacterSprite(shape);
        sr.color        = new Color(baseColor.r * 0.4f, baseColor.g * 0.4f, baseColor.b * 0.4f, 0.4f);
        sr.sortingOrder = isEnemy ? 0 : 1;
    }

    private static void AddNameLabel(GameObject parent, string unitName, bool isEnemy)
    {
        var go = new GameObject("NameLabel");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(0, -0.72f, 0);
        float parentScaleX = parent.transform.localScale.x;
        float parentScaleY = parent.transform.localScale.y;
        go.transform.localScale = new Vector3(0.16f / parentScaleX, 0.16f / parentScaleY, 1f);

        var tm = go.AddComponent<TextMesh>();
        tm.text          = unitName;
        tm.fontSize      = 60;
        tm.characterSize = 0.1f;
        tm.color         = isEnemy ? new Color(1f, 0.55f, 0.55f) : new Color(0.65f, 0.85f, 1f);
        tm.anchor        = TextAnchor.UpperCenter;
        tm.alignment     = TextAlignment.Center;
    }
}
