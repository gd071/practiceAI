using UnityEngine;

public enum EventPointKind { Story, Minigame, Npc }

/// <summary>
/// フィールド上のイベント発生地点。
/// 頭上で回転するマーカーを持ち、プレイヤーが近づいてEキーで起動する。
/// </summary>
public class FieldEventPoint : MonoBehaviour
{
    public string          eventId;
    public string          label;
    public EventPointKind  kind;
    public float           radius = 2.2f;
    public bool            autoTrigger;   // 触れただけで発動（オープニング等）

    private Transform _marker;

    public static FieldEventPoint Create(string eventId, string label, EventPointKind kind,
        Vector3 pos, bool autoTrigger = false)
    {
        var go = new GameObject("Event_" + eventId);
        go.transform.position = pos;
        var ep = go.AddComponent<FieldEventPoint>();
        ep.eventId     = eventId;
        ep.label       = label;
        ep.kind        = kind;
        ep.autoTrigger = autoTrigger;
        ep.BuildMarker();
        return ep;
    }

    private void BuildMarker()
    {
        var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(m.GetComponent<Collider>());
        m.name = "Marker";
        m.transform.SetParent(transform, false);
        m.transform.localPosition = new Vector3(0, 2.4f, 0);
        m.transform.localScale    = Vector3.one * 0.45f;
        _marker = m.transform;

        Color c = kind switch
        {
            EventPointKind.Story    => new Color(1f, 0.85f, 0.2f),
            EventPointKind.Minigame => new Color(0.3f, 0.9f, 1f),
            _                       => new Color(0.5f, 1f, 0.5f),
        };
        var mr = m.GetComponent<MeshRenderer>();
        mr.material = FieldBootstrap.MakeMat(c, emissive: true);
    }

    void Update()
    {
        if (_marker != null)
        {
            _marker.Rotate(0, 120f * Time.deltaTime, 40f * Time.deltaTime);
            _marker.localPosition = new Vector3(0,
                2.4f + Mathf.Sin(Time.time * 2.2f) * 0.18f, 0);
        }
    }
}
