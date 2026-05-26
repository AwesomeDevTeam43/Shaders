using UnityEngine;

[ExecuteInEditMode]
public class CCTVGlitchEffect : MonoBehaviour
{
    [Header("Shader Base")]
    public Shader glitchShader;
    private Material glitchMaterial;

    [Header("Definições do Efeito")]
    [Range(0f, 1f)] public float baseIntensity = 0.15f;
    public float scanlineFrequency = 5f;
    [Range(0f, 1f)] public float grainIntensity = 0.5f;

    [Header("Distancia do Jogador")]
    public Transform player;
    public Transform alvo;
    public float maxDistance = 15f;
    public float minDistance = 5f;
    [Range(0f, 1f)] public float distanceIntensity = 0.5f;

    private float currentGlitchValue;
    private float targetGlitchValue;
    private float glitchTimer;

    void Start()
    {
        if (glitchShader == null)
        {
            Debug.LogError("Atribui o CCTVGlitch Shader no Inspetor!");
            enabled = false;
            return;
        }
        glitchMaterial = new Material(glitchShader);
    }

    void Update()
    {
        glitchTimer -= Time.deltaTime;
        if (glitchTimer <= 0f)
        {
            targetGlitchValue = Random.value > 0.85f ? Random.Range(0.4f, 0.8f) : baseIntensity;
            glitchTimer = Random.Range(0.1f, 1.0f);
        }

        currentGlitchValue = Mathf.Lerp(currentGlitchValue, targetGlitchValue, Time.deltaTime * 8f);

        float glitchProximity = 0f;
        if(player != null && alvo != null)
        {
            float distance = Vector3.Distance(player.position, alvo.position);

            if(distance < maxDistance)
            {
                float proximityPercent = 1- Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));
                glitchProximity = proximityPercent * distanceIntensity;
            }
        }

        float finalGlitchValue = Mathf.Clamp01(currentGlitchValue + glitchProximity);

        if (glitchMaterial != null)
        {
            glitchMaterial.SetFloat("_GlitchIntensity", finalGlitchValue);
            glitchMaterial.SetFloat("_LinesFrequency", scanlineFrequency);
            glitchMaterial.SetFloat("_GraoIntensity", grainIntensity);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (glitchMaterial != null)
        {
            Graphics.Blit(source, destination, glitchMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    void OnDisable()
    {
        if (glitchMaterial) DestroyImmediate(glitchMaterial);
    }
}