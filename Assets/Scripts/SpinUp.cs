using UnityEngine;
using System.Collections;

public class KerrAnomaly : MonoBehaviour
{
    private Material blackHoleMat;
    private float targetMass = 0.08f;
    private float targetSpin = 5.94f;

    void Start()
    {
        blackHoleMat = GetComponent<MeshRenderer>().material;
        // Start completely dormant (invisible)
        blackHoleMat.SetFloat("_Mass", 0f);
        blackHoleMat.SetFloat("_Spin", 0f);
        blackHoleMat.SetFloat("_EventHorizon", 0f);
    }

    public void ActivateAnomaly()
    {
        StartCoroutine(ChargeUpRoutine());
    }

    private IEnumerator ChargeUpRoutine()
    {
        float elapsed = 0f;
        float duration = 3.0f; // Takes 3 seconds to fully form

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            // Exponentially ramp up the distortion for a violent "snap" into existence
            float currentMass = Mathf.Lerp(0f, targetMass, percent * percent);
            float currentSpin = Mathf.Lerp(0f, targetSpin, percent);
            float currentHorizon = Mathf.Lerp(0f, 0.02f, percent * percent);

            blackHoleMat.SetFloat("_Mass", currentMass);
            blackHoleMat.SetFloat("_Spin", currentSpin);
            blackHoleMat.SetFloat("_EventHorizon", currentHorizon);

            yield return null;
        }
    }
}