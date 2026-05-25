using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class MicrowaveReadingSteinerDriver : MonoBehaviour
{
    [Header("VR References")]
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private BlackHoleActivationButton microwaveSwitch;
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
    
    [Header("Hologramas Dinâmicos")]
    [Tooltip("Arrasta o material do Holograma para aqui")]
    [SerializeField] private Material materialHolograma;
    [Tooltip("Arrasta para aqui os objetos PAIS. O código vai injetar o script neles!")]
    [SerializeField] private Transform[] paisParaHolograma;

    private Coroutine shiftCoroutine;
    private bool isHeld;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();

        if (readingSteinerEffect == null)
            readingSteinerEffect = FindFirstObjectByType<ReadingSteinerEffect>();
    }

    private void OnEnable()
    {
        if (grabInteractable == null) return;
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        if (grabInteractable == null) return;
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
        grabInteractable.activated.RemoveListener(OnActivated);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (!isHeld) return;
        if (microwaveSwitch == null || !microwaveSwitch.IsMicrowaveOn) return;
        if (readingSteinerEffect == null) return;

        if (shiftCoroutine == null)
        {
            Debug.Log("Viagem no tempo iniciada!");
            shiftCoroutine = StartCoroutine(WorldlineShiftSequence());
        }
    }

    private IEnumerator WorldlineShiftSequence()
    {
        // 1. Build-up
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
        yield return new WaitForSeconds(15.0f);

        // 5. Regresso Violento
        if (readingSteinerEffect != null) readingSteinerEffect.currentIntensity = maxIntensity;
        yield return new WaitForSeconds(0.2f);

        // 6. Estabelecer nova Worldline (Regresso)
        if (player != null && startRoomTP != null) TeleportPlayer(startRoomTP);
        if (cctvEffect != null) cctvEffect.enabled = false;

        // --- ATIVAR / DESATIVAR OBJETOS E PORTAIS ---
        foreach (var obj in objectsOff) if (obj != null) obj.SetActive(false);
        foreach (var obj in objectsOn) if (obj != null) obj.SetActive(true);

        // --- INJETAR SCRIPT DE HOLOGRAMA DINAMICAMENTE ---
        if (materialHolograma != null && paisParaHolograma != null)
        {
            foreach (Transform pai in paisParaHolograma)
            {
                if (pai != null)
                {
                    // 1. Tenta ver se o objeto já tem o script (caso o tenhas posto à mão no Unity)
                    HologramController hc = pai.GetComponent<HologramController>();
                    
                    // 2. Se não tiver o script, cria-o e cola-o no objeto em pleno jogo!
                    if (hc == null)
                    {
                        hc = pai.gameObject.AddComponent<HologramController>();
                    }

                    // 3. Passa-lhe a referência do material azul
                    hc.materialHologramaBase = materialHolograma;
                    
                    // 4. Ativa o script. Isto faz disparar o Start() do HologramController,
                    // que vai varrer os filhos todos, mudar as cores e ativar o glitch de proximidade.
                    hc.enabled = true;
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
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; 

        player.position = targetTP.position;
        player.rotation = targetTP.rotation;

        if (cc != null) cc.enabled = true;
    }
}