using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// バトル画面の全UIをコードで生成・管理する。
/// BattleBootstrap.Start より先に Awake/OnEnable が完了する。
/// </summary>
[DefaultExecutionOrder(-10)]
public class BattleUI : MonoBehaviour
{
    // ---- 内部クラス ----
    private class UnitStatusUI
    {
        public GameObject root;
        public Text        nameText;
        public Image       hpFill;
        public Image       mpFill;
        public Text        hpValueText;
    }

    // ---- フィールド ----
    private Canvas     _canvas;
    private Text       _turnText;
    private Text       _logText;
    private GameObject _commandPanel;
    private bool       _inputEnabled;

    private readonly Dictionary<BattleUnit, UnitStatusUI> _statusMap =
        new Dictionary<BattleUnit, UnitStatusUI>();

    private readonly List<string> _logLines = new List<string>();
    private const int MAX_LOG = 6;

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

        // 新 Input System が有効な場合は InputSystemUIInputModule を使う。
        // なければ StandaloneInputModule にフォールバック。
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
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight   = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        BuildBackground();
        BuildTopPanel();
        BuildLogPanel();
        BuildBottomPanel();
    }

    // ---- 背景 ----
    private void BuildBackground()
    {
        // 全画面を透明にしてワールド空間のキャラクターを見せる。
        // カメラが SolidColor(濃紺)でクリアするので暗い背景は担保される。
        var img = MakeAnchoredImage("Background", _canvas.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0f));
        img.raycastTarget = false;
    }

    // ---- 上部パネル（敵ステータス + ターン表示） ----
    private void BuildTopPanel()
    {
        MakeAnchoredImage("TopPanel", _canvas.transform,
            new Vector2(0, 0.82f), Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.08f, 0.08f, 0.18f, 0.9f));

        var topTr = _canvas.transform.Find("TopPanel");

        _turnText = MakeText(topTr, "─ バトル開始 ─", 26, Color.yellow,
            TextAnchor.MiddleCenter,
            new Vector2(0.25f, 0f), new Vector2(0.75f, 1f),
            Vector2.zero, Vector2.zero);
    }

    // ---- バトルログ ----
    private void BuildLogPanel()
    {
        MakeAnchoredImage("LogPanel", _canvas.transform,
            new Vector2(0, 0.20f), new Vector2(1, 0.38f), Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.65f));

        var logTr = _canvas.transform.Find("LogPanel");

        _logText = MakeText(logTr, "", 19, new Color(0.88f, 0.88f, 0.88f),
            TextAnchor.LowerLeft,
            Vector2.zero, Vector2.one,
            new Vector2(12, 6), new Vector2(-12, -6));
    }

    // ---- 下部パネル（味方ステータス + コマンド） ----
    private void BuildBottomPanel()
    {
        MakeAnchoredImage("BottomPanel", _canvas.transform,
            Vector2.zero, new Vector2(1f, 0.20f), Vector2.zero, Vector2.zero,
            new Color(0.06f, 0.06f, 0.14f, 0.97f));

        BuildCommandButtons();
    }

    private void BuildCommandButtons()
    {
        var panel = new GameObject("CommandPanel");
        panel.transform.SetParent(_canvas.transform.Find("BottomPanel"), false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.62f, 0f);
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.22f, 0.5f);
        _commandPanel = panel;

        var cmds = new (string label, BattleCommand cmd)[]
        {
            ("攻撃",   BattleCommand.Attack),
            ("スキル", BattleCommand.Skill),
            ("必殺技", BattleCommand.Ultimate),
            ("パス",   BattleCommand.Pass),
            ("アイテム", BattleCommand.Item),
        };

        float w = 160f, h = 48f, padX = 12f, padY = 10f;
        for (int i = 0; i < cmds.Length; i++)
        {
            int col = i % 2, row = i / 2;
            float x = padX + col * (w + 8f);
            float y = -(padY + row * (h + 6f));
            BuildOneButton(panel.transform, cmds[i].label, cmds[i].cmd, x, y, w, h);
        }
    }

    private void BuildOneButton(Transform parent, string label, BattleCommand cmd,
        float x, float y, float w, float h)
    {
        var go = new GameObject("Btn_" + cmd);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x + w * 0.5f, y - h * 0.5f);
        rt.sizeDelta = new Vector2(w, h);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.28f, 0.48f);

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = new Color(0.18f, 0.28f, 0.48f);
        cb.highlightedColor = new Color(0.30f, 0.48f, 0.80f);
        cb.pressedColor     = new Color(0.10f, 0.18f, 0.36f);
        cb.disabledColor    = new Color(0.12f, 0.12f, 0.14f);
        btn.colors = cb;
        btn.targetGraphic = img;

        var captured = cmd;
        btn.onClick.AddListener(() =>
        {
            if (_inputEnabled)
                BattleManager.Instance?.SelectCommand(captured);
        });

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
    }

    // ================================================================
    // ユニットステータスバー生成（バトル開始後）
    // ================================================================
    private void HandleBattleStarted(List<BattleUnit> allies, List<BattleUnit> enemies)
    {
        BuildAllyStatus(allies);
        BuildEnemyStatus(enemies);
        SetInput(false);
    }

    private void BuildAllyStatus(List<BattleUnit> allies)
    {
        var area = new GameObject("AllyStatusArea");
        area.transform.SetParent(_canvas.transform, false);
        var rt = area.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(0.60f, 0.20f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        for (int i = 0; i < allies.Count; i++)
            _statusMap[allies[i]] = BuildStatusBar(area.transform, allies[i], i, false);
    }

    private void BuildEnemyStatus(List<BattleUnit> enemies)
    {
        var topTr = _canvas.transform.Find("TopPanel");
        for (int i = 0; i < enemies.Count; i++)
            _statusMap[enemies[i]] = BuildStatusBar(topTr, enemies[i], i, true);
    }

    private UnitStatusUI BuildStatusBar(Transform parent, BattleUnit unit, int index, bool isEnemy)
    {
        var ui   = new UnitStatusUI();
        float sw = 270f, sh = 68f, pad = 8f;

        var root = new GameObject("Status_" + unit.UnitName);
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = isEnemy ? new Vector2(0, 1) : new Vector2(0, 1);
        rt.sizeDelta = new Vector2(sw, sh);
        rt.anchoredPosition = new Vector2(
            pad + index * (sw + pad) + sw * 0.5f,
            -(pad + sh * 0.5f));

        var bg = root.AddComponent<Image>();
        bg.color = isEnemy
            ? new Color(0.28f, 0.08f, 0.08f, 0.85f)
            : new Color(0.08f, 0.08f, 0.28f, 0.85f);
        ui.root = root;

        // 名前
        ui.nameText = MakeText(root.transform, unit.UnitName, 16, Color.white,
            TextAnchor.MiddleLeft,
            new Vector2(0f, 0.60f), new Vector2(0.75f, 1f),
            new Vector2(8, 0), Vector2.zero);

        // HP
        MakeText(root.transform, "HP", 13, new Color(0.7f, 1f, 0.7f), TextAnchor.MiddleLeft,
            new Vector2(0f, 0.30f), new Vector2(0.14f, 0.62f), new Vector2(8, 0), Vector2.zero);

        var hpBg = MakeBarBg(root.transform,
            new Vector2(0.14f, 0.35f), new Vector2(0.82f, 0.60f));
        ui.hpFill = MakeBarFill(hpBg.transform, new Color(0.2f, 0.78f, 0.2f));

        ui.hpValueText = MakeText(root.transform,
            unit.CurrentHP + "/" + unit.Data.maxHP, 12, Color.white,
            TextAnchor.MiddleLeft,
            new Vector2(0.83f, 0.30f), new Vector2(1f, 0.62f),
            new Vector2(3, 0), Vector2.zero);

        if (!isEnemy)
        {
            // MP
            MakeText(root.transform, "MP", 13, new Color(0.7f, 0.7f, 1f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0.02f), new Vector2(0.14f, 0.32f), new Vector2(8, 0), Vector2.zero);

            var mpBg = MakeBarBg(root.transform,
                new Vector2(0.14f, 0.06f), new Vector2(0.82f, 0.30f));
            ui.mpFill = MakeBarFill(mpBg.transform, new Color(0.25f, 0.45f, 0.90f));
        }

        return ui;
    }

    // ================================================================
    // イベントハンドラ
    // ================================================================
    private void HandleDamage(BattleUnit unit, int damage)
    {
        RefreshStatus(unit);
    }

    private void HandleDefeated(BattleUnit unit)
    {
        if (_statusMap.TryGetValue(unit, out var ui))
        {
            ui.nameText.color = new Color(0.45f, 0.45f, 0.45f);
            if (ui.hpFill != null) ui.hpFill.color = new Color(0.35f, 0.35f, 0.35f);
            if (ui.hpFill != null) ui.hpFill.fillAmount = 0f;
        }
    }

    private void HandleHealed(BattleUnit unit, int amount) => RefreshStatus(unit);
    private void HandleMPChanged(BattleUnit unit)          => RefreshStatus(unit);

    private void HandleTurnStarted(BattleUnit unit)
    {
        _turnText.text = "► " + unit.UnitName + " のターン";
        _turnText.color = unit.IsEnemy ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 0.9f, 1f);

        foreach (var kv in _statusMap)
        {
            var bg = kv.Value.root.GetComponent<Image>();
            if (bg == null) continue;
            bool active = kv.Key == unit, enemy = kv.Key.IsEnemy;
            bg.color = active
                ? (enemy ? new Color(0.55f, 0.15f, 0.15f, 0.95f)
                         : new Color(0.15f, 0.25f, 0.60f, 0.95f))
                : (enemy ? new Color(0.28f, 0.08f, 0.08f, 0.85f)
                         : new Color(0.08f, 0.08f, 0.28f, 0.85f));
        }
    }

    private void HandlePhaseChanged(TurnPhase phase)
    {
        SetInput(phase == TurnPhase.PlayerInput);
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
            _turnText.text  = "★ 勝利！ ★";
            _turnText.color = Color.yellow;
            AddLog("全ての敵を倒した！勝利！");
        }
        else
        {
            _turnText.text  = "× 敗北...";
            _turnText.color = new Color(1f, 0.3f, 0.3f);
            AddLog("パーティは全滅した...");
        }
    }

    // ================================================================
    // ヘルパー
    // ================================================================
    private void RefreshStatus(BattleUnit unit)
    {
        if (!_statusMap.TryGetValue(unit, out var ui)) return;

        float hpRatio = (float)unit.CurrentHP / unit.Data.maxHP;
        ui.hpFill.fillAmount = hpRatio;
        ui.hpFill.color = hpRatio > 0.5f ? new Color(0.2f, 0.78f, 0.2f)
                        : hpRatio > 0.25f ? new Color(1f, 0.78f, 0.1f)
                        : new Color(0.9f, 0.18f, 0.1f);

        if (ui.hpValueText != null)
            ui.hpValueText.text = unit.CurrentHP + "/" + unit.Data.maxHP;

        if (ui.mpFill != null)
            ui.mpFill.fillAmount = (float)unit.CurrentMP / unit.Data.maxMP;
    }

    private void AddLog(string msg)
    {
        _logLines.Add(msg);
        if (_logLines.Count > MAX_LOG) _logLines.RemoveAt(0);
        _logText.text = string.Join("\n", _logLines);
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
