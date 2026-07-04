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
/// シーン起動時に9人パーティと敵勢力を生成してバトルを開始する。
/// BattleManager・TurnManager 等がなければ自動生成する。
/// </summary>
public class BattleBootstrap : MonoBehaviour
{
    void Start()
    {
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
    // 味方9人（PDF設定準拠）
    // ================================================================
    private List<BattleUnit> CreateAllies()
    {
        var list = new List<BattleUnit>();

        // ---- 前衛4人 ----
        list.Add(MakeUnit("主人公 ケイ", CharacterType.Protagonist, UltimateType.TimeStop,
            "時間断絶", hp:130, mp:70, atk:36, def:20, spd:25, mpCost:30,
            isEnemy:false, SpriteShape.Human, new Color(0.35f, 0.55f, 1f),
            pos: new Vector3(-7.4f, -1.1f, 0), scale: new Vector3(1.2f, 1.8f, 1f)));

        list.Add(MakeUnit("ヒロイン アイ", CharacterType.Heroine, UltimateType.MemoryReplay,
            "記憶回廊", hp:100, mp:90, atk:32, def:15, spd:30, mpCost:35,
            isEnemy:false, SpriteShape.Human, new Color(1f, 0.6f, 0.8f),
            pos: new Vector3(-5.7f, -1.1f, 0), scale: new Vector3(1.15f, 1.75f, 1f)));

        list.Add(MakeUnit("親友 リュウ", CharacterType.BestFriend, UltimateType.SpaceRift,
            "空間斬", hp:115, mp:55, atk:42, def:24, spd:20, mpCost:25,
            isEnemy:false, SpriteShape.Human, new Color(0.7f, 0.75f, 0.85f),
            pos: new Vector3(-4.0f, -1.1f, 0), scale: new Vector3(1.2f, 1.85f, 1f)));

        list.Add(MakeUnit("ショタ ニーヨ", CharacterType.Shota, UltimateType.EventRewrite,
            "事象書換", hp:85, mp:65, atk:30, def:12, spd:33, mpCost:30,
            isEnemy:false, SpriteShape.Human, new Color(1f, 0.85f, 0.4f),
            pos: new Vector3(-2.3f, -1.2f, 0), scale: new Vector3(1.0f, 1.45f, 1f)));

        // ---- 後衛5人 ----
        list.Add(MakeUnit("次元の巫女 レイ", CharacterType.Witch, UltimateType.DimensionCall,
            "次元干渉", hp:90, mp:100, atk:34, def:14, spd:22, mpCost:40,
            isEnemy:false, SpriteShape.Human, new Color(0.75f, 0.5f, 1f),
            pos: new Vector3(-8.0f, 0.8f, 0), scale: new Vector3(0.9f, 1.35f, 1f)));

        list.Add(MakeUnit("ギャル ルナ", CharacterType.Gal, UltimateType.AbyssCall,
            "本体召喚", hp:95, mp:85, atk:33, def:16, spd:28, mpCost:40,
            isEnemy:false, SpriteShape.Human, new Color(1f, 0.8f, 0.3f),
            pos: new Vector3(-6.5f, 0.8f, 0), scale: new Vector3(0.9f, 1.35f, 1f)));

        list.Add(MakeUnit("アンドロイド ミサ", CharacterType.Android, UltimateType.SatelliteBeam,
            "衛星通信", hp:110, mp:60, atk:38, def:22, spd:18, mpCost:35,
            isEnemy:false, SpriteShape.Android, new Color(0.4f, 0.95f, 0.9f),
            pos: new Vector3(-5.0f, 0.8f, 0), scale: new Vector3(0.85f, 1.25f, 1f)));

        list.Add(MakeUnit("幽霊犬 ポチ", CharacterType.Ghost, UltimateType.GhostRush,
            "突撃", hp:75, mp:50, atk:28, def:10, spd:38, mpCost:20,
            isEnemy:false, SpriteShape.Dog, new Color(0.85f, 0.9f, 1f),
            pos: new Vector3(-3.5f, 0.65f, 0), scale: new Vector3(1.0f, 0.7f, 1f)));

        list.Add(MakeUnit("王女 エリカ", CharacterType.Princess, UltimateType.Payoff,
            "ペイオフ", hp:100, mp:75, atk:31, def:18, spd:24, mpCost:35,
            isEnemy:false, SpriteShape.Human, new Color(1f, 0.72f, 0.2f),
            pos: new Vector3(-2.0f, 0.8f, 0), scale: new Vector3(0.9f, 1.35f, 1f)));

        return list;
    }

    // ================================================================
    // 敵勢力：厄災のカナタ（ボス）+ 戦闘用アンドロイド×2
    // ================================================================
    private List<BattleUnit> CreateEnemies()
    {
        var list = new List<BattleUnit>();

        list.Add(MakeUnit("戦闘用アンドロイドα", CharacterType.Android, UltimateType.SatelliteBeam,
            "自爆特攻", hp:120, mp:30, atk:26, def:14, spd:20, mpCost:99,
            isEnemy:true, SpriteShape.Spiky, new Color(0.6f, 0.35f, 0.35f),
            pos: new Vector3(2.6f, -0.5f, 0), scale: new Vector3(1.3f, 1.7f, 1f)));

        // ボス（CharacterType.Witch を厄災判定に使用）
        list.Add(MakeUnit("厄災のカナタ", CharacterType.Witch, UltimateType.AbyssCall,
            "厄災", hp:420, mp:999, atk:34, def:22, spd:26, mpCost:0,
            isEnemy:true, SpriteShape.Boss, new Color(0.75f, 0.15f, 0.25f),
            pos: new Vector3(5.0f, 0.9f, 0), scale: new Vector3(2.0f, 2.7f, 1f)));

        list.Add(MakeUnit("戦闘用アンドロイドβ", CharacterType.Android, UltimateType.SatelliteBeam,
            "自爆特攻", hp:120, mp:30, atk:26, def:14, spd:19, mpCost:99,
            isEnemy:true, SpriteShape.Spiky, new Color(0.6f, 0.35f, 0.35f),
            pos: new Vector3(7.4f, -0.5f, 0), scale: new Vector3(1.3f, 1.7f, 1f)));

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
