using UnityEngine;

public class VRAvatarDriver : MonoBehaviour
{
    [Header("IK Targets (On the Avatar)")]
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("XR Hardware (From XR Origin)")]
    public Transform xrOriginRoot; 
    public Transform xrCamera;
    public Transform xrLeftController;
    public Transform xrRightController;

    [Header("Offsets")]
    public Vector3 headPositionOffset;
    public Vector3 headRotationOffset; // NEW: The Neck Calibrator
    public Vector3 handPositionOffset;
    public Vector3 leftHandRotationOffset;  
    public Vector3 rightHandRotationOffset; 

    [Header("Body Settings")]
    public float turnSmoothness = 5f;
    public float headHeight = 1.6f;
    public float bodyRotationOffset = 180f; 

    void LateUpdate()
    {
        if (xrCamera != null && xrOriginRoot != null)
        {
            // --- THE CROUCH FIX ---
            float currentY = xrCamera.position.y - headHeight;
            Vector3 targetPosition = new Vector3(xrCamera.position.x, currentY, xrCamera.position.z);
            transform.position = targetPosition;

            // --- ROTATE BODY (WITH 180 FLIP) ---
            Vector3 lookDirection = xrCamera.forward;
            lookDirection.y = 0; 
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection) * Quaternion.Euler(0, bodyRotationOffset, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSmoothness);
            }
        }

        // --- SNAP THE TARGETS ---
        
        // Snap Head (With the new Rotation Offset!)
        if (headTarget != null && xrCamera != null)
        {
            headTarget.position = xrCamera.position + headPositionOffset;
            headTarget.rotation = xrCamera.rotation * Quaternion.Euler(headRotationOffset);
        }

        if (leftHandTarget != null && xrLeftController != null)
        {
            leftHandTarget.position = xrLeftController.position + handPositionOffset;
            leftHandTarget.rotation = xrLeftController.rotation * Quaternion.Euler(leftHandRotationOffset);
        }

        if (rightHandTarget != null && xrRightController != null)
        {
            rightHandTarget.position = xrRightController.position + handPositionOffset;
            rightHandTarget.rotation = xrRightController.rotation * Quaternion.Euler(rightHandRotationOffset);
        }
    }
}