using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// バトル画面の全UIをコードで生成・管理する。
/// 砂時計カウントバー・9人パーティのキャラカード（クリックで行動キャラ選択）・
/// コマンドボタン・バトルログ・ループ周回表示。
/// </summary>
[DefaultExecutionOrder(-10)]
public class BattleUI : MonoBehaviour
{
    // ---- 内部クラス ----
    private class UnitStatusUI
    {
        public GameObject root;
        public Image       bg;
        public Text        nameText;
        public Image       hpFill;
        public Image       mpFill;
        public Text        hpValueText;
    }

    // ---- フィールド ----
    private Canvas     _canvas;
    private Text       _turnText;
    private Text       _logText;
    private Text       _countText;
    private Image      _countFill;
    private Text       _loopText;
    private GameObject _commandPanel;
    private bool       _inputEnabled;

    private readonly Dictionary<BattleUnit, UnitStatusUI> _statusMap =
        new Dictionary<BattleUnit, UnitStatusUI>();

    private readonly List<string> _logLines = new List<string>();
    private const int MAX_LOG = 7;

    // ================================================================
    // Unity ライフサイクル
    // ================================================================
    void Awake()
    {
        EnsureEventSystem();
        BuildCanvas();
    }

    void OnEnable()
    {
        BattleManager.OnBattleStarted     += HandleBattleStarted;
        BattleUnit.OnDamageReceived       += HandleDamage;
        BattleUnit.OnUnitDefeated         += HandleDefeated;
        BattleUnit.OnHealed               += HandleHealed;
        BattleUnit.OnMPChanged            += HandleMPChanged;
        TurnManager.OnTurnStarted         += HandleTurnStarted;
        TurnManager.OnPhaseChanged        += HandlePhaseChanged;
        TurnManager.OnCountChanged        += HandleCountChanged;
        TurnManager.OnNewRoundStarted     += HandleNewRound;
        BattleManager.OnBattleEnded       += HandleBattleEnded;
        BattleManager.OnBattleLog         += AddLog;
        BattleManager.OnUltimateActivated += HandleUltimate;
    }

    void OnDisable()
    {
        BattleManager.OnBattleStarted     -= HandleBattleStarted;
        BattleUnit.OnDamageReceived       -= HandleDamage;
        BattleUnit.OnUnitDefeated         -= HandleDefeated;
        BattleUnit.OnHealed               -= HandleHealed;
        BattleUnit.OnMPChanged            -= HandleMPChanged;
        TurnManager.OnTurnStarted         -= HandleTurnStarted;
        TurnManager.OnPhaseChanged        -= HandlePhaseChanged;
        TurnManager.OnCountChanged        -= HandleCountChanged;
        TurnManager.OnNewRoundStarted     -= HandleNewRound;
        BattleManager.OnBattleEnded       -= HandleBattleEnded;
        BattleManager.OnBattleLog         -= AddLog;
        BattleManager.OnUltimateActivated -= HandleUltimate;
    }

    // ================================================================
    // Canvas 構築
    // ================================================================
    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();

