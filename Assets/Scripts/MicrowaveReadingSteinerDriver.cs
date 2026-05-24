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

    private Coroutine rampCoroutine;
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
        if (grabInteractable == null)
            return;

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        if (grabInteractable == null)
            return;

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

        if (rampCoroutine != null)
        {
            StopCoroutine(rampCoroutine);
            rampCoroutine = null;
        }
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (!isHeld)
            return;

        if (microwaveSwitch == null || !microwaveSwitch.IsMicrowaveOn)
            return;

        if (readingSteinerEffect == null)
            return;

        if (rampCoroutine != null)
            StopCoroutine(rampCoroutine);

        rampCoroutine = StartCoroutine(RampReadingSteiner());
    }

    private IEnumerator RampReadingSteiner()
    {
        float elapsed = 0f;
        float startIntensity = readingSteinerEffect.currentIntensity;

        while (elapsed < rampDuration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / rampDuration);
            float eased = percent * percent * percent;

            readingSteinerEffect.currentIntensity = Mathf.Lerp(startIntensity, maxIntensity, eased);
            yield return null;
        }

        readingSteinerEffect.currentIntensity = maxIntensity;
        rampCoroutine = null;
    }
}
