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

    [Header("Tesseract Performance")]
    [Tooltip("Arrasta o GameObject que tem o script TimelineCloneManager (ou o pai dos clones) para aqui.")]
    [SerializeField] private GameObject cloneManagerRoot;

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
    private bool hasLeaped = false; // LOCK: Impede que o salto aconteça duas vezes

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();

        if (readingSteinerEffect == null)
            readingSteinerEffect = FindFirstObjectByType<ReadingSteinerEffect>();
            
        // Garante que os clones começam desligados para poupar performance logo no início
        if (cloneManagerRoot != null)
            cloneManagerRoot.SetActive(false);
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

    private void OnGrabbed(SelectEnterEventArgs args) { isHeld = true; }
    private void OnReleased(SelectExitEventArgs args) { isHeld = false; }

    private void OnActivated(ActivateEventArgs args)
    {
        if (!isHeld) return;
        if (hasLeaped) return; // Se já viajou, ignora o clique!
        if (microwaveSwitch == null || !microwaveSwitch.IsMicrowaveOn) return;
        if (readingSteinerEffect == null) return;

        if (shiftCoroutine == null)
        {
            Debug.Log("Viagem no tempo iniciada!");
            hasLeaped = true; // Tranca a porta, viagem sem retorno
            shiftCoroutine = StartCoroutine(WorldlineShiftSequence());
        }
    }

    private IEnumerator WorldlineShiftSequence()
    {
        float elapsed = 0f;
        while (elapsed < rampDuration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / rampDuration);
            if (readingSteinerEffect != null)
                readingSteinerEffect.currentIntensity = Mathf.Lerp(0f, maxIntensity, percent * percent * percent);
            
            yield return null;
        }

        if (readingSteinerEffect != null) readingSteinerEffect.currentIntensity = maxIntensity;
        yield return new WaitForSeconds(0.4f);

        // --- ENTRA NO TESSERACT ---
        // Liga os clones exatamente no frame antes do jogador aparecer lá
        if (cloneManagerRoot != null) cloneManagerRoot.SetActive(true);
        if (player != null && cloneRoomTP != null) TeleportPlayer(cloneRoomTP);
        if (cctvEffect != null) cctvEffect.enabled = true;
        
        if (readingSteinerEffect != null) readingSteinerEffect.currentIntensity = 0f; 

        yield return new WaitForSeconds(15.0f);

        if (readingSteinerEffect != null) readingSteinerEffect.currentIntensity = maxIntensity;
        yield return new WaitForSeconds(0.2f);

        // --- VOLTA AO LABORATÓRIO ---
        if (player != null && startRoomTP != null) TeleportPlayer(startRoomTP);
        if (cctvEffect != null) cctvEffect.enabled = false;
        
        // Desliga os clones permanentemente para libertar o CPU
        if (cloneManagerRoot != null) cloneManagerRoot.SetActive(false);

        foreach (var obj in objectsOff) if (obj != null) obj.SetActive(false);
        foreach (var obj in objectsOn) if (obj != null) obj.SetActive(true);

        // --- INJETAR SCRIPT DE HOLOGRAMA DINAMICAMENTE ---
        if (materialHolograma != null && paisParaHolograma != null)
        {
            foreach (Transform pai in paisParaHolograma)
            {
                if (pai != null)
                {
                    HologramController hc = pai.GetComponent<HologramController>();
                    if (hc == null) hc = pai.gameObject.AddComponent<HologramController>();
                    hc.materialHologramaBase = materialHolograma;
                    hc.enabled = true;
                }
            }
        }

        if (readingSteinerEffect != null) readingSteinerEffect.currentIntensity = 0f;
        
        // Mantemos shiftCoroutine != null propositadamente aqui no final para garantir 
        // que o código nunca mais entra no bloco inicial.
        Debug.Log("Viagem concluída com sucesso! Worldline shift finalizado.");
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