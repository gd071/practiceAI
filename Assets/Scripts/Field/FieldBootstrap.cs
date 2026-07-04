using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// フィールドシーンを全てコードで構築する。
/// 3D背景（学園都市：学校・神社・商店街・時計塔・公園・浜辺）＋ドット絵キャラ。
/// HD-2D風：パースペクティブカメラを斜め見下ろしに配置する。
/// </summary>
public class FieldBootstrap : MonoBehaviour
{
    private PlayerController _player;
    private Text _objectiveText;
    private Text _interactText;
    private Text _partyText;

    private static Shader _litShader;

    // ================================================================
    void Start()
    {
        EnsureEventSystem();
        BuildLighting();
        var cam = BuildCamera();
        BuildTown();
        BuildSkyMeteor();
        BuildAmbientNpcs();

        // プレイヤー
        var pgo = new GameObject("Player");
        pgo.transform.position = new Vector3(0, 0.02f, -6f);
        _player = pgo.AddComponent<PlayerController>();
        _player.Setup(cam);

        // 会話・ミニゲーム
        if (GetComponent<DialogueUI>() == null)      gameObject.AddComponent<DialogueUI>();
        if (GetComponent<MinigameManager>() == null) gameObject.AddComponent<MinigameManager>();

        BuildHud();

        // 章に応じたイベントポイント配置
        StoryEvents.Setup(_player, this);
        StoryEvents.BuildEventPoints();
    }

    void Update()
    {
        // 目的表示
        if (_objectiveText != null)
            _objectiveText.text = $"◆ {BattleManager.LoopNumber}周目　" + StoryEvents.CurrentObjective();

        // インタラクトプロンプト
        if (_interactText != null)
        {
            var ep = (_player != null && !DialogueUI.IsPlaying && !MinigameManager.IsPlaying)
                ? _player.NearestPoint() : null;
            _interactText.text = (ep != null && !ep.autoTrigger)
                ? $"［E］{ep.label}" : "";
        }

        // パーティ表示
        if (_partyText != null)
        {
            var sb = new System.Text.StringBuilder("パーティ：");
            foreach (var id in StoryManager.PartyIds)
            {
                var def = CharacterDB.Get(id);
                if (def != null) sb.Append(def.name.Split(' ')[1] + "　");
            }
            _partyText.text = sb.ToString();
        }
    }

