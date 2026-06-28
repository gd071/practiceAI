using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーン起動時にテスト用キャラクターを生成してバトルを開始する。
/// BattleManager・TurnManager がなければ自動生成する。
/// </summary>
public class BattleBootstrap : MonoBehaviour
{
    void Start()
    {
        EnsureManagers();
        EnsureCamera();

        var allies  = CreateAllies();
        var enemies = CreateEnemies();

        PositionUnits(allies,  startX: -5f, y: -1.5f, spacing: 2f);
        PositionUnits(enemies, startX:  3f, y:  1.5f, spacing: 2.5f);

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
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private List<BattleUnit> CreateAllies()
    {
        var list = new List<BattleUnit>();
        list.Add(MakeUnit("主人公 ケイ",   CharacterType.Protagonist, UltimateType.TimeStop,
            "時間断絶",   hp:120, mp:60, atk:35, def:20, spd:25, mpCost:30,
            isEnemy:false, color: new Color(0.4f, 0.6f, 1f)));
        list.Add(MakeUnit("ヒロイン アイ", CharacterType.Heroine,     UltimateType.MemoryReplay,
            "記憶回廊",   hp: 90, mp:80, atk:30, def:15, spd:30, mpCost:35,
            isEnemy:false, color: new Color(1f, 0.6f, 0.8f)));
        list.Add(MakeUnit("親友 リュウ",   CharacterType.BestFriend,  UltimateType.SpaceRift,
            "空間斬",     hp:100, mp:50, atk:40, def:25, spd:20, mpCost:25,
            isEnemy:false, color: new Color(0.4f, 0.9f, 0.4f)));
        return list;
    }

    private List<BattleUnit> CreateEnemies()
    {
        var list = new List<BattleUnit>();
        list.Add(MakeUnit("歪んだ影",   CharacterType.Protagonist, UltimateType.AbyssCall,
            "虚無の吸引", hp:150, mp:40, atk:30, def:18, spd:22, mpCost:40,
            isEnemy:true, color: new Color(0.7f, 0.2f, 0.2f)));
        list.Add(MakeUnit("記憶の残滓", CharacterType.Ghost,       UltimateType.GhostRush,
            "記憶消去",   hp: 80, mp:30, atk:25, def:10, spd:35, mpCost:20,
            isEnemy:true, color: new Color(0.5f, 0.3f, 0.7f)));
        return list;
    }

    private BattleUnit MakeUnit(
        string unitName, CharacterType ctype, UltimateType utype, string ultimateName,
        int hp, int mp, int atk, int def, int spd, int mpCost,
        bool isEnemy, Color color)
    {
        var go = new GameObject(unitName);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeCharacterSprite(isEnemy);
        sr.color        = color;
        sr.sortingOrder = isEnemy ? 1 : 2;
        go.transform.localScale = isEnemy
            ? new Vector3(1.4f, 1.8f, 1f)
            : new Vector3(1.2f, 1.8f, 1f);

        // アウトライン（外枠）
        AddOutline(go, color, isEnemy);

        // 名前ラベル（ワールド空間）
        AddNameLabel(go, unitName, isEnemy);

        var data = ScriptableObject.CreateInstance<CharacterData>();
        data.characterName        = unitName;
        data.characterType        = ctype;
        data.maxHP                = hp;
        data.maxMP                = mp;
        data.atk                  = atk;
        data.def                  = def;
        data.spd                  = spd;
        data.ultimateName         = ultimateName;
        data.ultimateType         = utype;
        data.ultimateMPCost       = mpCost;
        data.ultimateDescription  = unitName + "の必殺技";

        var unit = go.AddComponent<BattleUnit>();
        unit.Initialize(data, isEnemy);
        return unit;
    }

    private void PositionUnits(List<BattleUnit> units, float startX, float y, float spacing)
    {
        for (int i = 0; i < units.Count; i++)
            units[i].transform.position = new Vector3(startX + i * spacing, y, 0);
    }

    // 人型シルエット風スプライト（頭部+胴体を模した形）
    private static Sprite MakeCharacterSprite(bool isEnemy)
    {
        int w = 64, h = 96;
        var tex    = new Texture2D(w, h);
        var pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;   // 0..1
                float ny = y / (float)h;   // 0..1

                bool inBody = nx > 0.15f && nx < 0.85f && ny < 0.72f;
                bool inHead = false;

                if (!isEnemy)
                {
                    // 味方：丸頭（上部中央に円）
                    float cx = 0.5f, cy = 0.86f, r = 0.2f;
                    float dx = nx - cx, dy = ny - cy;
                    inHead = dx * dx + dy * dy < r * r;
                }
                else
                {
                    // 敵：尖った頭（三角形）
                    float tipX = 0.5f, tipY = 1.0f;
                    float baseY = 0.72f;
                    if (ny > baseY)
                    {
                        float ratio = (ny - baseY) / (tipY - baseY);
                        float halfW = 0.22f * (1f - ratio);
                        inHead = nx > tipX - halfW && nx < tipX + halfW;
                    }
                }

                pixels[y * w + x] = (inBody || inHead) ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
    }

    // キャラクターのアウトライン（影つき枠）
    private static void AddOutline(GameObject parent, Color baseColor, bool isEnemy)
    {
        var go = new GameObject("Outline");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(0, 0, 0.01f);
        go.transform.localScale    = new Vector3(1.08f, 1.05f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeWhiteRect(64, 96);
        sr.color        = new Color(baseColor.r * 0.4f, baseColor.g * 0.4f, baseColor.b * 0.4f, 0.4f);
        sr.sortingOrder = isEnemy ? 0 : 1;
    }

    // 名前ラベル（キャラ足元）
    private static void AddNameLabel(GameObject parent, string unitName, bool isEnemy)
    {
        var go = new GameObject("NameLabel");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(0, -0.72f, 0);
        // 親スケール(1.2~1.4)を打ち消したうえで表示サイズを小さくする
        float parentScale = parent.transform.localScale.x;
        go.transform.localScale = Vector3.one * (0.16f / parentScale);

        var tm = go.AddComponent<TextMesh>();
        tm.text          = unitName;
        tm.fontSize      = 60;
        tm.characterSize = 0.1f;
        tm.color         = isEnemy ? new Color(1f, 0.55f, 0.55f) : new Color(0.65f, 0.85f, 1f);
        tm.anchor        = TextAnchor.UpperCenter;
        tm.alignment     = TextAlignment.Center;
    }

    private static Sprite MakeWhiteRect(int w, int h)
    {
        var tex    = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite MakeWhiteSquare()
    {
        return MakeWhiteRect(64, 64);
    }
}
