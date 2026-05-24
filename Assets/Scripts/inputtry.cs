using UnityEngine;
using UnityEngine.InputSystem;

public class AnomalyTrigger : MonoBehaviour
{
    [Header("The Button to Press")]
    public InputActionProperty triggerButton; // E.g., The 'A' button on your right controller

    [Header("The Target")]
    public KerrAnomaly anomalyScript; // The script on your black hole sphere

    private void OnEnable()
    {
        // Listen for the exact moment the button is pressed down
        triggerButton.action.Enable(); 
        
        // Listen for the press
        triggerButton.action.performed += OnTriggerPressed;
    }

    private void OnDisable()
    {
        // Always clean up the listener to prevent memory leaks
        triggerButton.action.performed -= OnTriggerPressed;
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        if (anomalyScript != null)
        {
            Debug.Log("EL PSY KONGROO: Anomaly Activated!");
            anomalyScript.ActivateAnomaly();
            
            // Optional: Disable the button so you can't accidentally trigger the coroutine twice
            triggerButton.action.Disable(); 
        }
    }
}