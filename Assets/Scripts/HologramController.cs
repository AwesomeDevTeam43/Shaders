using UnityEngine;
using System.Collections.Generic;

public class HologramController : MonoBehaviour
{
    [Header("Setup Automático")]
    [Tooltip("Arrasta o teu Material de Holograma para aqui. O script vai aplicá-lo a todos os filhos.")]
    public Material materialHologramaBase;

    [Header("Glitch Proximity Settings")]
    [Tooltip("Distância a que o efeito de glitch começa.")]
    public float glitchStartDistance = 2.0f;
    
    [Tooltip("Distância a que o glitch atinge a intensidade máxima.")]
    public float glitchMaxDistance = 1.5f;

    [Tooltip("Distância a que o glitch fica constante (sem pausas).")]
    public float constantGlitchDistance = 1.0f;

    // Variáveis privadas (o script procura isto automaticamente agora)
    private Transform playerTransform;
    private List<Material> materiaisInstanciados = new List<Material>();
    
    private int glitchIntensityID;
    private int isConstantGlitchID;

    void Start()
    {
        // 1. IR BUSCAR O PLAYER AUTOMATICAMENTE PELA TAG
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player não encontrado! Verifica se o teu jogador tem a Tag 'Player' no Inspector.");
        }

        // Cache dos IDs do shader para não pesar na performance
        glitchIntensityID = Shader.PropertyToID("_GlitchIntensity");
        isConstantGlitchID = Shader.PropertyToID("_IsConstantGlitch");

        // 2. PERCORRER TODOS OS FILHOS E O PRÓPRIO PAI
        // O GetComponentsInChildren apanha o MeshRenderer/SkinnedMeshRenderer do próprio objeto onde este script está, E de todos os filhos dentro dele!
        Renderer[] todosOsRenderers = GetComponentsInChildren<Renderer>();

        if (materialHologramaBase != null)
        {
            // 3. MUDAR OS MATERIAIS DE TODOS OS FILHOS
            foreach (Renderer rend in todosOsRenderers)
            {
                // Como um modelo 3D pode ter múltiplos materiais (ex: um carro tem material para pneus e outro para a chapa)
                // Fazemos um loop para garantir que TODAS as partes ficam holográficas.
                Material[] novosMateriais = new Material[rend.materials.Length];
                
                for (int i = 0; i < novosMateriais.Length; i++)
                {
                    // Criamos uma cópia do material na RAM para não estragar o ficheiro original
                    Material matInstance = new Material(materialHologramaBase);
                    novosMateriais[i] = matInstance;
                    
                    // Guardamos numa lista para podermos atualizar as variáveis de todos ao mesmo tempo no Update
                    materiaisInstanciados.Add(matInstance);
                }
                
                // Aplicamos os materiais atualizados de volta ao Renderer do filho
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
        // Se não houver player ou não houver materiais para atualizar, não faz nada
        if (playerTransform == null || materiaisInstanciados.Count == 0) return;

        // Calcula a distância entre o objeto Pai (este script) e o Player
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Calcula a matemática do Glitch com base na proximidade
        float intensity = Mathf.InverseLerp(glitchStartDistance, glitchMaxDistance, distance);
        float isConstant = (distance <= constantGlitchDistance) ? 1f : 0f;

        // 4. ATUALIZAR TODOS OS FILHOS EM SIMULTÂNEO
        foreach (Material mat in materiaisInstanciados)
        {
            mat.SetFloat(glitchIntensityID, intensity);
            mat.SetFloat(isConstantGlitchID, isConstant);
        }
    }
    
    // Gizmos para te ajudar a visualizar as distâncias no Unity
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