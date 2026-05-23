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
        // C# controla o ritmo orgânico das interferências
        glitchTimer -= Time.deltaTime;
        if (glitchTimer <= 0f)
        {
            targetGlitchValue = Random.value > 0.85f ? Random.Range(0.4f, 0.8f) : baseIntensity;
            glitchTimer = Random.Range(0.1f, 1.0f);
        }

        currentGlitchValue = Mathf.Lerp(currentGlitchValue, targetGlitchValue, Time.deltaTime * 8f);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (glitchMaterial != null)
        {
            // Injeta apenas os números calculados pelo C#
            glitchMaterial.SetFloat("_GlitchIntensity", currentGlitchValue);
            glitchMaterial.SetFloat("_LinesFrequency", scanlineFrequency);
            glitchMaterial.SetFloat("_GraoIntensity", grainIntensity);

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