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

    // You call this function from your VR Controller Button script!
    public void ActivateWorldlineShift()
    {
        StartCoroutine(ShiftRoutine());
    }

    private IEnumerator ShiftRoutine()
    {
        float duration = 2.5f; // How many seconds the glitch builds up
        float elapsed = 0f;

        // 1. The Build-Up (Ramp from 0 to max chaos)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Exponential ramp for a sudden violent snap at the end
            float percent = elapsed / duration;
            currentIntensity = Mathf.Lerp(0f, 1.2f, percent * percent * percent); 
            yield return null;
        }

        // 2. The Climax (Hold at max intensity so the screen is completely garbled)
        currentIntensity = 1.2f; 
        
        // ==========================================
        // DO YOUR SCENE LOADING OR TELEPORTING HERE!
        // ==========================================
        
        yield return new WaitForSeconds(0.4f); // Hold the glitch for a fraction of a second

        // 3. The Snap Back (Worldline established)
        currentIntensity = 0f;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Only apply the expensive effect if it's actually happening
        if (steinerMaterial != null && currentIntensity > 0.01f)
        {
            steinerMaterial.SetFloat("_GlitchIntensity", currentIntensity);
            Graphics.Blit(source, destination, steinerMaterial);
        }
        else
        {
            // Just pass the camera feed through normally
            Graphics.Blit(source, destination);
        }
    }

    void OnDisable()
    {
        if (steinerMaterial) DestroyImmediate(steinerMaterial);
    }
}