    public PlayerController Player => _player;

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
    }

    // ================================================================
    // ライト・カメラ
    // ================================================================
    private void BuildLighting()
    {
        var lgo = new GameObject("Sun");
        var light = lgo.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1.15f;
        light.color     = new Color(1f, 0.95f, 0.85f);
        lgo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.55f);
    }

    private Camera BuildCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }
        cam.orthographic    = false;   // 3D背景のためパースペクティブ
        cam.fieldOfView     = 50f;
        cam.backgroundColor = new Color(0.55f, 0.68f, 0.85f);   // 春の空
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 9.5f, -15f);
        cam.transform.rotation = Quaternion.Euler(42f, 0, 0);
        return cam;
    }

    // ================================================================
    // 町の構築
    // ================================================================
    private void BuildTown()
    {
        // 地面（芝生）
        MakeBox("Ground", new Vector3(0, -0.5f, 0), new Vector3(80, 1, 80),
            new Color(0.42f, 0.58f, 0.36f));

        // 道路（十字）
        MakeBox("Road_NS", new Vector3(0, 0.01f, 0),  new Vector3(5, 0.02f, 80),
            new Color(0.55f, 0.53f, 0.5f));
        MakeBox("Road_EW", new Vector3(0, 0.01f, 0),  new Vector3(80, 0.02f, 5),
            new Color(0.55f, 0.53f, 0.5f));

        // ---- 学校（北） ----
        MakeBuilding(new Vector3(0, 0, 22),  new Vector3(16, 6, 8),
            new Color(0.85f, 0.82f, 0.75f), new Color(0.4f, 0.45f, 0.55f));
        MakeBuilding(new Vector3(-12, 0, 22), new Vector3(6, 4, 6),
            new Color(0.85f, 0.82f, 0.75f), new Color(0.4f, 0.45f, 0.55f));
        MakeBox("SchoolGate_L", new Vector3(-3, 0, 15), new Vector3(0.6f, 2.5f, 0.6f),
            new Color(0.5f, 0.5f, 0.55f));
        MakeBox("SchoolGate_R", new Vector3(3, 0, 15),  new Vector3(0.6f, 2.5f, 0.6f),
            new Color(0.5f, 0.5f, 0.55f));

        // ---- 時計塔（中央北） ----
        MakeBuilding(new Vector3(10, 0, 12), new Vector3(4, 12, 4),
            new Color(0.72f, 0.66f, 0.58f), new Color(0.3f, 0.5f, 0.4f));
        var clock = MakeBox("ClockFace", new Vector3(10, 10.5f, 9.9f),
            new Vector3(2.2f, 2.2f, 0.2f), new Color(0.95f, 0.93f, 0.8f));
        MakeBox("ClockHand", new Vector3(10, 10.5f, 9.75f),
            new Vector3(0.15f, 0.9f, 0.1f), new Color(0.1f, 0.1f, 0.1f));

        // ---- 神社（東の丘） ----
        MakeBox("ShrineHill", new Vector3(24, 0.5f, 8), new Vector3(14, 1.2f, 14),
            new Color(0.5f, 0.6f, 0.4f));
        MakeBuilding(new Vector3(24, 1.1f, 10), new Vector3(6, 3.5f, 5),
            new Color(0.75f, 0.3f, 0.25f), new Color(0.35f, 0.3f, 0.3f));
        // 鳥居
        MakeBox("Torii_L",   new Vector3(22.5f, 1.1f, 3), new Vector3(0.5f, 3.5f, 0.5f),
            new Color(0.8f, 0.25f, 0.2f));
        MakeBox("Torii_R",   new Vector3(25.5f, 1.1f, 3), new Vector3(0.5f, 3.5f, 0.5f),
            new Color(0.8f, 0.25f, 0.2f));
        MakeBox("Torii_Top", new Vector3(24, 2.9f, 3),    new Vector3(4.6f, 0.5f, 0.7f),
            new Color(0.8f, 0.25f, 0.2f));

        // ---- 商店街（西） ----
        Color[] shopColors = {
            new Color(0.85f, 0.6f, 0.5f), new Color(0.6f, 0.7f, 0.85f),
            new Color(0.85f, 0.8f, 0.55f), new Color(0.65f, 0.8f, 0.6f) };
        for (int i = 0; i < 4; i++)
        {
            MakeBuilding(new Vector3(-24 + i * 5.5f, 0, 6), new Vector3(4.5f, 3.5f, 5),
                shopColors[i], new Color(0.45f, 0.35f, 0.3f));
        }
        // アイス屋の旗
        MakeBox("IceFlag", new Vector3(-24, 4.2f, 3.4f), new Vector3(0.15f, 1.6f, 0.15f),
            new Color(0.9f, 0.9f, 0.95f));

        // ---- 公園（南西） ----
        MakeBox("ParkGround", new Vector3(-18, 0.02f, -18), new Vector3(16, 0.04f, 14),
            new Color(0.55f, 0.68f, 0.42f));
        MakeTree(new Vector3(-24, 0, -14));
        MakeTree(new Vector3(-13, 0, -22));
        MakeTree(new Vector3(-22, 0, -23));
        MakeBox("Bench", new Vector3(-16, 0.4f, -16), new Vector3(2.2f, 0.5f, 0.7f),
            new Color(0.55f, 0.4f, 0.28f));

        // ---- 浜辺（南端） ----
        MakeBox("Sand",  new Vector3(8, 0.01f, -32), new Vector3(50, 0.05f, 12),
            new Color(0.87f, 0.8f, 0.6f));
        MakeBox("Sea",   new Vector3(8, -0.1f, -44), new Vector3(60, 0.05f, 14),
            new Color(0.25f, 0.5f, 0.75f));

        // ---- 住宅（散在） ----
        MakeBuilding(new Vector3(16, 0, -10), new Vector3(5, 3, 5),
            new Color(0.8f, 0.75f, 0.68f), new Color(0.6f, 0.3f, 0.25f));
        MakeBuilding(new Vector3(22, 0, -18), new Vector3(5, 3, 5),
            new Color(0.75f, 0.78f, 0.72f), new Color(0.35f, 0.4f, 0.55f));
        MakeBuilding(new Vector3(-28, 0, 16), new Vector3(5, 3, 5),
            new Color(0.78f, 0.7f, 0.66f), new Color(0.5f, 0.35f, 0.3f));

        // 広場の噴水（中央南）
        MakeBox("FountainBase", new Vector3(0, 0.3f, -12), new Vector3(4, 0.6f, 4),
            new Color(0.7f, 0.7f, 0.75f));
        MakeBox("FountainWater", new Vector3(0, 0.65f, -12), new Vector3(3.2f, 0.15f, 3.2f),
            new Color(0.4f, 0.65f, 0.9f));
    }

    private void MakeTree(Vector3 pos)
    {
        MakeBox("Trunk", pos + new Vector3(0, 1f, 0), new Vector3(0.6f, 2f, 0.6f),
            new Color(0.45f, 0.32f, 0.22f));
        MakeBox("Leaf",  pos + new Vector3(0, 2.8f, 0), new Vector3(2.6f, 2.2f, 2.6f),
            new Color(0.3f, 0.5f, 0.28f));
    }

    private void MakeBuilding(Vector3 pos, Vector3 size, Color body, Color roof)
    {
        MakeBox("Bldg", pos + new Vector3(0, size.y * 0.5f, 0), size, body);
        MakeBox("Roof", pos + new Vector3(0, size.y + 0.4f, 0),
            new Vector3(size.x + 0.8f, 0.8f, size.z + 0.8f), roof);
    }

    private GameObject MakeBox(string name, Vector3 pos, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position   = pos;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().material = MakeMat(color);
        return go;
    }

    // ================================================================
    // 空の隕石（Xデーの象徴）
    // ================================================================
    private void BuildSkyMeteor()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SkyMeteor";
        Destroy(go.GetComponent<Collider>());
        go.transform.position   = new Vector3(25, 40, 70);
        go.transform.localScale = Vector3.one * 12f;
        go.GetComponent<MeshRenderer>().material =
            MakeMat(new Color(0.9f, 0.3f, 0.2f), emissive: true);
    }

    // ================================================================
    // 街の住人（ドット絵NPC）
    // ================================================================
    private void BuildAmbientNpcs()
    {
        Vector3[] spots = {
            new Vector3(-20, 0.02f, 2),   // 商店街
            new Vector3(5, 0.02f, -10),   // 広場
            new Vector3(20, 0.02f, 0),    // 神社ふもと
            new Vector3(-2, 0.02f, 12),   // 学校前
        };
        for (int i = 0; i < spots.Length; i++)
        {
            var go = new GameObject("Npc_" + i);
            go.transform.position = spots[i];
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelSpriteFactory.Npc(i, 0);
            if (i % 2 == 1) sr.flipX = true;
        }
    }

    // ================================================================
    // HUD
    // ================================================================
    private void BuildHud()
    {
        var go = new GameObject("FieldHud");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        _objectiveText = MakeHudText(go.transform, 26, new Color(1f, 0.95f, 0.75f),
            TextAnchor.UpperLeft, new Vector2(0.01f, 0.90f), new Vector2(0.7f, 0.99f));

        _partyText = MakeHudText(go.transform, 22, new Color(0.8f, 0.9f, 1f),
            TextAnchor.UpperLeft, new Vector2(0.01f, 0.84f), new Vector2(0.9f, 0.90f));

        _interactText = MakeHudText(go.transform, 30, Color.white,
            TextAnchor.MiddleCenter, new Vector2(0.3f, 0.28f), new Vector2(0.7f, 0.34f));

        var help = MakeHudText(go.transform, 20, new Color(1f, 1f, 1f, 0.55f),
            TextAnchor.LowerRight, new Vector2(0.55f, 0.005f), new Vector2(0.995f, 0.05f));
        help.text = "移動：WASD / 矢印キー　　調べる：E";
    }

    private Text MakeHudText(Transform parent, int size, Color color, TextAnchor anchor,
        Vector2 ancMin, Vector2 ancMax)
    {
        var go = new GameObject("HudText");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = new Vector2(10, 0); rt.offsetMax = new Vector2(-10, 0);
        var t = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = size;
        t.color     = color;
        t.alignment = anchor;
        t.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor    = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    // ================================================================
    // マテリアル生成（URP Lit / フォールバック）
    // ================================================================
    public static Material MakeMat(Color color, bool emissive = false)
    {
        if (_litShader == null)
        {
            _litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_litShader == null) _litShader = Shader.Find("Standard");
        }
        var mat = new Material(_litShader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", color * 1.6f);
        }
        return mat;
    }
}
