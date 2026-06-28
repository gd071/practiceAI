using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// バトル中の視覚演出を管理する。
/// ダメージ・回復ポップアップ、ユニットフラッシュ、必殺技エフェクト。
/// </summary>
[DefaultExecutionOrder(-5)]
public class BattleEffectManager : MonoBehaviour
{
    private Canvas        _effectCanvas;
    private RectTransform _effectRoot;

    // ================================================================
    void Awake()
    {
        BuildEffectCanvas();
    }

    void OnEnable()
    {
        BattleUnit.OnDamageReceived       += HandleDamage;
        BattleUnit.OnHealed               += HandleHeal;
        BattleManager.OnUltimateActivated += HandleUltimate;
    }

    void OnDisable()
    {
        BattleUnit.OnDamageReceived       -= HandleDamage;
        BattleUnit.OnHealed               -= HandleHeal;
        BattleManager.OnUltimateActivated -= HandleUltimate;
    }

    // ================================================================
    // Canvas 構築
    // ================================================================
    private void BuildEffectCanvas()
    {
        var go = new GameObject("EffectCanvas");
        _effectCanvas = go.AddComponent<Canvas>();
        _effectCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _effectCanvas.sortingOrder = 20; // BattleUI(10) より前面

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        _effectRoot = go.GetComponent<RectTransform>();
    }

    // ================================================================
    // イベントハンドラ
    // ================================================================
    private void HandleDamage(BattleUnit unit, int damage)
    {
        StartCoroutine(FlashUnit(unit, new Color(1f, 0.15f, 0.15f)));
        StartCoroutine(FloatText("-" + damage, unit.transform.position,
                                 new Color(1f, 0.25f, 0.25f)));
    }

    private void HandleHeal(BattleUnit unit, int amount)
    {
        StartCoroutine(FloatText("+" + amount + " HP", unit.transform.position,
                                 new Color(0.25f, 1f, 0.4f)));
    }

    private void HandleUltimate(BattleUnit unit, UltimateType type)
    {
        StartCoroutine(PlayUltimateEffect(unit, type));
    }

    // ================================================================
    // ユニットフラッシュ
    // ================================================================
    private IEnumerator FlashUnit(BattleUnit unit, Color flashColor,
        float onDuration = 0.10f, float offDuration = 0.07f, int count = 2)
    {
        var sr = unit.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color original = sr.color;

        for (int i = 0; i < count; i++)
        {
            sr.color = flashColor;
            yield return new WaitForSeconds(onDuration);
            // 撃破済みなら元の色に戻さない
            sr.color = unit.IsAlive ? original : new Color(0.35f, 0.35f, 0.35f);
            if (i < count - 1) yield return new WaitForSeconds(offDuration);
        }
    }

    // ================================================================
    // 浮かび上がるテキスト
    // ================================================================
    private IEnumerator FloatText(string msg, Vector3 worldPos, Color color)
    {
        var go = new GameObject("FloatText");
        go.transform.SetParent(_effectCanvas.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 70);

        var txt = go.AddComponent<Text>();
        txt.text       = msg;
        txt.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize   = 36;
        txt.fontStyle  = FontStyle.Bold;
        txt.color      = color;
        txt.alignment  = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        Camera cam     = Camera.main;
        float elapsed  = 0f;
        const float duration = 1.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t01 = elapsed / duration;

            // ワールド座標で上昇させてスクリーン座標に変換
            Vector3 currentWorld = worldPos + Vector3.up * (0.3f + t01 * 1.8f);
            Vector3 screenPos = cam != null
                ? cam.WorldToScreenPoint(currentWorld)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _effectRoot, screenPos, null, out Vector2 local);
            rt.anchoredPosition = local;

