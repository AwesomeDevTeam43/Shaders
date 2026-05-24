using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
public class BlackHoleActivationButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private XRBaseInteractable buttonInteractable;

    [Header("Anomaly")]
    [SerializeField] private GameObject anomalyRoot;
    [SerializeField] private KerrAnomaly anomalyScript;

    [Header("Behavior")]
    [SerializeField] private bool disableButtonAfterUse = true;
    
    public bool IsMicrowaveOn { get; private set; } = false;

    private bool hasTriggered;

    private void Awake()
    {
        if (buttonInteractable == null)
            buttonInteractable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (buttonInteractable != null)
            buttonInteractable.selectEntered.AddListener(OnButtonSelected);
    }

    private void OnDisable()
    {
        if (buttonInteractable != null)
            buttonInteractable.selectEntered.RemoveListener(OnButtonSelected);
    }

    private void OnButtonSelected(SelectEnterEventArgs args)
    {
        if (hasTriggered)
            return;

        StartCoroutine(EnableAndPlayAnomaly());
    }

    private IEnumerator EnableAndPlayAnomaly()
    {
        hasTriggered = true;

        IsMicrowaveOn = true;

        if (anomalyRoot != null && !anomalyRoot.activeSelf)
            anomalyRoot.SetActive(true);

        if (anomalyScript == null && anomalyRoot != null)
            anomalyScript = anomalyRoot.GetComponentInChildren<KerrAnomaly>(true);

        yield return null;

        if (anomalyScript != null)
            anomalyScript.ActivateAnomaly();

        if (disableButtonAfterUse && buttonInteractable != null)
            buttonInteractable.enabled = false;
    }
}
