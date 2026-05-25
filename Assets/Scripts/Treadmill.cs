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
    public int clonesInEachDirection = 15;
    public float spacing = 4.0f;

    private List<Transform[]> cloneBonesList = new List<Transform[]>();
    private Transform[] sourceBones;

    void Start()
    {
        sourceBones = sourceRoot.GetComponentsInChildren<Transform>();

        for (int z = -clonesInEachDirection; z <= clonesInEachDirection; z++)
        {
            if (z == 0) continue;

            Vector3 offset = new Vector3(0, 0, z * spacing);
            SpawnClone(offset);
        }
    }

    void SpawnClone(Vector3 offset)
    {
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

        for (int i = 0; i < sourceBones.Length; i++)
        {
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