using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class HologramController : MonoBehaviour
{
    [Header("References")]
    public Renderer hologramRenderer;
    public Transform playerTransform;

    [Header("Glitch Proximity Settings")]
    [Tooltip("Distance at which the glitch effect starts.")]
    public float glitchStartDistance = 5f;
    
    [Tooltip("Distance at which the glitch effect is at maximum intensity.")]
    public float glitchMaxDistance = 1.5f;

    [Tooltip("Distance at which the glitch becomes constant without pausing.")]
    public float constantGlitchDistance = 0.5f;

    private Material hologramMaterial;
    private int glitchIntensityID;
    private int isConstantGlitchID;

    void Start()
    {
        if (hologramRenderer == null)
            hologramRenderer = GetComponent<Renderer>();

        hologramMaterial = hologramRenderer.material;

        // Cache both IDs for performance
        glitchIntensityID = Shader.PropertyToID("_GlitchIntensity");
        isConstantGlitchID = Shader.PropertyToID("_IsConstantGlitch");
    }

    void Update()
    {
        if (playerTransform == null || hologramMaterial == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 1. Handle Intensity (InverseLerp handles the math smoothly)
        float intensity = Mathf.InverseLerp(glitchStartDistance, glitchMaxDistance, distance);
        hologramMaterial.SetFloat(glitchIntensityID, intensity);

        // 2. Handle Constant Glitch Threshold (0 or 1)
        float isConstant = (distance <= constantGlitchDistance) ? 1f : 0f;
        hologramMaterial.SetFloat(isConstantGlitchID, isConstant);
    }
    
    // Updated Gizmos to show the new threshold in purple
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, glitchStartDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, glitchMaxDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, constantGlitchDistance);
    }
}