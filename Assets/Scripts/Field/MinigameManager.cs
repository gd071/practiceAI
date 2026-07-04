using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 町の要所で遊べるミニゲーム3種。
///  1. シャボン玉あそび（浜辺）……飛んでくるシャボン玉をクリックで割る
///  2. アイス早食い競争（商店街）…スペース連打
///  3. 花火打ち上げ（公園）………タイミングバーで真ん中を狙う
/// 初クリアで「加護」を獲得し、バトルの全員ステータスが強化される。
/// </summary>
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }
    public static bool IsPlaying          { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance  = null;
        IsPlaying = false;
    }

    private Canvas        _canvas;
    private RectTransform _root;
    private static Font DefaultFont =>
        Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildCanvas();
    }

    private void BuildCanvas()
    {
        var go = new GameObject("MinigameCanvas");
        go.transform.SetParent(transform, false);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 45;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        _root = go.GetComponent<RectTransform>();
    }

    // ================================================================
    // 1. シャボン玉あそび
    // ================================================================
    public void StartBubble()
    {
        if (IsPlaying) return;
        StartCoroutine(CoBubble());
    }

    private IEnumerator CoBubble()
    {
        IsPlaying = true;
        var bg = MakeOverlay(new Color(0.2f, 0.4f, 0.6f, 0.85f));
        var title = MakeText("シャボン玉あそび！　12秒で 8個 割ろう！", 38,
            new Color(1f, 1f, 0.8f), new Vector2(0, 420), new Vector2(1400, 60));
        var scoreText = MakeText("0 / 8", 34, Color.white,
            new Vector2(0, 350), new Vector2(400, 50));

        int popped = 0;
        float timer = 12f;
        var bubbles = new System.Collections.Generic.List<GameObject>();
        float nextSpawn = 0f;

        while (timer > 0f && popped < 8)
        {
            timer -= Time.deltaTime;
            nextSpawn -= Time.deltaTime;

            if (nextSpawn <= 0f)
            {
                nextSpawn = 0.55f;
                var b = new GameObject("Bubble");
                b.transform.SetParent(_root, false);
                var rt = b.AddComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(Random.Range(-750f, 750f), -560f);
                float s = Random.Range(70f, 130f);
                rt.sizeDelta = new Vector2(s, s);
                var img = b.AddComponent<Image>();
                img.color = new Color(0.7f, 0.9f, 1f, 0.75f);
                var btn = b.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() =>
                {
                    popped++;
                    scoreText.text = $"{popped} / 8";
                    Destroy(b);
                });
                b.AddComponent<BubbleFloat>();
                bubbles.Add(b);
            }

            title.text = $"シャボン玉あそび！　残り {timer:F1} 秒";
            yield return null;
        }

        foreach (var b in bubbles) if (b != null) Destroy(b);
        bool success = popped >= 8;
        yield return ShowResult(success, "bubble",
            success ? "全部キラキラ！　波の音と歓声が重なった。" : "波にさらわれちゃった……また挑戦しよう！");

        Destroy(bg.gameObject); Destroy(title.gameObject); Destroy(scoreText.gameObject);
        IsPlaying = false;
    }

    /// <summary>シャボン玉をふわふわ上昇させる。</summary>
    private class BubbleFloat : MonoBehaviour
    {
        private RectTransform _rt;
        private float _drift;
        void Start()
        {
            _rt = GetComponent<RectTransform>();
            _drift = Random.Range(-40f, 40f);
        }
        void Update()
        {
            if (_rt == null) return;
            _rt.anchoredPosition += new Vector2(
                Mathf.Sin(Time.time * 2f + _drift) * 40f * Time.deltaTime,
                120f * Time.deltaTime);
            if (_rt.anchoredPosition.y > 620f) Destroy(gameObject);
        }
    }

    // ================================================================
    // 2. アイス早食い競争
    // ================================================================
    public void StartIceMash()
    {
        if (IsPlaying) return;
        StartCoroutine(CoIceMash());
    }

    private IEnumerator CoIceMash()
    {
        IsPlaying = true;
        var bg = MakeOverlay(new Color(0.5f, 0.35f, 0.25f, 0.88f));
        var title = MakeText("アイス早食い競争！　スペース連打で 40口 食べきれ！", 38,
            new Color(1f, 0.95f, 0.85f), new Vector2(0, 420), new Vector2(1500, 60));

        // アイス本体（連打で縮む）
        var ice = MakeImage(new Vector2(0, -30), new Vector2(220, 380),
            new Color(0.95f, 0.9f, 0.85f));
        var cone = MakeImage(new Vector2(0, -300), new Vector2(140, 160),
            new Color(0.8f, 0.6f, 0.35f));
        var counter = MakeText("0 / 40", 46, Color.white,
            new Vector2(0, 320), new Vector2(500, 70));

        int bites = 0;
        float timer = 8f;

        while (timer > 0f && bites < 40)
        {
            timer -= Time.deltaTime;
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
            {
                bites++;
                counter.text = $"{bites} / 40";
                float r = 1f - bites / 40f;
                ice.rectTransform.sizeDelta = new Vector2(220f * Mathf.Max(0.05f, r), 380f * Mathf.Max(0.05f, r));
            }
            title.text = $"スペース連打！　残り {timer:F1} 秒";
            yield return null;
        }

        bool success = bites >= 40;
        yield return ShowResult(success, "ice",
            success ? "完食！　……頭がキーンとするけど、最高の味だ。" : "溶けちゃった……歩きながら食べる練習をしよう。");

        Destroy(bg.gameObject); Destroy(title.gameObject);
        Destroy(ice.gameObject); Destroy(cone.gameObject); Destroy(counter.gameObject);
        IsPlaying = false;
    }

    // ================================================================
    // 3. 花火打ち上げ
    // ================================================================
    public void StartFireworks()
    {
        if (IsPlaying) return;
        StartCoroutine(CoFireworks());
    }

    private IEnumerator CoFireworks()
    {
        IsPlaying = true;
        var bg = MakeOverlay(new Color(0.05f, 0.05f, 0.15f, 0.93f));
        var title = MakeText("花火打ち上げ！　バーが中央に来たらスペース！（3発中2発成功で勝利）", 34,
            new Color(1f, 0.9f, 0.7f), new Vector2(0, 420), new Vector2(1600, 60));

        // タイミングバー
        var barBg = MakeImage(new Vector2(0, -350), new Vector2(900, 46),
            new Color(0.2f, 0.2f, 0.3f));
        var center = MakeImage(new Vector2(0, -350), new Vector2(110, 46),
            new Color(0.9f, 0.75f, 0.2f, 0.85f));
        var cursor = MakeImage(new Vector2(0, -350), new Vector2(18, 60),
            Color.white);

        int hits = 0;

        for (int round = 0; round < 3; round++)
        {
            float speed = 900f + round * 300f;
            float pos = -430f;
            int dir = 1;
            bool fired = false;

            while (!fired)
            {
                pos += dir * speed * Time.deltaTime;
                if (pos > 430f)  { pos = 430f;  dir = -1; }
                if (pos < -430f) { pos = -430f; dir = 1; }
                cursor.rectTransform.anchoredPosition = new Vector2(pos, -350f);

                var kb = Keyboard.current;
                if (kb != null && kb.spaceKey.wasPressedThisFrame)
                {
                    fired = true;
                    bool hit = Mathf.Abs(pos) < 60f;
                    if (hit) hits++;
                    yield return CoLaunchFirework(hit);
                }
                yield return null;
            }
        }

        bool success = hits >= 2;
        yield return ShowResult(success, "hanabi",
            success ? "大輪の花が夜空に咲いた。……誰かの鼻歌が、聴こえた気がした。" : "不発……火薬を乾かして出直そう。");

        Destroy(bg.gameObject); Destroy(title.gameObject);
        Destroy(barBg.gameObject); Destroy(center.gameObject); Destroy(cursor.gameObject);
        IsPlaying = false;
    }

    private IEnumerator CoLaunchFirework(bool success)
    {
        // 打ち上げの光点
        var spark = MakeImage(new Vector2(Random.Range(-300f, 300f), -250f),
            new Vector2(14, 14), Color.white);
        float t = 0f;
        Vector2 from = spark.rectTransform.anchoredPosition;
        Vector2 to   = from + new Vector2(Random.Range(-60f, 60f), 550f);
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            spark.rectTransform.anchoredPosition = Vector2.Lerp(from, to, t / 0.5f);
            yield return null;
        }
        Destroy(spark.gameObject);

        if (!success) yield break;

        // 開花
        var petals = new System.Collections.Generic.List<Image>();
        Color col = new Color(Random.Range(0.6f, 1f), Random.Range(0.5f, 1f), Random.Range(0.4f, 1f));
        for (int i = 0; i < 16; i++)
        {
            var p = MakeImage(to, new Vector2(16, 16), col);
            petals.Add(p);
        }
        t = 0f;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            for (int i = 0; i < petals.Count; i++)
            {
                float ang = i * Mathf.PI * 2f / petals.Count;
                float dist = t / 0.7f * 260f;
                petals[i].rectTransform.anchoredPosition =
                    to + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                petals[i].color = new Color(col.r, col.g, col.b, 1f - t / 0.7f);
            }
            yield return null;
        }
        foreach (var p in petals) Destroy(p.gameObject);
    }

    // ================================================================
    // 結果表示・加護付与
    // ================================================================
    private IEnumerator ShowResult(bool success, string blessingId, string flavor)
    {
        string msg = success ? "★ クリア！ ★\n" + flavor : "ざんねん…\n" + flavor;

        bool firstClear = success && !StoryManager.Blessings.Contains(blessingId);
        if (firstClear)
        {
            StoryManager.AddBlessing(blessingId);
            msg += "\n\n《思い出の加護》を獲得！　仲間全員が少し強くなった。";
        }

        var panel = MakeImage(Vector2.zero, new Vector2(900, 300),
            new Color(0.05f, 0.05f, 0.12f, 0.96f));
        var text = MakeText(msg, 32, success ? new Color(1f, 0.95f, 0.6f) : Color.white,
            Vector2.zero, new Vector2(860, 280));

        yield return new WaitForSeconds(3.2f);
        Destroy(panel.gameObject);
        Destroy(text.gameObject);
    }

    // ================================================================
    // ファクトリ
    // ================================================================
    private Image MakeOverlay(Color color)
    {
        var go = new GameObject("MgOverlay");
        go.transform.SetParent(_root, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private Image MakeImage(Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("MgImg");
        go.transform.SetParent(_root, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text MakeText(string content, int size, Color color, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("MgText");
        go.transform.SetParent(_root, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = DefaultFont;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(2, -2);
        return t;
    }
}
