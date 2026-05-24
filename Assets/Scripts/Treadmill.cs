using UnityEngine;
using System.Collections.Generic;

public class TimelineCloneManager : MonoBehaviour
{
    [Header("The Real You")]
    [Tooltip("Drag your main okabe_wiggle (the one with the IK) here")]
    public Transform sourceRoot; 

    [Header("The Clone")]
    [Tooltip("Drag the stripped-down Okabe_Clone Prefab here")]
    public GameObject clonePrefab; 

    [Header("Tesseract Line Settings")]
    public int clonesInEachDirection = 15; // Spawns 15 in front, 15 behind (30 total)
    public float spacing = 4.0f;           // Distance between you and the next clone

    private List<Transform[]> cloneBonesList = new List<Transform[]>();
    private Transform[] sourceBones;

    void Start()
    {
        // Grab all bones from the real VR Okabe
        sourceBones = sourceRoot.GetComponentsInChildren<Transform>();

        // Generate a single straight line of clones along the Z-axis
        for (int z = -clonesInEachDirection; z <= clonesInEachDirection; z++)
        {
            // CRITICAL: Skip the '0' position so a clone doesn't spawn inside you
            if (z == 0) continue;

            // Offset purely on the Z axis (0 for X and Y)
            Vector3 offset = new Vector3(0, 0, z * spacing);
            SpawnClone(offset);
        }
    }

    void SpawnClone(Vector3 offset)
    {
        // Spawn the clone at the offset distance
        GameObject clone = Instantiate(clonePrefab, transform.position + offset, transform.rotation, transform);
        
        Transform[] cloneBones = clone.GetComponentsInChildren<Transform>();
        
        if (cloneBones.Length == sourceBones.Length)
        {
            cloneBonesList.Add(cloneBones);
        }
        else
        {
            Debug.LogError("Clone bone count does not match! Make sure they are the same 3D model.");
            Destroy(clone);
        }
    }

    void LateUpdate()
    {
        if (sourceBones == null || cloneBonesList.Count == 0) return;

        // Copy every bone rotation/position to all clones instantly
        for (int i = 0; i < sourceBones.Length; i++)
        {
            // Skip the absolute root transform so the clones don't collapse into your position
            if (i == 0) continue; 

            Quaternion sourceRot = sourceBones[i].localRotation;
            Vector3 sourcePos = sourceBones[i].localPosition;

            foreach (Transform[] cloneBones in cloneBonesList)
            {
                cloneBones[i].localRotation = sourceRot;
                cloneBones[i].localPosition = sourcePos;
            }
        }
    }
}