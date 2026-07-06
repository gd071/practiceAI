using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>会話1行分。</summary>
public struct DialogueLine
{
    public string speaker;
    public string text;
    public DialogueLine(string speaker, string text)
    {
        this.speaker = speaker;
        this.text    = text;
    }
}

/// <summary>
/// 会話ウィンドウ。名前欄＋タイプライター表示。
/// スペース / Enter / クリックで送り。全てコードで生成する。
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    public static bool IsPlaying     { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance  = null;
        IsPlaying = false;
    }

    private Canvas _canvas;
    private GameObject _window;
    private Text _nameText;
    private Text _bodyText;
    private Text _hintText;

    private bool _advanceRequested;

    // ================================================================
    void Awake()
    {
        // _Bootstrap に同居しているため gameObject ごと破壊してはいけない
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildUI();
    }

    void Update()
    {
        if (!IsPlaying) return;

        var kb = Keyboard.current;
        var ms = Mouse.current;
        bool pressed =
            (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame
                            || kb.eKey.wasPressedThisFrame)) ||
            (ms != null && ms.leftButton.wasPressedThisFrame);

        if (pressed) _advanceRequested = true;
    }

    // ================================================================
    private void BuildUI()
    {
        var go = new GameObject("DialogueCanvas");
        go.transform.SetParent(transform, false);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        // ウィンドウ
        _window = new GameObject("Window");
        _window.transform.SetParent(go.transform, false);
        var wrt = _window.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0.08f, 0.03f);
        wrt.anchorMax = new Vector2(0.92f, 0.26f);
        wrt.offsetMin = wrt.offsetMax = Vector2.zero;
        var wimg = _window.AddComponent<Image>();
        wimg.color = new Color(0.05f, 0.06f, 0.15f, 0.93f);

        // 枠線
        var frame = new GameObject("Frame");
        frame.transform.SetParent(_window.transform, false);
        var frt = frame.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(4, 4); frt.offsetMax = new Vector2(-4, -4);
        var fimg = frame.AddComponent<Image>();
        fimg.color = new Color(0f, 0f, 0f, 0f);
        var outline = frame.AddComponent<Outline>();
        outline.effectColor    = new Color(0.5f, 0.6f, 0.9f, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        // 名前欄
        var nameBg = new GameObject("NameBg");
        nameBg.transform.SetParent(_window.transform, false);
        var nrt = nameBg.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0.02f, 0.98f);
        nrt.anchorMax = new Vector2(0.02f, 0.98f);
        nrt.sizeDelta = new Vector2(320, 52);
        nrt.pivot     = new Vector2(0f, 0.5f);
        var nimg = nameBg.AddComponent<Image>();
        nimg.color = new Color(0.15f, 0.2f, 0.5f, 0.97f);

        _nameText = MakeText(nameBg.transform, "", 28, new Color(1f, 0.95f, 0.7f),
            TextAnchor.MiddleCenter);

        // 本文
        _bodyText = MakeText(_window.transform, "", 30, Color.white, TextAnchor.UpperLeft);
        var brt = _bodyText.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.03f, 0.10f);
        brt.anchorMax = new Vector2(0.97f, 0.86f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;

        // 送りヒント
        _hintText = MakeText(_window.transform, "▼ スペース / クリック", 20,
            new Color(0.7f, 0.75f, 0.95f), TextAnchor.LowerRight);
        var hrt = _hintText.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.6f, 0.02f);
        hrt.anchorMax = new Vector2(0.98f, 0.14f);
        hrt.offsetMin = hrt.offsetMax = Vector2.zero;

        _window.SetActive(false);
    }

    private Text MakeText(Transform parent, string content, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(10, 4); rt.offsetMax = new Vector2(-10, -4);
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = size;
        t.color     = color;
        t.alignment = anchor;
        t.raycastTarget = false;
        return t;
    }

    // ================================================================
    // 再生API
    // ================================================================
    public void Play(List<DialogueLine> lines, System.Action onComplete = null)
    {
        if (IsPlaying) return;
        StartCoroutine(CoPlay(lines, onComplete));
    }

    private IEnumerator CoPlay(List<DialogueLine> lines, System.Action onComplete)
    {
        IsPlaying = true;
        _window.SetActive(true);
        _advanceRequested = false;
        yield return null;   // 開始入力を食い潰さないよう1フレーム待つ
        _advanceRequested = false;

        foreach (var line in lines)
        {
            _nameText.text = line.speaker;
            _nameText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(line.speaker));

            // タイプライター
            _bodyText.text = "";
            _hintText.gameObject.SetActive(false);
            foreach (char ch in line.text)
            {
                _bodyText.text += ch;
                if (_advanceRequested)
                {
                    _bodyText.text = line.text;   // スキップで全文表示
                    _advanceRequested = false;
                    break;
                }
                yield return new WaitForSeconds(0.022f);
            }

            // 送り待ち
            _hintText.gameObject.SetActive(true);
            _advanceRequested = false;
            while (!_advanceRequested) yield return null;
            _advanceRequested = false;
        }

        _window.SetActive(false);
        IsPlaying = false;
        onComplete?.Invoke();
    }
}
