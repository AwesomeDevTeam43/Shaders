using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class MicrowaveReadingSteinerDriver : MonoBehaviour
{
    [Header("References")]
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

    [Header("Worldline State")]
    [SerializeField] private MonoBehaviour cctvEffect;
    [SerializeField] private GameObject[] objectsOff;
    [SerializeField] private GameObject[] objectsOn;
    
    [Header("Hologramas")]
    [Tooltip("Arrasta para aqui os objetos Pais que têm o script HologramController")]
    [SerializeField] private HologramController[] hologramasParaAtivar;

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
            shiftCoroutine = StartCoroutine(WorldlineShiftSequence());
    }

    private IEnumerator WorldlineShiftSequence()
    {
        // 1. Build-up
        float elapsed = 0f;
        while (elapsed < rampDuration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / rampDuration);
            readingSteinerEffect.currentIntensity = Mathf.Lerp(0f, maxIntensity, percent * percent * percent);
            yield return null;
        }

        // 2. Clímax
        readingSteinerEffect.currentIntensity = maxIntensity;
        yield return new WaitForSeconds(0.4f);

        // 3. Teleporte para Sala dos Clones
        if (player != null && cloneRoomTP != null) TeleportPlayer(cloneRoomTP);
        if (cctvEffect != null) cctvEffect.enabled = true;
        
        readingSteinerEffect.currentIntensity = 0f; 

        // 4. Choque Psicológico
        yield return new WaitForSeconds(5.0f);

        // 5. Regresso Violento
        readingSteinerEffect.currentIntensity = maxIntensity;
        yield return new WaitForSeconds(0.2f);

        // 6. Estabelecer nova Worldline
        if (player != null && startRoomTP != null) TeleportPlayer(startRoomTP);
        if (cctvEffect != null) cctvEffect.enabled = false;

        foreach (var obj in objectsOff) if (obj != null) obj.SetActive(false);
        foreach (var obj in objectsOn) if (obj != null) obj.SetActive(true);

        // -------------------------------------------------------------
        // NOVA LÓGICA: Ativar os Controladores de Holograma
        // -------------------------------------------------------------
        if (hologramasParaAtivar != null)
        {
            foreach (HologramController hc in hologramasParaAtivar)
            {
                if (hc != null)
                {
                    // Ligar o script faz com que o Start() dele corra, 
                    // substituindo os materiais todos automaticamente!
                    hc.enabled = true; 
                }
            }
        }

        readingSteinerEffect.currentIntensity = 0f;
        shiftCoroutine = null;
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