            // 後半35%でフェードアウト
            float alpha = t01 < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (t01 - 0.65f) / 0.35f);
            txt.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        Destroy(go);
    }

    // ================================================================
    // 必殺技エフェクト
    // ================================================================
    private IEnumerator PlayUltimateEffect(BattleUnit unit, UltimateType type)
    {
        switch (type)
        {
            case UltimateType.TimeStop:
                yield return StartCoroutine(EffectTimeStop());
                break;

            case UltimateType.MemoryReplay:
                yield return StartCoroutine(EffectMemoryReplay());
                break;

            case UltimateType.SpaceRift:
                yield return StartCoroutine(EffectSpaceRift());
                break;

            case UltimateType.AbyssCall:
            case UltimateType.GhostRush:
                Color darkColor = new Color(0.5f, 0.1f, 0.8f, 0.75f);
                yield return StartCoroutine(EffectFlashShake(darkColor));
                break;

            case UltimateType.SatelliteBeam:
                yield return StartCoroutine(EffectSatelliteBeam());
                break;

            default:
                yield return StartCoroutine(EffectFlashShake(new Color(1f, 0.75f, 0.1f, 0.75f)));
                break;
        }
    }

    // ---- 時間断絶：青白フラッシュ → "時間断絶" テキスト演出 ----
    private IEnumerator EffectTimeStop()
    {
        yield return StartCoroutine(ScreenFlash(new Color(0.65f, 0.88f, 1f, 0.95f), 0.25f));

        var overlay = MakeOverlay(new Color(0.55f, 0.68f, 0.85f, 0.18f));
        var label   = MakeCenterText("時　間　断　絶", 60, new Color(0.8f, 0.95f, 1f, 0f));

        yield return StartCoroutine(FadeTextIn(label, 0.3f));
        yield return new WaitForSeconds(0.7f);
        yield return StartCoroutine(FadeTextOut(label, 0.4f));

        Destroy(label.gameObject);
        Destroy(overlay.gameObject);
    }

    // ---- 記憶回廊：セピアオーバーレイ + テキスト ----
    private IEnumerator EffectMemoryReplay()
    {
        var overlay = MakeOverlay(new Color(0.88f, 0.74f, 0.48f, 0.28f));
        var label   = MakeCenterText("記　憶　回　廊", 60, new Color(1f, 0.95f, 0.72f, 0f));

        yield return StartCoroutine(FadeTextIn(label, 0.35f));
        yield return new WaitForSeconds(0.8f);
        yield return StartCoroutine(FadeTextOut(label, 0.45f));

        Destroy(label.gameObject);
        Destroy(overlay.gameObject);
    }

    // ---- 空間斬：斜め斬撃ライン ----
    private IEnumerator EffectSpaceRift()
    {
        yield return StartCoroutine(CameraShake(0.2f, 0.08f));

        float[] offsets = { -100f, 0f, 110f };
        float[] widths  = { 6f, 10f, 7f };
        var slashes = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("Slash" + i);
            go.transform.SetParent(_effectCanvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(2800, widths[i]);
            rt.anchoredPosition = new Vector2(offsets[i], 0f);
            rt.localRotation    = Quaternion.Euler(0f, 0f, -42f);

            slashes[i] = go.AddComponent<Image>();
            slashes[i].color = new Color(0.45f, 0.88f, 1f, 1f);
            slashes[i].raycastTarget = false;
        }

        yield return new WaitForSeconds(0.07f);

        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            float a = 1f - t / 0.3f;
            foreach (var s in slashes)
                if (s != null) s.color = new Color(0.45f, 0.88f, 1f, a);
            yield return null;
        }

        foreach (var s in slashes)
            if (s != null) Destroy(s.gameObject);
    }

    // ---- 衛星砲：上から光線が降ってくる ----
    private IEnumerator EffectSatelliteBeam()
    {
        // 細い光線が上から降下
        var beam = new GameObject("Beam");
        beam.transform.SetParent(_effectCanvas.transform, false);
        var rt = beam.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(60, 0);
        rt.anchoredPosition = Vector2.zero;

        var img = beam.AddComponent<Image>();
        img.color = new Color(1f, 0.9f, 0.4f, 0.9f);
        img.raycastTarget = false;

        // 光線を伸ばす
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            float h = Mathf.Lerp(0, 1200, t / 0.2f);
            rt.sizeDelta = new Vector2(60, h);
            yield return null;
        }

        yield return StartCoroutine(ScreenFlash(new Color(1f, 0.9f, 0.5f, 0.85f), 0.2f));
        yield return StartCoroutine(CameraShake(0.4f, 0.15f));

        Destroy(beam);
    }

    // ---- フラッシュ + カメラシェイク（汎用必殺技） ----
    private IEnumerator EffectFlashShake(Color flashColor)
    {
        yield return StartCoroutine(ScreenFlash(flashColor, 0.22f));
        yield return StartCoroutine(CameraShake(0.38f, 0.18f));
    }

    // ================================================================
    // プリミティブ演出
    // ================================================================
    private IEnumerator ScreenFlash(Color color, float duration)
    {
        var overlay = MakeOverlay(Color.clear);
        float half = duration * 0.5f;

        for (float t = 0; t < half; t += Time.deltaTime)
        {
            overlay.color = Color.Lerp(Color.clear, color, t / half);
            yield return null;
        }
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            overlay.color = Color.Lerp(color, Color.clear, t / half);
            yield return null;
        }

        Destroy(overlay.gameObject);
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        Vector3 orig = cam.transform.position;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float strength = magnitude * (1f - t / duration);
            cam.transform.position = orig + new Vector3(
                Random.Range(-1f, 1f) * strength,
                Random.Range(-1f, 1f) * strength,
                0f);
            yield return null;
        }
        cam.transform.position = orig;
    }

    private IEnumerator FadeTextIn(Text txt, float duration)
    {
        Color c = txt.color;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            txt.color = new Color(c.r, c.g, c.b, t / duration);
            yield return null;
        }
        txt.color = new Color(c.r, c.g, c.b, 1f);
    }

    private IEnumerator FadeTextOut(Text txt, float duration)
    {
        Color c = txt.color;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            txt.color = new Color(c.r, c.g, c.b, 1f - t / duration);
            yield return null;
        }
        txt.color = new Color(c.r, c.g, c.b, 0f);
    }

    // ================================================================
    // UI ファクトリ
    // ================================================================
    private Image MakeOverlay(Color color)
    {
        var go = new GameObject("Overlay");
        go.transform.SetParent(_effectCanvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text MakeCenterText(string msg, int size, Color color)
    {
        var go = new GameObject("CenterText");
        go.transform.SetParent(_effectCanvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.38f);
        rt.anchorMax = new Vector2(0.9f, 0.62f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<Text>();
        txt.text      = msg;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = size;
        txt.color     = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = new Vector2(3, -3);

        return txt;
    }
}
