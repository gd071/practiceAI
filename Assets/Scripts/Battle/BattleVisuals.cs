using UnityEngine;

/// <summary>
/// ワールド空間のビジュアルを担当する。
/// グラデーション背景・地面・キャラ名ラベル・スプライト装飾を生成する。
/// BattleBootstrap から AddComponent で追加される。
/// </summary>
public class BattleVisuals : MonoBehaviour
{
    void Start()
    {
        BuildGroundLine();
        BuildBattlefieldDivider();
    }

    // ================================================================
    // グラデーション背景（上：深い紫紺 → 下：暗黒）
    // ================================================================
    private void BuildGradientBackground()
    {
        var go = new GameObject("GradientBG");
        var mesh = new Mesh();

        // 画面より少し大きいクワッド（カメラ ortho size=5, aspect≈1.78）
        float w = 20f, h = 12f;
        mesh.vertices = new Vector3[]
        {
            new(-w, -h, 1f),
            new( w, -h, 1f),
            new( w,  h, 1f),
            new(-w,  h, 1f),
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.uv = new Vector2[]
        {
            new(0, 0), new(1, 0), new(1, 1), new(0, 1)
        };

        // 頂点カラーでグラデーション
        var colBottom = new Color(0.02f, 0.01f, 0.05f);
        var colTop    = new Color(0.05f, 0.04f, 0.16f);
        mesh.colors = new Color[] { colBottom, colBottom, colTop, colTop };

        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.material = CreateVertexColorMat();
        mr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows       = false;
    }

    // ================================================================
    // 地面ライン（奥行き感）
    // ================================================================
    private void BuildGroundLine()
    {
        // 味方側の地面
        MakeHorizontalBar("Ground_Ally",
            x: -6f, y: -0.6f, w: 5.5f, h: 0.06f,
            color: new Color(0.3f, 0.45f, 0.8f, 0.6f));

        // 敵側の地面
        MakeHorizontalBar("Ground_Enemy",
            x:  4.5f, y:  2.0f, w: 4.5f, h: 0.06f,
            color: new Color(0.75f, 0.2f, 0.2f, 0.6f));

        // 影（地面の直下に薄い楕円風）
        MakeShadowLine("Shadow_Ally",   x: -6f,  y: -0.85f);
        MakeShadowLine("Shadow_Enemy",  x:  4.5f, y:  1.75f);
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

    private void MakeShadowLine(string name, float x, float y)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeRect(1, 1, Color.white);
        sr.color        = new Color(0f, 0f, 0f, 0.3f);
        sr.sortingOrder = -2;
        go.transform.position   = new Vector3(x, y, 0.6f);
        go.transform.localScale = new Vector3(5f, 0.18f, 1f);
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
        go.transform.position   = new Vector3(0f, 0f, 0.5f);
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

    private static Material CreateVertexColorMat()
    {
        // Sprites/Default は URP でも頂点カラー（SpriteRenderer.color と同等）に対応している
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader);
        mat.enableInstancing = false;
        return mat;
    }
}
