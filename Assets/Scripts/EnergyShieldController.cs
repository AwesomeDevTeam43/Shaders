using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnergyShieldController : MonoBehaviour
{
    [Header("References")]
    public Renderer shieldRenderer;

    [Header("Impact Settings")]
    [Tooltip("How long the impact animation takes (seconds)")]
    public float impactDuration = 0.6f;

    private Material _mat;
    private Coroutine _animCoroutine;

    // Cache shader property IDs for performance
    private static readonly int ImpactPosID  = Shader.PropertyToID("_ImpactPos");
    private static readonly int ImpactTimeID = Shader.PropertyToID("_ImpactTime");
    private static readonly int RadiusID     = Shader.PropertyToID("_ImpactRadius");

    void Start()
    {
        if (shieldRenderer == null)
            shieldRenderer = GetComponent<Renderer>();

        _mat = shieldRenderer.material;
        _mat.SetFloat(ImpactTimeID, 0f);
    }

    void OnCollisionEnter(Collision col)
    {
        TriggerImpact(col.contacts[0].point);
    }

    void OnTriggerEnter(Collider other)
    {
        Vector3 impactPoint = GetComponent<Collider>()
                              .ClosestPoint(other.transform.position);
        TriggerImpact(impactPoint);
    }

    public void TriggerImpact(Vector3 worldPosition, float radius = 0.6f)
    {
        if (_mat == null) return;

        _mat.SetVector(ImpactPosID, new Vector4(worldPosition.x,
                                                worldPosition.y,
                                                worldPosition.z, 0f));
        _mat.SetFloat(RadiusID, radius);

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateImpact());
    }

    IEnumerator AnimateImpact()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / impactDuration;
            _mat.SetFloat(ImpactTimeID, Mathf.Clamp01(t));
            yield return null;
        }
        _mat.SetFloat(ImpactTimeID, 0f);
    }

    void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }
}
