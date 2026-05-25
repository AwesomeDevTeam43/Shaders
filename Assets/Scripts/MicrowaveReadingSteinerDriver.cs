using System.Collections;
using UnityEngine;

public class MicrowaveReadingSteinerDriver : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Arrasta a câmara que tem o script ReadingSteinerEffect")]
    [SerializeField] private ReadingSteinerEffect readingSteinerEffect;

    [Header("Effect Settings")]
    [SerializeField] private float rampDuration = 2.5f;
    [SerializeField] private float maxIntensity = 1.2f;

    [Header("Teleport Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform cloneRoomTP;
    [SerializeField] private Transform startRoomTP;

    [Header("Worldline State (Portais e Objetos)")]
    [SerializeField] private MonoBehaviour cctvEffect;
    [Tooltip("Objetos a ESCONDER na nova realidade (Ex: A Parede Normal)")]
    [SerializeField] private GameObject[] objectsOff;
    [Tooltip("Objetos a MOSTRAR na nova realidade (Ex: O Quad do Portal e a Esfera)")]
    [SerializeField] private GameObject[] objectsOn;
    
    [Header("Hologramas (Automático por Pais)")]
    [Tooltip("Arrasta o material do Holograma para aqui")]
    [SerializeField] private Material materialHolograma;
    [Tooltip("Arrasta para aqui os objetos PAIS. O script vai transformar o pai e TODOS os seus filhos.")]
    [SerializeField] private Transform[] paisParaHolograma;

    private Coroutine shiftCoroutine;

    // Função pública para poderes chamar do teu script de VR ou de um botão
    public void ActivateWorldlineShift()
    {
        // Evita que o jogador ative a viagem duas vezes ao mesmo tempo
        if (shiftCoroutine == null)
        {
            Debug.Log("Viagem no tempo iniciada!");
            shiftCoroutine = StartCoroutine(WorldlineShiftSequence());
        }
    }

    private IEnumerator WorldlineShiftSequence()
    {
        // 1. Build-up (Distorção a aumentar)
        float elapsed = 0f;
        while (elapsed < rampDuration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / rampDuration);
            if (readingSteinerEffect != null)
                readingSteinerEffect.currentIntensity = Mathf.Lerp(0f, maxIntensity, percent * percent * percent);
            
            yield return null;
        }

        // 2. Clímax
        if (readingSteinerEffect != null) readingSteinerEffect.currentIntensity = maxIntensity;
        yield return new WaitForSeconds(0.4f);

        // 3. Teleporte para Sala dos Clones
        if (player != null && cloneRoomTP != null) TeleportPlayer(cloneRoomTP);
        if (cctvEffect != null) cctvEffect.enabled = true;
        
        if (readingSteinerEffect != null) readingSteinerEffect.currentIntensity = 0f; 

        // 4. Choque Psicológico na sala dos clones
        yield return new WaitForSeconds(5.0f);

        // 5. Regresso Violento
        if (readingSteinerEffect != null) readingSteinerEffect.currentIntensity = maxIntensity;
        yield return new WaitForSeconds(0.2f);

        // 6. Estabelecer nova Worldline (Regresso)
        if (player != null && startRoomTP != null) TeleportPlayer(startRoomTP);
        if (cctvEffect != null) cctvEffect.enabled = false;

        // --- ATIVAR / DESATIVAR OBJETOS E PORTAIS ---
        foreach (var obj in objectsOff) if (obj != null) obj.SetActive(false);
        foreach (var obj in objectsOn) if (obj != null) obj.SetActive(true);

        // --- TRANSFORMAR EM HOLOGRAMA (AUTOMÁTICO NOS FILHOS) ---
        if (materialHolograma != null && paisParaHolograma != null)
        {
            // Percorre cada objeto Pai que arrastaste no Inspetor
            foreach (Transform pai in paisParaHolograma)
            {
                if (pai != null)
                {
                    // Procura automaticamente todos os componentes de desenho no Pai e nos Filhos
                    Renderer[] todosOsRenderers = pai.GetComponentsInChildren<Renderer>(true);
                    
                    foreach (Renderer rend in todosOsRenderers)
                    {
                        // Muda todos os materiais de cada parte
                        Material[] novosMateriais = new Material[rend.materials.Length];
                        for (int i = 0; i < novosMateriais.Length; i++)
                        {
                            novosMateriais[i] = materialHolograma;
                        }
                        rend.materials = novosMateriais;
                    }
                }
            }
        }

        // Fim da sequência
        if (readingSteinerEffect != null) readingSteinerEffect.currentIntensity = 0f;
        shiftCoroutine = null;
        Debug.Log("Viagem concluída com sucesso!");
    }

    private void TeleportPlayer(Transform targetTP)
    {
        // Desligamos o CharacterController brevemente para o Unity não bloquear o Teleporte do Jogador
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; 

        player.position = targetTP.position;
        player.rotation = targetTP.rotation;

        if (cc != null) cc.enabled = true;
    }
}