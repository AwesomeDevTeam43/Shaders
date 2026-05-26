using UnityEngine;
using System.Collections.Generic;

public class HologramController : MonoBehaviour
{
    [Header("Setup Automático")]
    [Tooltip("Arrasta o teu Material de Holograma para aqui. O script vai aplicá-lo a todos os filhos.")]
    public Material materialHologramaBase;

    [Header("Glitch Proximity Settings")]
    [Tooltip("Distância a que o efeito de glitch começa.")]
    public float glitchStartDistance = 5f;
    
    [Tooltip("Distância a que o glitch atinge a intensidade máxima.")]
    public float glitchMaxDistance = 1.5f;

    [Tooltip("Distância a que o glitch fica constante (sem pausas).")]
    public float constantGlitchDistance = 0.5f;

    private Transform playerTransform;
    private List<Material> materiaisInstanciados = new List<Material>();
    
    private int glitchIntensityID;
    private int isConstantGlitchID;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player não encontrado! Verifica se o teu jogador tem a Tag 'Player' no Inspector.");
        }

        glitchIntensityID = Shader.PropertyToID("_GlitchIntensity");
        isConstantGlitchID = Shader.PropertyToID("_IsConstantGlitch");

        Renderer[] todosOsRenderers = GetComponentsInChildren<Renderer>();

        if (materialHologramaBase != null)
        {
            foreach (Renderer rend in todosOsRenderers)
            {
                Material[] novosMateriais = new Material[rend.materials.Length];
                
                for (int i = 0; i < novosMateriais.Length; i++)
                {
                    Material matInstance = new Material(materialHologramaBase);
                    novosMateriais[i] = matInstance;
                    
                    materiaisInstanciados.Add(matInstance);
                }
                
                rend.materials = novosMateriais;
            }
        }
        else
        {
            Debug.LogError("Falta colocar o 'Material Holograma Base' no script " + gameObject.name);
        }
    }

    void Update()
    {
        if (playerTransform == null || materiaisInstanciados.Count == 0) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        float intensity = Mathf.InverseLerp(glitchStartDistance, glitchMaxDistance, distance);
        float isConstant = (distance <= constantGlitchDistance) ? 1f : 0f;

        foreach (Material mat in materiaisInstanciados)
        {
            mat.SetFloat(glitchIntensityID, intensity);
            mat.SetFloat(isConstantGlitchID, isConstant);
        }
    }
    
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