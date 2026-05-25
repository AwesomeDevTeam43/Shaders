using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.XR.CoreUtils; // NEW: Required for official flawless VR teleportation

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

    [Header("Teleport Settings (Official API)")]
    [Tooltip("Drag the top-level XR Origin object here to link the XROrigin component")]
    [SerializeField] private XROrigin xrOrigin; 
    [SerializeField] private Transform cloneRoomTP;
    [SerializeField] private Transform startRoomTP;

    [Header("Worldline State")]
    [SerializeField] private CCTVGlitchEffect cctvEffect;
    [SerializeField] private GameObject[] objectsOff;
    [SerializeField] private GameObject[] objectsOn;

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

    private void OnGrabbed(SelectEnterEventArgs args) { isHeld = true; }
    private void OnReleased(SelectExitEventArgs args) { isHeld = false; }

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
        // FORCE THE COMPONENT ON so the shader actually runs even if unchecked in the Inspector
        readingSteinerEffect.enabled = true; 

        // 1. Build-up
        float elapsed = 0f;
        while (elapsed < rampDuration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / rampDuration);
            readingSteinerEffect.currentIntensity = Mathf.Lerp(0f, maxIntensity, percent * percent * percent);
            yield return null;
        }

        // 2. Climax
        readingSteinerEffect.currentIntensity = maxIntensity;
        yield return new WaitForSeconds(0.4f);

        // 3. Teleport to Clone Room
        if (xrOrigin != null && cloneRoomTP != null) 
            ProVRTeleport(cloneRoomTP);
            
        if (cctvEffect != null) cctvEffect.enabled = true;
        
        readingSteinerEffect.currentIntensity = 0f; 
        readingSteinerEffect.enabled = false; // Turn off to save performance

        // 4. Psychological Shock (Wait in the Tesseract)
        yield return new WaitForSeconds(5.0f);

        // 5. Violent Snap Back
        readingSteinerEffect.enabled = true; // FORCE ON AGAIN
        readingSteinerEffect.currentIntensity = maxIntensity;
        yield return new WaitForSeconds(0.2f);

        // 6. Establish New Worldline
        if (xrOrigin != null && startRoomTP != null) 
            ProVRTeleport(startRoomTP);
            
        foreach (var obj in objectsOff) if (obj != null) obj.SetActive(false);
        foreach (var obj in objectsOn) if (obj != null) obj.SetActive(true);

        readingSteinerEffect.currentIntensity = 0f;
        readingSteinerEffect.enabled = false; // Turn off for good
        
        shiftCoroutine = null;
    }

    // NEW: Flawless Native XR Teleport Math
    private void ProVRTeleport(Transform targetTP)
    {
        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // Disable so physics don't block the teleport

        // Unity's built-in API automatically calculates the camera offset and rotation math safely
        xrOrigin.MoveCameraToWorldLocation(targetTP.position);
        xrOrigin.MatchOriginUpCameraForward(targetTP.up, targetTP.forward);

        if (cc != null) cc.enabled = true;
    }
}