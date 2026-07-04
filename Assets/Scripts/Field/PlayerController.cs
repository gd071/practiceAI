using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// フィールドのプレイヤー操作（WASD/矢印キー移動・XZ平面）。
/// パーティメンバーがドット絵で後ろをついて歩く。
/// Eキーで近くのイベントポイントを起動する。
/// </summary>
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6.5f;

    private SpriteRenderer _sr;
    private CharacterDef   _def;
    private float          _animTimer;
    private int            _frame;
    private bool           _facingLeft;

    // 隊列
    private readonly List<Vector3> _trail = new List<Vector3>();
    private readonly List<FollowerVisual> _followers = new List<FollowerVisual>();
    private const float TrailStep   = 0.35f;
    private const int   TrailSpacing = 4;   // フォロワー間の履歴間隔

    private Camera _cam;

    private class FollowerVisual
    {
        public SpriteRenderer sr;
        public CharacterDef   def;
        public int            frame;
        public float          animTimer;
    }

    // ================================================================
    public void Setup(Camera cam)
    {
        _cam = cam;
        _def = CharacterDB.Get("kei");

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite = PixelSpriteFactory.Human(_def.id, _def.hairColor, _def.bodyColor, 0);

        _trail.Add(transform.position);
        RebuildFollowers();
    }

    /// <summary>パーティ構成が変わったら呼び直す（主人公以外を隊列表示）。</summary>
    public void RebuildFollowers()
    {
        foreach (var f in _followers)
            if (f.sr != null) Destroy(f.sr.gameObject);
        _followers.Clear();

        int idx = 0;
        foreach (var id in StoryManager.PartyIds)
        {
            if (id == "kei") continue;
            var def = CharacterDB.Get(id);
            if (def == null) continue;

            var go = new GameObject("Follower_" + id);
            go.transform.position = transform.position - new Vector3(0.8f * (idx + 1), 0, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = def.shape == SpriteShape.Dog
                ? PixelSpriteFactory.Dog(def.hairColor, 0)
                : PixelSpriteFactory.Human(def.id, def.hairColor, def.bodyColor, 0);

            _followers.Add(new FollowerVisual { sr = sr, def = def });
            idx++;
        }
    }

    // ================================================================
    void Update()
    {
        if (DialogueUI.IsPlaying || MinigameManager.IsPlaying)
        {
            UpdateCamera();
            return;
        }

        // ---- 移動入力 ----
        Vector2 input = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    input.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  input.y -= 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  input.x -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
        }

        bool moving = input.sqrMagnitude > 0.01f;
        if (moving)
        {
            Vector3 delta = new Vector3(input.x, 0, input.y).normalized * moveSpeed * Time.deltaTime;
            Vector3 next  = transform.position + delta;
            next.x = Mathf.Clamp(next.x, -38f, 38f);
            next.z = Mathf.Clamp(next.z, -38f, 38f);
            transform.position = next;

            if (Mathf.Abs(input.x) > 0.01f) _facingLeft = input.x < 0;

            // 軌跡を記録（隊列用）
            if (_trail.Count == 0 ||
                (transform.position - _trail[_trail.Count - 1]).sqrMagnitude > TrailStep * TrailStep)
            {
                _trail.Add(transform.position);
                if (_trail.Count > 200) _trail.RemoveAt(0);
            }
        }

        // ---- 歩行アニメ ----
        UpdateWalkAnim(moving);
        UpdateFollowers(moving);
        UpdateCamera();

        // ---- インタラクト ----
        if (kb != null && kb.eKey.wasPressedThisFrame)
            TryInteract();
        CheckAutoTriggers();
    }

    private void UpdateWalkAnim(bool moving)
    {
        if (moving)
        {
            _animTimer += Time.deltaTime;
            if (_animTimer > 0.18f)
            {
                _animTimer = 0f;
                _frame = 1 - _frame;
            }
        }
        else _frame = 0;

        _sr.sprite = PixelSpriteFactory.Human(_def.id, _def.hairColor, _def.bodyColor, _frame);
        _sr.flipX  = _facingLeft;
    }

    private void UpdateFollowers(bool moving)
    {
        for (int i = 0; i < _followers.Count; i++)
        {
            var f = _followers[i];
            if (f.sr == null) continue;

            // 軌跡上の少し後ろの位置へ
            int trailIdx = _trail.Count - 1 - (i + 1) * TrailSpacing;
            if (trailIdx >= 0)
            {
                Vector3 target = _trail[trailIdx];
                Vector3 cur    = f.sr.transform.position;
                Vector3 next   = Vector3.Lerp(cur, target, Time.deltaTime * 8f);
                if (Mathf.Abs(next.x - cur.x) > 0.005f)
                    f.sr.flipX = next.x < cur.x;
                f.sr.transform.position = next;
            }

            if (moving)
            {
                f.animTimer += Time.deltaTime;
                if (f.animTimer > 0.18f)
                {
                    f.animTimer = 0f;
                    f.frame = 1 - f.frame;
                }
            }
            else f.frame = 0;

            f.sr.sprite = f.def.shape == SpriteShape.Dog
                ? PixelSpriteFactory.Dog(f.def.hairColor, f.frame)
                : PixelSpriteFactory.Human(f.def.id, f.def.hairColor, f.def.bodyColor, f.frame);
        }
    }

    private void UpdateCamera()
    {
        if (_cam == null) return;
        Vector3 target = transform.position + new Vector3(0, 9.5f, -9.5f);
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, target, Time.deltaTime * 5f);
        _cam.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
    }

    // ================================================================
    // イベント起動
    // ================================================================
    public FieldEventPoint NearestPoint()
    {
        FieldEventPoint best = null;
        float bestDist = float.MaxValue;
        foreach (var ep in Object.FindObjectsByType<FieldEventPoint>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(transform.position, ep.transform.position);
            if (d < ep.radius && d < bestDist)
            {
                bestDist = d;
                best = ep;
            }
        }
        return best;
    }

    private void TryInteract()
    {
        var ep = NearestPoint();
        if (ep != null)
            StoryEvents.Trigger(ep);
    }

    private void CheckAutoTriggers()
    {
        var ep = NearestPoint();
        if (ep != null && ep.autoTrigger)
            StoryEvents.Trigger(ep);
    }
}
