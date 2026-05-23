using UnityEngine;

public class CCTVGlitchEffect : MonoBehaviour
{
    public Material glitchMaterial;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
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
}