using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 必殺技発動時に BattleCanvas のパネル自体を動かす「UI破壊演出」を担当する。
/// BattleEffectManager がオーバーレイ演出、こちらがパネル変形演出を受け持つ。
/// </summary>
[DefaultExecutionOrder(-4)]
public class UIEffectManager : MonoBehaviour
{
    // BattleUI が Awake で生成するルートCanvas
    private RectTransform _canvasRt;

    // 操作対象パネル
    private RectTransform _topPanel;
    private RectTransform _logPanel;
    private RectTransform _bottomPanel;
    private RectTransform _allyArea;

    // ログテキスト（EventRewrite / GhostRush で使う）
    private Text _logText;

    // CanvasGroup（GhostRush で使う）
    private CanvasGroup _canvasGroup;

    // ================================================================
    void Start()
    {
        // BattleUI.Awake より後なので BattleCanvas は存在する
        var canvasGo = GameObject.Find("BattleCanvas");
        if (canvasGo == null) return;

        _canvasRt    = canvasGo.GetComponent<RectTransform>();
        _topPanel    = canvasGo.transform.Find("TopPanel")     as RectTransform;
        _logPanel    = canvasGo.transform.Find("LogPanel")     as RectTransform;
        _bottomPanel = canvasGo.transform.Find("BottomPanel")  as RectTransform;
        _allyArea    = canvasGo.transform.Find("AllyStatusArea") as RectTransform;

        var logTr = canvasGo.transform.Find("LogPanel/Text");
        if (logTr != null) _logText = logTr.GetComponent<Text>();

        // CanvasGroup を追加してアルファ全体操作を可能にする
        _canvasGroup = canvasGo.GetComponent<CanvasGroup>()
                    ?? canvasGo.AddComponent<CanvasGroup>();
    }

    void OnEnable()  => BattleManager.OnUltimateActivated += OnUltimate;
    void OnDisable() => BattleManager.OnUltimateActivated -= OnUltimate;

    // ================================================================
    private void OnUltimate(BattleUnit unit, UltimateType type)
    {
        StartCoroutine(PlayEffect(type));
    }

    private IEnumerator PlayEffect(UltimateType type)
    {
        switch (type)
        {
            case UltimateType.TimeStop:      yield return CoTimeStop();      break;
            case UltimateType.MemoryReplay:  yield return CoMemoryReplay();  break;
            case UltimateType.SpaceRift:     yield return CoSpaceRift();     break;
            case UltimateType.EventRewrite:  yield return CoEventRewrite();  break;
            case UltimateType.AbyssCall:     yield return CoAbyssCall();     break;
            case UltimateType.GhostRush:     yield return CoGhostRush();     break;
            default:                         yield return CoPulse();         break;
        }
    }

    // ================================================================
    // 時間断絶：UI全体が青白くフリーズしてガタつく
    // ================================================================
    private IEnumerator CoTimeStop()
    {
        // 全体を青白く染める
        yield return LerpCanvasAlpha(1f, 0.45f, 0.18f);

        // 画面ガタつき（時間が固まった衝撃）
        yield return ShakeCanvas(0.25f, 14f);

        // 青白い状態で静止
        yield return new WaitForSeconds(0.6f);

        // 元に戻す
        yield return LerpCanvasAlpha(0.45f, 1f, 0.22f);
    }

    // ================================================================
    // 記憶回廊：ログパネルが後ろに流れて過去のテキストがこだまする
    // ================================================================
    private IEnumerator CoMemoryReplay()
    {
        if (_logPanel == null) yield break;

        Vector2 origPos = _logPanel.anchoredPosition;

        // ログパネルを右に流す
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            _logPanel.anchoredPosition = origPos + new Vector2(Mathf.Lerp(0, 120f, t / 0.3f), 0);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // 元に戻す
        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            _logPanel.anchoredPosition = origPos + new Vector2(Mathf.Lerp(120f, 0, t / 0.25f), 0);
            yield return null;
        }
        _logPanel.anchoredPosition = origPos;
    }

    // ================================================================
    // 空間斬：上パネルと下パネルが物理的に引き裂かれる
    // ================================================================
    private IEnumerator CoSpaceRift()
    {
        if (_topPanel == null || _bottomPanel == null) yield break;

        Vector2 topOrig = _topPanel.anchoredPosition;
        Vector2 botOrig = _bottomPanel.anchoredPosition;

        // 引き裂き
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.deltaTime;
            float r = t / 0.22f;
            _topPanel.anchoredPosition    = topOrig + new Vector2(0,  r * 90f);
            _bottomPanel.anchoredPosition = botOrig + new Vector2(0, -r * 90f);
            yield return null;
        }

        // 裂けた状態で停止（暗黒が覗く）
        yield return new WaitForSeconds(0.35f);

        // 衝撃で戻る（オーバーシュート気味に）
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

        // バウンス
        yield return ShakeCanvas(0.2f, 8f);
    }

    // ================================================================
    // 事象書換：ログテキストが文字化けして書き直される
    // ================================================================
    private IEnumerator CoEventRewrite()
    {
        if (_logText == null) yield break;

        string original = _logText.text;
        const string pool = "あいうえおかきくけこ０１２３！？#＊▲△□■※";

        // スクランブル
        for (int i = 0; i < 18; i++)
        {
            var sb = new StringBuilder();
            int len = Random.Range(8, 22);
            for (int c = 0; c < len; c++)
                sb.Append(pool[Random.Range(0, pool.Length)]);
            _logText.text = sb.ToString();
            yield return new WaitForSeconds(0.055f);
        }

        // 書き戻し（1文字ずつ）
        _logText.text = "";
        foreach (char ch in original)
        {
            _logText.text += ch;
            yield return new WaitForSeconds(0.018f);
        }
    }

    // ================================================================
    // 深淵召喚：UI全体がゆっくり沈んでいき暗闇に飲まれる
    // ================================================================
    private IEnumerator CoAbyssCall()
    {
        if (_canvasRt == null) yield break;

        Vector2 origPos = _canvasRt.anchoredPosition;

        // アルファを落としながら沈む
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float r = t / 0.5f;
            _canvasRt.anchoredPosition = origPos + new Vector2(0, -r * 60f);
            if (_canvasGroup != null) _canvasGroup.alpha = 1f - r * 0.6f;
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        // 浮上して戻る
        t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float r = t / 0.3f;
            _canvasRt.anchoredPosition = origPos + new Vector2(0, -60f * (1f - r));
            if (_canvasGroup != null) _canvasGroup.alpha = 0.4f + r * 0.6f;
            yield return null;
        }
        _canvasRt.anchoredPosition = origPos;
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }

    // ================================================================
    // 霊体突撃：UI全体が半透明になって激しくちらつく
    // ================================================================
    private IEnumerator CoGhostRush()
    {
        if (_canvasGroup == null) yield break;

        float elapsed = 0f;
        const float duration = 1.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // sin で激しくちらつかせる
            float t = elapsed / duration;
            _canvasGroup.alpha = Mathf.Lerp(0.15f, 0.85f,
                (Mathf.Sin(elapsed * 28f) + 1f) * 0.5f) * (1f - t * 0.3f);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }

    // ================================================================
    // 汎用：UIがどくっとパルスする（未割当タイプ用）
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
}
