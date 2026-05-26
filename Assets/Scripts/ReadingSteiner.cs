using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class ReadingSteinerEffect : MonoBehaviour
{
    [Header("Shader Setup")]
    public Shader steinerShader;
    private Material steinerMaterial;

    [Header("Current State")]
    [Range(0f, 1.5f)] public float currentIntensity = 0f;

    void Start()
    {
        if (steinerShader == null)
        {
            Debug.LogError("Assign the ReadingSteiner Shader in the Inspector!");
            enabled = false;
            return;
        }
        steinerMaterial = new Material(steinerShader);
    }

    public void ActivateWorldlineShift()
    {
        StartCoroutine(ShiftRoutine());
    }

    private IEnumerator ShiftRoutine()
    {
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            currentIntensity = Mathf.Lerp(0f, 1.2f, percent * percent * percent); 
            yield return null;
        }

        currentIntensity = 1.2f; 
        
        yield return new WaitForSeconds(0.4f);

        currentIntensity = 0f;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (steinerMaterial != null && currentIntensity > 0.01f)
        {
            steinerMaterial.SetFloat("_GlitchIntensity", currentIntensity);
            Graphics.Blit(source, destination, steinerMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    void OnDisable()
    {
        if (steinerMaterial) DestroyImmediate(steinerMaterial);
    }
}