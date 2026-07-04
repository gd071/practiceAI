using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 必殺技のメタUI演出を担当する。
/// UIそのものが攻撃・演出になる（UI非表示→プレイヤー直接攻撃、巨大ボタン、
/// ショップ出現、文字化け、画面反転→隕石、UIが敵に飛ぶ 等）。
/// インタラクティブな必殺技はダメージ処理までここが担当し、
/// BattleManager は SequenceRunning が false になるまで待機する。
/// </summary>
[DefaultExecutionOrder(-4)]
public class UIEffectManager : MonoBehaviour
{
    /// <summary>演出シーケンス実行中フラグ。BattleManager が待機に使う。</summary>
    public static bool SequenceRunning { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => SequenceRunning = false;

    // BattleUI が生成するルートCanvas
    private RectTransform _canvasRt;
    private RectTransform _topPanel;
    private RectTransform _logPanel;
    private RectTransform _bottomPanel;
    private Text          _logText;
    private CanvasGroup   _canvasGroup;

    // メタ演出専用Canvas（バトルUIを消しても表示される最前面レイヤー）
    private Canvas        _metaCanvas;
    private RectTransform _metaRoot;

    private static Font DefaultFont =>
        Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // ================================================================
    void Start()
    {
        var canvasGo = GameObject.Find("BattleCanvas");
        if (canvasGo != null)
        {
            _canvasRt    = canvasGo.GetComponent<RectTransform>();
            _topPanel    = canvasGo.transform.Find("TopPanel")    as RectTransform;
            _logPanel    = canvasGo.transform.Find("LogPanel")    as RectTransform;
            _bottomPanel = canvasGo.transform.Find("BottomPanel") as RectTransform;

            var logTr = canvasGo.transform.Find("LogPanel/Text");
            if (logTr != null) _logText = logTr.GetComponent<Text>();

            _canvasGroup = canvasGo.GetComponent<CanvasGroup>()
                        ?? canvasGo.AddComponent<CanvasGroup>();
        }

        BuildMetaCanvas();
    }

    private void BuildMetaCanvas()
    {
        var go = new GameObject("MetaCanvas");
        _metaCanvas = go.AddComponent<Canvas>();
        _metaCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _metaCanvas.sortingOrder = 40; // 全UIの最前面

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        _metaRoot = go.GetComponent<RectTransform>();
    }

    void OnEnable()  => BattleManager.OnUltimateActivated += OnUltimate;
    void OnDisable() => BattleManager.OnUltimateActivated -= OnUltimate;

    // ================================================================
    private void OnUltimate(BattleUnit unit, UltimateType type)
    {
        // インタラクティブ系は同期的にフラグを立てて BattleManager を待たせる
        switch (type)
        {
            case UltimateType.MemoryReplay:
            case UltimateType.DimensionCall:
            case UltimateType.AbyssCall:
            case UltimateType.SatelliteBeam:
            case UltimateType.GhostRush:
            case UltimateType.Payoff:
                SequenceRunning = true;
                break;
        }
        StartCoroutine(PlayEffect(unit, type));
    }

    private IEnumerator PlayEffect(BattleUnit actor, UltimateType type)
    {
        switch (type)
        {
            case UltimateType.TimeStop:      yield return CoTimeStop();            break;
            case UltimateType.SpaceRift:     yield return CoSpaceRift();           break;
            case UltimateType.EventRewrite:  yield return CoEventRewrite();        break;

            case UltimateType.MemoryReplay:  yield return CoMemoryReplay(actor);   break;
            case UltimateType.DimensionCall: yield return CoDimensionCall(actor);  break;
            case UltimateType.AbyssCall:     yield return CoAbyssCall(actor);      break;
            case UltimateType.SatelliteBeam: yield return CoSatelliteBeam(actor);  break;
            case UltimateType.GhostRush:     yield return CoGhostRush(actor);      break;
            case UltimateType.Payoff:        yield return CoPayoff(actor);         break;

            default:                         yield return CoPulse();               break;
        }
        SequenceRunning = false;
    }

    // ================================================================
    // 主人公：時間断絶 ── UIが青白く凍りついてガタつく
    // ================================================================
    private IEnumerator CoTimeStop()
    {
        yield return LerpCanvasAlpha(1f, 0.45f, 0.18f);
        yield return ShakeCanvas(0.25f, 14f);

        // 砂時計が砕ける表現：カウント表示付近に破片
        var shards = new List<Image>();
        for (int i = 0; i < 10; i++)
        {
            var s = MakeMetaImage(new Vector2(Random.Range(-140f, 140f), Random.Range(380f, 470f)),
                new Vector2(Random.Range(8f, 22f), Random.Range(8f, 22f)),
                new Color(0.7f, 0.9f, 1f, 0.9f));
            s.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            shards.Add(s);
        }

        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            foreach (var s in shards)
            {
                if (s == null) continue;
                s.rectTransform.anchoredPosition += new Vector2(0, -Time.deltaTime * 420f);
                var c = s.color; c.a = 1f - t / 0.7f; s.color = c;
            }
            yield return null;
        }
        foreach (var s in shards) if (s != null) Destroy(s.gameObject);

        yield return LerpCanvasAlpha(0.45f, 1f, 0.22f);
    }

