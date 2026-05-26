using UnityEngine;
using UnityEngine.InputSystem;

public class AnomalyTrigger : MonoBehaviour
{
    [Header("The Button to Press")]
    public InputActionProperty triggerButton;
    [Header("The Target")]
    public KerrAnomaly anomalyScript;

    private void OnEnable()
    {
        triggerButton.action.Enable(); 
        
        triggerButton.action.performed += OnTriggerPressed;
    }

    private void OnDisable()
    {
        triggerButton.action.performed -= OnTriggerPressed;
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        if (anomalyScript != null)
        {
            Debug.Log("EL PSY KONGROO: Anomaly Activated!");
            anomalyScript.ActivateAnomaly();
            
            triggerButton.action.Disable(); 
        }
    }
}