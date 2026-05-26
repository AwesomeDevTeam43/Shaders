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

    [Header("Repulsion Settings")]
    [Tooltip("Force applied to objects that collide with the shield")]
    public float repulsionForce = 15f;
    [Tooltip("Extra upward bias added to the repulsion direction (0 = purely outward)")]
    public float upwardBias = 0.2f;
    [Tooltip("Force mode used when repelling the object")]
    public ForceMode forceMode = ForceMode.Impulse;

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
        Vector3 contactPoint = col.contacts[0].point;
        TriggerImpact(contactPoint);
        RepelObject(col.rigidbody, contactPoint);
    }

    void OnTriggerEnter(Collider other)
    {
        Vector3 impactPoint = GetComponent<Collider>()
                              .ClosestPoint(other.transform.position);
        TriggerImpact(impactPoint);
        RepelObject(other.attachedRigidbody, impactPoint);
    }

    /// <summary>
    /// Pushes <paramref name="rb"/> away from <paramref name="contactPoint"/>
    /// using the configured repulsion force.
    /// </summary>
    private void RepelObject(Rigidbody rb, Vector3 contactPoint)
    {
        if (rb == null) return;

        // Direction: from contact point outward, away from the shield centre
        Vector3 direction = (contactPoint - transform.position).normalized;

        // Add a small upward component so objects don't get pushed straight into the ground
        direction = (direction + Vector3.up * upwardBias).normalized;

        rb.AddForce(direction * repulsionForce, forceMode);
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
