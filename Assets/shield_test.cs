using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShieldAutoTest : MonoBehaviour
{
    public EnergyShieldController shield;
    public float interval = 1.5f;
    public bool showGizmos = true;
    [Range(0.1f, 5f)] public float gizmoDuration = 1.2f;
    [Range(1, 32)] public int maxGizmoHits = 12;

    struct DebugHit
    {
        public Vector3 point;
        public float time;
    }

    readonly List<DebugHit> _debugHits = new List<DebugHit>();
    Vector3 _lastImpactPoint;
    bool _hasImpactPoint;

    void Start()
    {
        if (shield == null)
        {
            enabled = false;
            return;
        }

        StartCoroutine(AutoImpact());
    }

    void Update()
    {
        CleanupOldGizmos();
    }

    IEnumerator AutoImpact()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            Vector3 pt = shield.transform.position
                         + Random.onUnitSphere * 1.5f;
            float radius = Random.Range(0.3f, 0.9f);

            AddImpactPoint(pt);
            shield.TriggerImpact(pt, radius);
        }
    }

    void AddImpactPoint(Vector3 point)
    {
        _lastImpactPoint = point;
        _hasImpactPoint = true;

        _debugHits.Add(new DebugHit
        {
            point = point,
            time = Time.time
        });

        int maxHits = Mathf.Max(1, maxGizmoHits);
        if (_debugHits.Count > maxHits)
            _debugHits.RemoveAt(0);
    }

    void CleanupOldGizmos()
    {
        if (_debugHits.Count == 0)
            return;

        float life = Mathf.Max(0.1f, gizmoDuration);
        float now = Time.time;
        for (int i = _debugHits.Count - 1; i >= 0; i--)
        {
            if (now - _debugHits[i].time > life)
                _debugHits.RemoveAt(i);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying)
            return;

        float life = Mathf.Max(0.1f, gizmoDuration);
        float now = Time.time;

        foreach (var hit in _debugHits)
        {
            float t = Mathf.Clamp01((now - hit.time) / life);
            float s = Mathf.Lerp(0.14f, 0.04f, t);
            Color c = Color.Lerp(new Color(1f, 0.95f, 0.2f, 1f), new Color(1f, 0.35f, 0.1f, 0f), t);

            Gizmos.color = c;
            Gizmos.DrawLine(hit.point - Vector3.right * s, hit.point + Vector3.right * s);
            Gizmos.DrawLine(hit.point - Vector3.up * s, hit.point + Vector3.up * s);
            Gizmos.DrawLine(hit.point - Vector3.forward * s, hit.point + Vector3.forward * s);
            Gizmos.DrawWireSphere(hit.point, Mathf.Lerp(0.08f, 0.03f, t));
        }

        if (_hasImpactPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_lastImpactPoint, 0.1f);
        }
    }
#endif
}