        var inputModuleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null)
            es.AddComponent(inputModuleType);
        else
            es.AddComponent<StandaloneInputModule>();
    }

    private void BuildCanvas()
    {
        var go = new GameObject("BattleCanvas");
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        BuildBackground();
        BuildTopPanel();
        BuildCountBar();
        BuildLogPanel();
        BuildBottomPanel();
    }

    private void BuildBackground()
    {
        // 透明背景（ワールド空間のキャラクターを見せる）
        var img = MakeAnchoredImage("Background", _canvas.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0f));
        img.raycastTarget = false;
    }

    // ---- 上部パネル（敵ステータス + ターン表示 + 周回数） ----
    private void BuildTopPanel()
    {
        MakeAnchoredImage("TopPanel", _canvas.transform,
            new Vector2(0, 0.85f), Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.08f, 0.08f, 0.18f, 0.9f));

        var topTr = _canvas.transform.Find("TopPanel");

        _turnText = MakeText(topTr, "─ バトル開始 ─", 26, Color.yellow,
            TextAnchor.MiddleCenter,
            new Vector2(0.30f, 0f), new Vector2(0.70f, 0.35f),
            Vector2.zero, Vector2.zero);

        // ループ周回数（左上）
        _loopText = MakeText(_canvas.transform, "", 24, new Color(0.75f, 0.75f, 0.95f),
            TextAnchor.MiddleLeft,
            new Vector2(0.005f, 0.79f), new Vector2(0.18f, 0.845f),
            new Vector2(8, 0), Vector2.zero);
    }

    // ---- 砂時計カウントバー ----
    private void BuildCountBar()
    {
        var barBg = MakeAnchoredImage("CountBar", _canvas.transform,
            new Vector2(0.28f, 0.795f), new Vector2(0.72f, 0.835f),
            Vector2.zero, Vector2.zero,
            new Color(0.12f, 0.12f, 0.18f, 0.95f));
        barBg.raycastTarget = false;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barBg.transform, false);
        var frt = fillGo.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(3, 3); frt.offsetMax = new Vector2(-3, -3);
        _countFill = fillGo.AddComponent<Image>();
        _countFill.color      = new Color(0.95f, 0.8f, 0.3f);
        _countFill.type       = Image.Type.Filled;
        _countFill.fillMethod = Image.FillMethod.Horizontal;
        _countFill.fillAmount = 1f;
        _countFill.raycastTarget = false;

        _countText = MakeText(barBg.transform, "砂時計 100 / 100", 22, Color.white,
            TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    // ---- バトルログ ----
    private void BuildLogPanel()
    {
        MakeAnchoredImage("LogPanel", _canvas.transform,
            new Vector2(0, 0.24f), new Vector2(0.44f, 0.50f), Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.62f));

        var logTr = _canvas.transform.Find("LogPanel");

        _logText = MakeText(logTr, "", 18, new Color(0.9f, 0.9f, 0.9f),
            TextAnchor.LowerLeft,
            Vector2.zero, Vector2.one,
            new Vector2(12, 6), new Vector2(-12, -6));
    }

    // ---- 下部パネル（味方カード + コマンド） ----
    private void BuildBottomPanel()
    {
        MakeAnchoredImage("BottomPanel", _canvas.transform,
            Vector2.zero, new Vector2(1f, 0.24f), Vector2.zero, Vector2.zero,
            new Color(0.06f, 0.06f, 0.14f, 0.97f));

        BuildCommandButtons();
    }

    private void BuildCommandButtons()
    {
        var panel = new GameObject("CommandPanel");
        panel.transform.SetParent(_canvas.transform.Find("BottomPanel"), false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.66f, 0f);
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.22f, 0.5f);
        _commandPanel = panel;

        var cmds = new (string label, BattleCommand cmd)[]
        {
            ($"攻撃 [{BattleManager.CostAttack}]",   BattleCommand.Attack),
            ($"スキル [{BattleManager.CostSkill}]",  BattleCommand.Skill),
            ($"必殺技 [{BattleManager.CostUltimate}]", BattleCommand.Ultimate),
            ($"パス [+{BattleManager.PassGain}]",    BattleCommand.Pass),
            ($"救急 [{BattleManager.CostItem}]",     BattleCommand.Item),
        };

        float w = 200f, h = 62f, padX = 12f, padY = 12f;
        for (int i = 0; i < cmds.Length; i++)
        {
            int col = i % 3, row = i / 3;
            float x = padX + col * (w + 8f);
            float y = -(padY + row * (h + 8f));
            var captured = cmds[i].cmd;
            BuildOneButton(panel.transform, cmds[i].label, x, y, w, h,
                new Color(0.18f, 0.28f, 0.48f),
                () =>
                {
                    if (_inputEnabled)
                        BattleManager.Instance?.SelectCommand(captured);
                });
        }

        // ターン終了ボタン
        BuildOneButton(panel.transform, "▶ ターン終了",
            padX + 2 * (w + 8f), -(padY + h + 8f), w, h,
            new Color(0.45f, 0.20f, 0.20f),
            () =>
            {
                if (_inputEnabled)
                    BattleManager.Instance?.EndPlayerPhase();
            });
    }

    private void BuildOneButton(Transform parent, string label,
        float x, float y, float w, float h, Color baseColor, System.Action onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x + w * 0.5f, y - h * 0.5f);
        rt.sizeDelta = new Vector2(w, h);

        var img = go.AddComponent<Image>();
        img.color = baseColor;

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = baseColor;
        cb.highlightedColor = baseColor * 1.6f;
        cb.pressedColor     = baseColor * 0.6f;
        cb.disabledColor    = new Color(0.12f, 0.12f, 0.14f);
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick());

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = textGo.AddComponent<Text>();
        t.text      = label;
        t.fontSize  = 22;
        t.color     = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = GetDefaultFont();
        t.raycastTarget = false;
    }

    // ================================================================
    // ユニットステータス生成（バトル開始後）
    // ================================================================
    private void HandleBattleStarted(List<BattleUnit> allies, List<BattleUnit> enemies)
    {
        BuildAllyCards(allies);
        BuildEnemyStatus(enemies);
        _loopText.text = $"◆ {BattleManager.LoopNumber}周目";
        SetInput(false);
    }

    /// <summary>味方9人のカード（クリックで行動キャラ選択）。2段組。</summary>
    private void BuildAllyCards(List<BattleUnit> allies)
    {
        var area = new GameObject("AllyStatusArea");
        area.transform.SetParent(_canvas.transform, false);
        var rt = area.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(0.66f, 0.24f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        float cw = 238f, ch = 118f, pad = 8f;
        int perRow = 5;

        for (int i = 0; i < allies.Count; i++)
        {
            int col = i % perRow, row = i / perRow;
            var ui = BuildAllyCard(area.transform, allies[i],
                pad + col * (cw + pad), -(pad + row * (ch + pad)), cw, ch);
            _statusMap[allies[i]] = ui;
        }
    }

    private UnitStatusUI BuildAllyCard(Transform parent, BattleUnit unit,
        float x, float y, float w, float h)
    {
        var ui = new UnitStatusUI();

        var root = new GameObject("Card_" + unit.UnitName);
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x + w * 0.5f, y - h * 0.5f);
        rt.sizeDelta = new Vector2(w, h);

        ui.bg = root.AddComponent<Image>();
        ui.bg.color = new Color(0.08f, 0.08f, 0.28f, 0.88f);
        ui.root = root;

        // クリックで行動キャラ選択
        var btn = root.AddComponent<Button>();
        btn.targetGraphic = ui.bg;
        var captured = unit;
        btn.onClick.AddListener(() =>
        {
            if (_inputEnabled)
                BattleManager.Instance?.SelectActor(captured);
        });

        // 名前
        ui.nameText = MakeText(root.transform, unit.UnitName, 17, Color.white,
            TextAnchor.MiddleLeft,
            new Vector2(0f, 0.66f), new Vector2(1f, 1f),
            new Vector2(8, 0), Vector2.zero);

        // HP
        MakeText(root.transform, "HP", 13, new Color(0.7f, 1f, 0.7f), TextAnchor.MiddleLeft,
            new Vector2(0f, 0.38f), new Vector2(0.13f, 0.64f), new Vector2(8, 0), Vector2.zero);
        var hpBg = MakeBarBg(root.transform,
            new Vector2(0.13f, 0.42f), new Vector2(0.72f, 0.62f));
        ui.hpFill = MakeBarFill(hpBg.transform, new Color(0.2f, 0.78f, 0.2f));
        ui.hpValueText = MakeText(root.transform,
            unit.CurrentHP + "/" + unit.Data.maxHP, 13, Color.white,
            TextAnchor.MiddleLeft,
            new Vector2(0.73f, 0.38f), new Vector2(1f, 0.64f),
            new Vector2(3, 0), Vector2.zero);

        // MP
        MakeText(root.transform, "MP", 13, new Color(0.7f, 0.7f, 1f), TextAnchor.MiddleLeft,
            new Vector2(0f, 0.10f), new Vector2(0.13f, 0.36f), new Vector2(8, 0), Vector2.zero);
        var mpBg = MakeBarBg(root.transform,
            new Vector2(0.13f, 0.14f), new Vector2(0.72f, 0.34f));
        ui.mpFill = MakeBarFill(mpBg.transform, new Color(0.25f, 0.45f, 0.90f));

        return ui;
    }

    private void BuildEnemyStatus(List<BattleUnit> enemies)
    {
        var topTr = _canvas.transform.Find("TopPanel");
        float sw = 330f, sh = 62f, pad = 10f;

        for (int i = 0; i < enemies.Count; i++)
        {
            var unit = enemies[i];
            var ui   = new UnitStatusUI();

            var root = new GameObject("Status_" + unit.UnitName);
            root.transform.SetParent(topTr, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(sw, sh);
            rt.anchoredPosition = new Vector2(pad + i * (sw + pad) + sw * 0.5f, -(pad + sh * 0.5f));

            ui.bg = root.AddComponent<Image>();
            ui.bg.color = new Color(0.28f, 0.08f, 0.08f, 0.85f);
            ui.root = root;

            ui.nameText = MakeText(root.transform, unit.UnitName, 17, Color.white,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 0.52f), new Vector2(1f, 1f),
                new Vector2(8, 0), Vector2.zero);

            var hpBg = MakeBarBg(root.transform,
                new Vector2(0.04f, 0.14f), new Vector2(0.78f, 0.46f));
            ui.hpFill = MakeBarFill(hpBg.transform, new Color(0.85f, 0.25f, 0.2f));

            ui.hpValueText = MakeText(root.transform,
                unit.CurrentHP + "/" + unit.Data.maxHP, 13, Color.white,
                TextAnchor.MiddleLeft,
                new Vector2(0.79f, 0.10f), new Vector2(1f, 0.48f),
                new Vector2(3, 0), Vector2.zero);

            _statusMap[unit] = ui;
        }
    }

    // ================================================================
    // イベントハンドラ
    // ================================================================
    private void HandleDamage(BattleUnit unit, int damage) => RefreshStatus(unit);
    private void HandleHealed(BattleUnit unit, int amount) => RefreshStatus(unit);
    private void HandleMPChanged(BattleUnit unit)          => RefreshStatus(unit);

    private void HandleDefeated(BattleUnit unit)
    {
        if (_statusMap.TryGetValue(unit, out var ui))
        {
            ui.nameText.color = new Color(0.45f, 0.45f, 0.45f);
            if (ui.hpFill != null) { ui.hpFill.color = new Color(0.35f, 0.35f, 0.35f); ui.hpFill.fillAmount = 0f; }
            ui.bg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        }
    }

    private void HandleTurnStarted(BattleUnit unit)
    {
        if (unit == null) return;

        _turnText.text = unit.IsEnemy
            ? "► " + unit.UnitName + " の行動！"
            : "► " + unit.UnitName + " を選択中";
        _turnText.color = unit.IsEnemy ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 0.9f, 1f);

        // 選択中/行動中のカードをハイライト
        foreach (var kv in _statusMap)
        {
            if (kv.Value.bg == null || !kv.Key.IsAlive) continue;
            bool active = kv.Key == unit, enemy = kv.Key.IsEnemy;
            kv.Value.bg.color = active
                ? (enemy ? new Color(0.60f, 0.16f, 0.16f, 0.97f)
                         : new Color(0.18f, 0.32f, 0.65f, 0.97f))
                : (enemy ? new Color(0.28f, 0.08f, 0.08f, 0.85f)
                         : new Color(0.08f, 0.08f, 0.28f, 0.88f));
        }
    }

    private void HandlePhaseChanged(TurnPhase phase)
    {
        SetInput(phase == TurnPhase.PlayerInput);
    }

    private void HandleCountChanged(int current, int max)
    {
        if (_countFill != null) _countFill.fillAmount = Mathf.Clamp01((float)current / max);
        if (_countText != null) _countText.text = $"砂時計 {current} / {max}";

        if (_countFill != null)
        {
            float r = (float)current / max;
            _countFill.color = r > 0.5f  ? new Color(0.95f, 0.8f, 0.3f)
                             : r > 0.25f ? new Color(0.95f, 0.55f, 0.2f)
                             : new Color(0.9f, 0.25f, 0.15f);
        }
    }

    private void HandleNewRound(int round)
    {
        AddLog($"── ラウンド {round} ──");
    }

    private void HandleUltimate(BattleUnit unit, UltimateType type)
    {
        _turnText.text  = "★★ " + unit.Data.ultimateName + " ★★";
        _turnText.color = new Color(1f, 0.85f, 0f);
        StartCoroutine(ResetTurnColor(1.5f, new Color(0.6f, 0.9f, 1f)));
    }

    private void HandleBattleEnded(BattleResult result)
    {
        SetInput(false);
        if (result == BattleResult.Victory)
        {
            _turnText.text  = "★ 真エンディング ★";
            _turnText.color = Color.yellow;
            StartCoroutine(CoVictoryBanner());
        }
        else
        {
            _turnText.text  = "── ループ ──";
            _turnText.color = new Color(0.7f, 0.6f, 1f);
        }
    }

    private IEnumerator CoVictoryBanner()
    {
        yield return new WaitForSeconds(0.8f);

        var overlay = MakeAnchoredImage("VictoryOverlay", _canvas.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0f));
        overlay.raycastTarget = false;

        float t = 0f;
        while (t < 2.0f)
        {
            t += Time.deltaTime;
            overlay.color = new Color(1f, 1f, 1f, Mathf.Min(0.55f, t / 2.0f * 0.55f));
            yield return null;
        }

        var banner = MakeText(_canvas.transform,
            "全ての選択を、成功させた。\n\n隕石は砕け、197周のループは終わりを告げる。\n\n── 真エンディング ──",
            42, new Color(0.15f, 0.1f, 0.05f), TextAnchor.MiddleCenter,
            new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.75f), Vector2.zero, Vector2.zero);
        banner.fontStyle = FontStyle.Bold;
    }

    // ================================================================
    // ヘルパー
    // ================================================================
    private void RefreshStatus(BattleUnit unit)
    {
        if (!_statusMap.TryGetValue(unit, out var ui)) return;
        if (!unit.IsAlive) return;

        float hpRatio = (float)unit.CurrentHP / unit.Data.maxHP;
        ui.hpFill.fillAmount = hpRatio;
        if (!unit.IsEnemy)
        {
            ui.hpFill.color = hpRatio > 0.5f ? new Color(0.2f, 0.78f, 0.2f)
                            : hpRatio > 0.25f ? new Color(1f, 0.78f, 0.1f)
                            : new Color(0.9f, 0.18f, 0.1f);
        }

        if (ui.hpValueText != null)
            ui.hpValueText.text = unit.CurrentHP + "/" + unit.Data.maxHP;

        if (ui.mpFill != null)
            ui.mpFill.fillAmount = (float)unit.CurrentMP / unit.Data.maxMP;
    }

    private void AddLog(string msg)
    {
        _logLines.Add(msg);
        if (_logLines.Count > MAX_LOG) _logLines.RemoveAt(0);
        if (_logText != null) _logText.text = string.Join("\n", _logLines);
    }

    private void SetInput(bool enabled)
    {
        _inputEnabled = enabled;
        if (_commandPanel == null) return;
        foreach (Transform child in _commandPanel.transform)
        {
            var btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = enabled;
        }
    }

    private IEnumerator ResetTurnColor(float delay, Color col)
    {
        yield return new WaitForSeconds(delay);
        if (_turnText != null) _turnText.color = col;
    }

    // ================================================================
    // UI ファクトリ
    // ================================================================
    private Image MakeAnchoredImage(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private Text MakeText(Transform parent, string content, int size, Color color,
        TextAnchor anchor, Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.fontSize  = size;
        t.color     = color;
        t.alignment = anchor;
        t.font      = GetDefaultFont();
        t.raycastTarget = false;
        return t;
    }

    private Image MakeBarBg(Transform parent, Vector2 ancMin, Vector2 ancMax)
    {
        var go = new GameObject("BarBg");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.18f);
        img.raycastTarget = false;
        return img;
    }

    private Image MakeBarFill(Transform parent, Color color)
    {
        var go = new GameObject("Fill");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color      = color;
        img.type       = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = 1f;
        img.raycastTarget = false;
        return img;
    }

    private static Font GetDefaultFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