    // ================================================================
    // ヒロイン：記憶回廊 ── 画面（UI）がひっくり返り、隕石が落ちる
    // ================================================================
    private IEnumerator CoMemoryReplay(BattleUnit actor)
    {
        // 1. Canvas 全体を180度回転（UIがひっくり返る）
        if (_canvasRt != null)
        {
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                _canvasRt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 180f, t / 0.45f));
                yield return null;
            }
            _canvasRt.localRotation = Quaternion.Euler(0, 0, 180f);
        }

        var label = MakeMetaText("──記憶が、あの日の空を再生する──", 40,
            new Color(1f, 0.9f, 0.6f), new Vector2(0f, 200f), new Vector2(1400, 80));
        yield return new WaitForSeconds(0.7f);
        Destroy(label.gameObject);

        // 2. 隕石落下（巨大な火球が画面上から敵陣へ）
        var meteor = MakeMetaImage(new Vector2(450f, 750f), new Vector2(260f, 260f),
            new Color(1f, 0.45f, 0.15f, 0.95f));
        var core = MakeMetaImage(Vector2.zero, new Vector2(150f, 150f),
            new Color(1f, 0.85f, 0.4f, 1f));
        core.transform.SetParent(meteor.transform, false);

        float tf = 0f;
        Vector2 from = meteor.rectTransform.anchoredPosition;
        Vector2 to   = new Vector2(450f, -80f);
        while (tf < 0.55f)
        {
            tf += Time.deltaTime;
            meteor.rectTransform.anchoredPosition = Vector2.Lerp(from, to, tf / 0.55f);
            yield return null;
        }

        // 着弾フラッシュ
        var flash = MakeMetaOverlay(new Color(1f, 0.7f, 0.3f, 0f));
        float ft = 0f;
        while (ft < 0.15f)
        {
            ft += Time.deltaTime;
            flash.color = new Color(1f, 0.7f, 0.3f, ft / 0.15f * 0.9f);
            yield return null;
        }

        // ダメージ適用：敵全体に隕石
        var bm = BattleManager.Instance;
        if (bm != null)
        {
            foreach (var e in bm.AliveEnemies().ToArray())
                bm.UltHit(e, Mathf.RoundToInt(actor.Data.atk * 2.4f));
        }

        while (ft > 0f)
        {
            ft -= Time.deltaTime * 1.6f;
            flash.color = new Color(1f, 0.7f, 0.3f, Mathf.Max(0, ft / 0.15f) * 0.9f);
            yield return null;
        }
        Destroy(flash.gameObject);
        Destroy(meteor.gameObject);

        // 3. UIを元に戻す
        if (_canvasRt != null)
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                _canvasRt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(180f, 360f, t / 0.35f));
                yield return null;
            }
            _canvasRt.localRotation = Quaternion.identity;
        }
    }

    // ================================================================
    // 親友：空間斬 ── 上下パネルが物理的に引き裂かれる
    // ================================================================
    private IEnumerator CoSpaceRift()
    {
        if (_topPanel == null || _bottomPanel == null) yield break;

        Vector2 topOrig = _topPanel.anchoredPosition;
        Vector2 botOrig = _bottomPanel.anchoredPosition;

        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.deltaTime;
            float r = t / 0.22f;
            _topPanel.anchoredPosition    = topOrig + new Vector2(0,  r * 90f);
            _bottomPanel.anchoredPosition = botOrig + new Vector2(0, -r * 90f);
            yield return null;
        }

        yield return new WaitForSeconds(0.35f);

        t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            float r = 1f - t / 0.18f;
            _topPanel.anchoredPosition    = topOrig + new Vector2(0,  r * 90f);
            _bottomPanel.anchoredPosition = botOrig + new Vector2(0, -r * 90f);
            yield return null;
        }
        _topPanel.anchoredPosition    = topOrig;
        _bottomPanel.anchoredPosition = botOrig;

        yield return ShakeCanvas(0.2f, 8f);
    }

    // ================================================================
    // ショタ：事象書換 ── ログテキストが文字化けして書き直される
    // ================================================================
    private IEnumerator CoEventRewrite()
    {
        if (_logText == null) yield break;

        string original = _logText.text;
        const string pool = "あいうえおかきくけこ０１２３！？#＊▲△□■※死生転写";

        for (int i = 0; i < 18; i++)
        {
            var sb = new StringBuilder();
            int len = Random.Range(8, 22);
            for (int c = 0; c < len; c++)
                sb.Append(pool[Random.Range(0, pool.Length)]);
            _logText.text = sb.ToString();
            yield return new WaitForSeconds(0.055f);
        }

        _logText.text = "";
        foreach (char ch in original)
        {
            _logText.text += ch;
            yield return new WaitForSeconds(0.015f);
        }
    }

    // ================================================================
    // 次元の巫女：次元召喚 ── 全UIが消え、プレイヤー自身が敵を攻撃する
    // ================================================================
    private IEnumerator CoDimensionCall(BattleUnit actor)
    {
        // UIが割れて消える
        yield return ShakeCanvas(0.3f, 18f);
        yield return LerpCanvasAlpha(1f, 0f, 0.4f);

        var title = MakeMetaText("──次元の壁が、開いた──", 44,
            new Color(0.85f, 0.7f, 1f), new Vector2(0f, 330f), new Vector2(1400, 80));
        var guide = MakeMetaText("プレイヤーよ。画面をクリックして直接攻撃せよ（残り 5 回）", 30,
            Color.white, new Vector2(0f, 260f), new Vector2(1500, 60));

        // 全画面クリック受付
        int remaining = 5;
        var clickGo = new GameObject("DimensionClickCatcher");
        clickGo.transform.SetParent(_metaRoot, false);
        var crt = clickGo.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = crt.offsetMax = Vector2.zero;
        var cimg = clickGo.AddComponent<Image>();
        cimg.color = new Color(0f, 0f, 0f, 0.01f); // ほぼ透明だがクリック可能
        var cbtn = clickGo.AddComponent<Button>();

        cbtn.onClick.AddListener(() =>
        {
            if (remaining <= 0) return;
            remaining--;
            guide.text = remaining > 0
                ? $"プレイヤーよ。画面をクリックして直接攻撃せよ（残り {remaining} 回）"
                : "──次元の壁が、閉じていく──";

            var bm = BattleManager.Instance;
            bm?.UltHit(null, Mathf.RoundToInt(actor.Data.atk * 1.3f));
            StartCoroutine(CoPlayerSlash());
        });

        // 5クリック or 10秒で終了
        float timeout = 10f;
        while (remaining > 0 && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            if (BattleManager.Instance != null && !BattleManager.Instance.IsBattleActive) break;
            yield return null;
        }

        Destroy(clickGo);
        Destroy(title.gameObject);
        Destroy(guide.gameObject);

        yield return LerpCanvasAlpha(0f, 1f, 0.35f);
    }

    /// <summary>プレイヤーの一撃：画面を白い斬撃が走る。</summary>
    private IEnumerator CoPlayerSlash()
    {
        var slash = MakeMetaImage(
            new Vector2(Random.Range(-300f, 500f), Random.Range(-150f, 250f)),
            new Vector2(900f, 8f), new Color(1f, 1f, 1f, 1f));
        slash.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-70f, 70f));
        slash.raycastTarget = false;

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            slash.color = new Color(1f, 1f, 1f, 1f - t / 0.25f);
            yield return null;
        }
        Destroy(slash.gameObject);
    }

    // ================================================================
    // 美少女ギャル：深淵 ── 全UIが文字化けし、本体（クトゥルフ）が蹂躙する
    // ================================================================
    private IEnumerator CoAbyssCall(BattleUnit actor)
    {
        // Canvas 上の全 Text を収集して文字化けさせる
        var texts = _canvasRt != null
            ? _canvasRt.GetComponentsInChildren<Text>(true)
            : new Text[0];
        var originals = new Dictionary<Text, string>();
        foreach (var t in texts) originals[t] = t.text;

        const string pool = "ﾆ§Ψヸ√≠∀彁◈▚▞卍Ω々〆ヱ゛゜ㇷ゚厨襾龘齾爩鬱";

        var label = MakeMetaText("【 本 体 召 喚 】", 56,
            new Color(0.6f, 0.2f, 0.9f), new Vector2(0f, 300f), new Vector2(1200, 90));

        // 沈み + 文字化けループ + 触手ヒット
        Vector2 origPos = _canvasRt != null ? _canvasRt.anchoredPosition : Vector2.zero;
        var bm = BattleManager.Instance;
        int hits = 4;

        float elapsed = 0f;
        const float duration = 2.2f;
        float nextScramble = 0f, nextHit = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (_canvasRt != null)
            {
                float sink = Mathf.Min(1f, elapsed / 0.5f);
                _canvasRt.anchoredPosition = origPos + new Vector2(0, -sink * 50f);
                if (_canvasGroup != null) _canvasGroup.alpha = 1f - sink * 0.35f;
            }

            // 文字化け更新
            if (elapsed >= nextScramble)
            {
                nextScramble = elapsed + 0.08f;
                foreach (var kv in originals)
                {
                    if (kv.Key == null) continue;
                    var sb = new StringBuilder();
                    int len = Mathf.Max(4, kv.Value.Length);
                    for (int c = 0; c < len; c++)
                        sb.Append(pool[Random.Range(0, pool.Length)]);
                    kv.Key.text = sb.ToString();
                }
            }

            // 触手の一撃
            if (hits > 0 && elapsed >= nextHit)
            {
                nextHit = elapsed + 0.4f;
                hits--;
                bm?.UltHit(null, Mathf.RoundToInt(actor.Data.atk * 1.1f));
                StartCoroutine(CoTentacle());
            }

            yield return null;
        }

        // 復元
        foreach (var kv in originals)
            if (kv.Key != null) kv.Key.text = kv.Value;

        if (_canvasRt != null) _canvasRt.anchoredPosition = origPos;
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        Destroy(label.gameObject);
    }

    /// <summary>深淵の触手：画面端から紫の帯が振り下ろされる。</summary>
    private IEnumerator CoTentacle()
    {
        var tent = MakeMetaImage(
            new Vector2(Random.Range(150f, 750f), 620f),
            new Vector2(60f, 1300f),
            new Color(0.4f, 0.1f, 0.6f, 0.85f));
        tent.raycastTarget = false;
        tent.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-25f, 25f));

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            tent.color = new Color(0.4f, 0.1f, 0.6f, 0.85f * (1f - t / 0.3f));
            yield return null;
        }
        Destroy(tent.gameObject);
    }

    // ================================================================
    // アンドロイド：衛星砲 ── UIが全部消え、巨大ボタンだけの画面になる
    // ================================================================
    private IEnumerator CoSatelliteBeam(BattleUnit actor)
    {
        yield return LerpCanvasAlpha(1f, 0f, 0.35f);

        var caption = MakeMetaText("衛星軌道上ステーション ─ 照準完了", 34,
            new Color(0.6f, 1f, 0.9f), new Vector2(0f, 280f), new Vector2(1400, 60));

        // 巨大ボタン
        bool pressed = false;
        var btnGo = new GameObject("BigRedButton");
        btnGo.transform.SetParent(_metaRoot, false);
        var brt = btnGo.AddComponent<RectTransform>();
        brt.anchoredPosition = new Vector2(0f, -40f);
        brt.sizeDelta = new Vector2(420f, 420f);
        var bimg = btnGo.AddComponent<Image>();
        bimg.color = new Color(1f, 0.2f, 0.25f);
        var bbtn = btnGo.AddComponent<Button>();
        var colors = bbtn.colors;
        colors.highlightedColor = new Color(1f, 0.45f, 0.45f);
        colors.pressedColor     = new Color(0.7f, 0.1f, 0.12f);
        bbtn.colors = colors;
        bbtn.targetGraphic = bimg;
        bbtn.onClick.AddListener(() => pressed = true);

        var btnLabel = new GameObject("Label");
        btnLabel.transform.SetParent(btnGo.transform, false);
        var lrt = btnLabel.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var ltxt = btnLabel.AddComponent<Text>();
        ltxt.text = "PUSH";
        ltxt.font = DefaultFont;
        ltxt.fontSize = 90;
        ltxt.fontStyle = FontStyle.Bold;
        ltxt.color = Color.white;
        ltxt.alignment = TextAnchor.MiddleCenter;
        ltxt.raycastTarget = false;

        // ボタン点滅しながら待機（8秒で自動発射）
        float timeout = 8f;
        while (!pressed && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            float pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
            bimg.color = Color.Lerp(new Color(1f, 0.2f, 0.25f), new Color(1f, 0.5f, 0.5f), pulse);
            yield return null;
        }

        Destroy(btnGo);
        Destroy(caption.gameObject);

        // 発射！全画面ビーム
        var beam = MakeMetaImage(new Vector2(0f, 0f), new Vector2(500f, 2200f),
            new Color(0.7f, 1f, 0.95f, 0.95f));
        beam.raycastTarget = false;

        var bm = BattleManager.Instance;
        if (bm != null)
        {
            foreach (var e in bm.AliveEnemies().ToArray())
                bm.UltHit(e, Mathf.RoundToInt(actor.Data.atk * 2.8f));
        }

        float bt = 0f;
        while (bt < 0.6f)
        {
            bt += Time.deltaTime;
            beam.color = new Color(0.7f, 1f, 0.95f, 0.95f * (1f - bt / 0.6f));
            beam.rectTransform.sizeDelta = new Vector2(500f * (1f - bt / 0.6f * 0.7f), 2200f);
            yield return null;
        }
        Destroy(beam.gameObject);

        yield return LerpCanvasAlpha(0f, 1f, 0.3f);
    }

    // ================================================================
    // 幽霊犬：突撃 ── UIパーツが敵に向かって飛んでいく
    // ================================================================
    private IEnumerator CoGhostRush(BattleUnit actor)
    {
        var bm  = BattleManager.Instance;
        var cam = Camera.main;

        string[] labels = { "攻撃", "スキル", "パス", "HP", "MP", "わん！" };

        for (int i = 0; i < 6; i++)
        {
            // 敵のスクリーン座標を狙う
            Vector2 targetPos = new Vector2(Random.Range(250f, 650f), Random.Range(50f, 300f));
            var enemies = bm != null ? bm.AliveEnemies() : null;
            if (enemies != null && enemies.Count > 0 && cam != null)
            {
                var e = enemies[Random.Range(0, enemies.Count)];
                Vector3 sp = cam.WorldToScreenPoint(e.transform.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _metaRoot, sp, null, out targetPos);
            }

            // UIチップ生成（下部から発射）
            var chip = MakeMetaImage(new Vector2(Random.Range(-700f, -300f), -420f),
                new Vector2(150f, 46f), new Color(0.35f, 0.55f, 0.95f, 0.95f));
            chip.raycastTarget = false;
            var chipText = new GameObject("T");
            chipText.transform.SetParent(chip.transform, false);
            var trt = chipText.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tt = chipText.AddComponent<Text>();
            tt.text = labels[i]; tt.font = DefaultFont; tt.fontSize = 24;
            tt.color = Color.white; tt.alignment = TextAnchor.MiddleCenter;
            tt.raycastTarget = false;

            // 飛翔
            Vector2 from = chip.rectTransform.anchoredPosition;
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float r = t / 0.22f;
                chip.rectTransform.anchoredPosition = Vector2.Lerp(from, targetPos, r * r);
                chip.rectTransform.localRotation = Quaternion.Euler(0, 0, r * 540f);
                yield return null;
            }

            bm?.UltHit(null, Mathf.RoundToInt(actor.Data.atk * 0.85f));
            Destroy(chip.gameObject);
            yield return new WaitForSeconds(0.08f);
        }

        // 余韻：UI全体がふるえる
        yield return ShakeCanvas(0.25f, 10f);
    }

    // ================================================================
    // 王女：ペイオフ ── 戦場にショップUIが出現する
    // ================================================================
    private IEnumerator CoPayoff(BattleUnit actor)
    {
        if (_canvasGroup != null) _canvasGroup.alpha = 0.35f;

        int coins  = 3;
        bool done  = false;
        var  bm    = BattleManager.Instance;

        // ショップパネル
        var panel = new GameObject("PayoffShop");
        panel.transform.SetParent(_metaRoot, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(780f, 560f);
        var pimg = panel.AddComponent<Image>();
        pimg.color = new Color(0.12f, 0.09f, 0.03f, 0.97f);

        MakeChildText(panel.transform, "── 王女のペイオフ・ショップ ──", 40,
            new Color(1f, 0.85f, 0.3f), new Vector2(0f, 220f), new Vector2(740f, 60f));
        var coinText = MakeChildText(panel.transform, "所持コイン：3枚", 30,
            Color.white, new Vector2(0f, 160f), new Vector2(600f, 50f));

        // 商品ボタン
        (string label, System.Action effect)[] items =
        {
            ("メテオ先物 購入\n（敵全体に大ダメージ）", () =>
            {
                if (bm == null) return;
                foreach (var e in bm.AliveEnemies().ToArray())
                    bm.UltHit(e, Mathf.RoundToInt(actor.Data.atk * 1.6f));
            }),
            ("回復薬 買い占め\n（味方全体HP回復）", () =>
            {
                bm?.HealAllAllies(Mathf.RoundToInt(actor.Data.atk * 1.4f));
            }),
            ("時間の前借り\n（砂時計カウント +30）", () =>
            {
                bm?.AddCountBonus(30);
            }),
        };

        var buttons = new List<Button>();
        for (int i = 0; i < items.Length; i++)
        {
            var it = items[i];
            var bgo = new GameObject("Item" + i);
            bgo.transform.SetParent(panel.transform, false);
            var brt = bgo.AddComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(0f, 70f - i * 110f);
            brt.sizeDelta = new Vector2(600f, 95f);
            var bimg = bgo.AddComponent<Image>();
            bimg.color = new Color(0.55f, 0.42f, 0.1f);
            var btn = bgo.AddComponent<Button>();
            btn.targetGraphic = bimg;
            buttons.Add(btn);

            MakeChildText(bgo.transform, it.label + "  【1コイン】", 24,
                Color.white, Vector2.zero, new Vector2(580f, 90f));

            btn.onClick.AddListener(() =>
            {
                if (coins <= 0) return;
                coins--;
                coinText.text = $"所持コイン：{coins}枚";
                it.effect();
                if (coins <= 0) done = true;
            });
        }

        // 閉店ボタン
        var closeGo = new GameObject("Close");
        closeGo.transform.SetParent(panel.transform, false);
        var closert = closeGo.AddComponent<RectTransform>();
        closert.anchoredPosition = new Vector2(0f, -245f);
        closert.sizeDelta = new Vector2(240f, 52f);
        var closeimg = closeGo.AddComponent<Image>();
        closeimg.color = new Color(0.35f, 0.35f, 0.4f);
        var closebtn = closeGo.AddComponent<Button>();
        closebtn.targetGraphic = closeimg;
        closebtn.onClick.AddListener(() => done = true);
        MakeChildText(closeGo.transform, "閉店する", 24, Color.white,
            Vector2.zero, new Vector2(220f, 45f));

        // 待機（15秒で自動閉店）
        float timeout = 15f;
        while (!done && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            if (bm != null && !bm.IsBattleActive) break;
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);
        Destroy(panel);
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }

    // ================================================================
    // 汎用パルス
    // ================================================================
    private IEnumerator CoPulse()
    {
        yield return LerpCanvasAlpha(1f, 0.3f, 0.12f);
        yield return LerpCanvasAlpha(0.3f, 1f, 0.18f);
    }

    // ================================================================
    // ユーティリティ
    // ================================================================
    private IEnumerator LerpCanvasAlpha(float from, float to, float duration)
    {
        if (_canvasGroup == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }

    private IEnumerator ShakeCanvas(float duration, float magnitude)
    {
        if (_canvasRt == null) yield break;
        Vector2 orig = _canvasRt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float str = magnitude * (1f - t / duration);
            _canvasRt.anchoredPosition = orig + new Vector2(
                Random.Range(-str, str), Random.Range(-str, str));
            yield return null;
        }
        _canvasRt.anchoredPosition = orig;
    }

    // ---- MetaCanvas ファクトリ ----
    private Image MakeMetaImage(Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("MetaImg");
        go.transform.SetParent(_metaRoot, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private Image MakeMetaOverlay(Color color)
    {
        var go = new GameObject("MetaOverlay");
        go.transform.SetParent(_metaRoot, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text MakeMetaText(string content, int size, Color color, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("MetaText");
        go.transform.SetParent(_metaRoot, false);
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
        outline.effectColor    = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2, -2);
        return t;
    }

    private Text MakeChildText(Transform parent, string content, int size, Color color,
        Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
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
        return t;
    }
}
