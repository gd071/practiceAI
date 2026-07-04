using UnityEngine;

/// <summary>
/// ワールド空間のビジュアルを担当する。
/// 星空・空に浮かぶ不吉な隕石（Xデーの象徴）・地面ライン・区切り線を生成する。
/// BattleBootstrap から AddComponent で追加される。
/// </summary>
public class BattleVisuals : MonoBehaviour
{
    private SpriteRenderer _meteorGlow;

    void Start()
    {
        BuildStarfield();
        BuildDoomMeteor();
        BuildGroundLines();
        BuildBattlefieldDivider();
    }

    void Update()
    {
        // 隕石が不吉に脈動する
        if (_meteorGlow != null)
        {
            float pulse = (Mathf.Sin(Time.time * 1.6f) + 1f) * 0.5f;
            _meteorGlow.color = new Color(1f, 0.35f, 0.2f, 0.18f + pulse * 0.14f);
        }
    }

    // ================================================================
    // 星空
    // ================================================================
    private void BuildStarfield()
    {
        var root = new GameObject("Starfield");
        Random.State prev = Random.state;
        Random.InitState(20260401); // 4/1 ループ開始日

        for (int i = 0; i < 70; i++)
        {
            var go = new GameObject("Star" + i);
            go.transform.SetParent(root.transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeRect(1, 1, Color.white);
            sr.sortingOrder = -10;

            float brightness = Random.Range(0.25f, 0.9f);
            sr.color = new Color(brightness, brightness, Mathf.Min(1f, brightness + 0.1f), Random.Range(0.4f, 0.9f));

            float size = Random.Range(0.02f, 0.06f);
            go.transform.position   = new Vector3(Random.Range(-9f, 9f), Random.Range(-1.0f, 4.8f), 2f);
            go.transform.localScale = new Vector3(size, size, 1f);
        }
        Random.state = prev;
    }

    // ================================================================
    // 空に浮かぶ隕石（Xデーの象徴 ── 常に頭上にある脅威）
    // ================================================================
    private void BuildDoomMeteor()
    {
        var root = new GameObject("DoomMeteor");
        root.transform.position = new Vector3(-1.5f, 3.6f, 3f);

        // 外側の赤いグロー
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(root.transform, false);
        _meteorGlow = glowGo.AddComponent<SpriteRenderer>();
        _meteorGlow.sprite       = MakeCircle(64, new Color(1f, 0.4f, 0.2f, 1f), soft: true);
        _meteorGlow.sortingOrder = -9;
        glowGo.transform.localScale = new Vector3(2.6f, 2.6f, 1f);

        // 本体
        var coreGo = new GameObject("Core");
        coreGo.transform.SetParent(root.transform, false);
        var core = coreGo.AddComponent<SpriteRenderer>();
        core.sprite       = MakeCircle(64, new Color(0.45f, 0.18f, 0.12f, 1f), soft: false);
        core.sortingOrder = -8;
        coreGo.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
    }

    // ================================================================
    // 地面ライン
    // ================================================================
    private void BuildGroundLines()
    {
        // 味方前衛
        MakeHorizontalBar("Ground_AllyFront",
            x: -4.8f, y: -2.05f, w: 6.6f, h: 0.06f,
            color: new Color(0.3f, 0.45f, 0.8f, 0.6f));

        // 味方後衛（薄め）
        MakeHorizontalBar("Ground_AllyBack",
            x: -5.0f, y: 0.05f, w: 7.0f, h: 0.04f,
            color: new Color(0.3f, 0.45f, 0.8f, 0.30f));

        // 敵側
        MakeHorizontalBar("Ground_Enemy",
            x: 5.0f, y: -1.40f, w: 5.8f, h: 0.06f,
            color: new Color(0.75f, 0.2f, 0.2f, 0.6f));

        // 影
        MakeShadowLine("Shadow_Ally",  x: -4.8f, y: -2.30f, w: 6.6f);
        MakeShadowLine("Shadow_Enemy", x:  5.0f, y: -1.65f, w: 5.8f);
    }

    private void MakeHorizontalBar(string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeRect(1, 1, Color.white);
        sr.color        = color;
        sr.sortingOrder = -1;
        go.transform.position   = new Vector3(x, y, 0.5f);
        go.transform.localScale = new Vector3(w, h, 1f);
    }

    private void MakeShadowLine(string name, float x, float y, float w)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeRect(1, 1, Color.white);
        sr.color        = new Color(0f, 0f, 0f, 0.3f);
        sr.sortingOrder = -2;
        go.transform.position   = new Vector3(x, y, 0.6f);
        go.transform.localScale = new Vector3(w, 0.18f, 1f);
    }

    // ================================================================
    // 戦場の区切りライン（中央縦線）
    // ================================================================
    private void BuildBattlefieldDivider()
    {
        var go = new GameObject("BattleDivider");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeRect(1, 1, Color.white);
        sr.color        = new Color(0.5f, 0.5f, 0.8f, 0.08f);
        sr.sortingOrder = -1;
        go.transform.position   = new Vector3(0.3f, 0f, 0.5f);
        go.transform.localScale = new Vector3(0.02f, 12f, 1f);
    }

    // ================================================================
    // ユーティリティ
    // ================================================================
    private static Sprite MakeRect(int w, int h, Color color)
    {
        var tex    = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
    }

    /// <summary>円形スプライト。soft=true でエッジをフェードさせる。</summary>
    private static Sprite MakeCircle(int size, Color color, bool soft)
    {
        var tex    = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half + 0.5f, dy = y - half + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / half;
                float a;
                if (soft)
                    a = Mathf.Clamp01(1f - dist) * color.a;
                else
                    a = dist < 0.95f ? color.a : 0f;
                pixels[y * size + x] = new Color(color.r, color.g, color.b, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
