using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// フィールド用ドット絵スプライトをプロシージャル生成する。
/// 16x24 の人型（髪・肌・服・足）と 24x14 の犬。FilterMode.Point でドット感を出す。
/// </summary>
public static class PixelSpriteFactory
{
    private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    private static readonly Color Skin    = new Color(1f, 0.87f, 0.73f);
    private static readonly Color EyeCol  = new Color(0.1f, 0.1f, 0.15f);
    private static readonly Color Clear   = new Color(0, 0, 0, 0);

    /// <summary>人型キャラのドット絵（16x24）。frame=0/1 で歩行2コマ。</summary>
    public static Sprite Human(string id, Color hair, Color body, int frame)
    {
        string key = $"h_{id}_{frame}";
        if (_cache.TryGetValue(key, out var s) && s != null) return s;

        const int W = 16, H = 24;
        var px = NewGrid(W, H);

        Color shoe  = new Color(0.2f, 0.18f, 0.2f);
        Color bodyD = body * 0.75f; bodyD.a = 1f;

        // 足（歩行フレームで開閉）
        if (frame == 0)
        {
            Rect2(px, W, 5, 0, 2, 4, shoe);   // 左足
            Rect2(px, W, 9, 0, 2, 4, shoe);   // 右足
        }
        else
        {
            Rect2(px, W, 4, 0, 2, 4, shoe);
            Rect2(px, W, 10, 0, 2, 4, shoe);
        }

        // 胴体（服）
        Rect2(px, W, 4, 4, 8, 7, body);
        Rect2(px, W, 4, 4, 8, 1, bodyD);      // 裾の影

        // 腕
        Rect2(px, W, 2, 5, 2, 5, body);
        Rect2(px, W, 12, 5, 2, 5, body);
        Rect2(px, W, 2, 4, 2, 1, Skin);       // 手
        Rect2(px, W, 12, 4, 2, 1, Skin);

        // 頭（肌）
        Rect2(px, W, 4, 11, 8, 8, Skin);

        // 髪（頭頂＋サイド）
        Rect2(px, W, 3, 16, 10, 5, hair);
        Rect2(px, W, 3, 13, 2, 4, hair);
        Rect2(px, W, 11, 13, 2, 4, hair);

        // 目
        Set(px, W, 6, 14, EyeCol);
        Set(px, W, 9, 14, EyeCol);

        return Bake(key, px, W, H);
    }

    /// <summary>犬のドット絵（24x14）。</summary>
    public static Sprite Dog(Color furMain, int frame)
    {
        string key = $"dog_{frame}";
        if (_cache.TryGetValue(key, out var s) && s != null) return s;

        const int W = 24, H = 14;
        var px = NewGrid(W, H);

        Color furD = furMain * 0.72f; furD.a = 1f;

        // 足
        if (frame == 0)
        {
            Rect2(px, W, 4, 0, 2, 3, furD);
            Rect2(px, W, 14, 0, 2, 3, furD);
        }
        else
        {
            Rect2(px, W, 6, 0, 2, 3, furD);
            Rect2(px, W, 12, 0, 2, 3, furD);
        }

        // 胴体
        Rect2(px, W, 3, 3, 14, 6, furMain);

        // 頭
        Rect2(px, W, 15, 5, 7, 7, furMain);

        // 耳
        Rect2(px, W, 16, 12, 2, 2, furD);
        Rect2(px, W, 20, 12, 2, 2, furD);

        // 目・鼻
        Set(px, W, 19, 9, EyeCol);
        Set(px, W, 21, 8, EyeCol);

        // しっぽ
        Rect2(px, W, 1, 7, 2, 3, furD);

        return Bake(key, px, W, H);
    }

    /// <summary>汎用NPC（色違い人型）。</summary>
    public static Sprite Npc(int variant, int frame)
    {
        Color[] hairs = {
            new Color(0.35f, 0.2f, 0.12f), new Color(0.15f, 0.15f, 0.18f),
            new Color(0.6f, 0.5f, 0.35f),  new Color(0.5f, 0.25f, 0.25f) };
        Color[] bodies = {
            new Color(0.4f, 0.55f, 0.4f),  new Color(0.55f, 0.45f, 0.6f),
            new Color(0.6f, 0.55f, 0.35f), new Color(0.35f, 0.5f, 0.6f) };
        int i = Mathf.Abs(variant) % hairs.Length;
        return Human("npc" + i, hairs[i], bodies[i], frame);
    }

    // ================================================================
    // 内部ユーティリティ
    // ================================================================
    private static Color[] NewGrid(int w, int h)
    {
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Clear;
        return px;
    }

    private static void Set(Color[] px, int w, int x, int y, Color c)
    {
        int idx = y * w + x;
        if (idx >= 0 && idx < px.Length) px[idx] = c;
    }

    private static void Rect2(Color[] px, int w, int x, int y, int rw, int rh, Color c)
    {
        for (int yy = y; yy < y + rh; yy++)
            for (int xx = x; xx < x + rw; xx++)
                Set(px, w, xx, yy, c);
    }

    private static Sprite Bake(string key, Color[] px, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;   // ドット絵の要
        tex.wrapMode   = TextureWrapMode.Clamp;

        // pixelsPerUnit 16 → 人型は高さ1.5ユニット
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        _cache[key] = sprite;
        return sprite;
    }
}
