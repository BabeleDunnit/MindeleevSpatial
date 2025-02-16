using UnityEngine;
using System.Collections.Generic;

public class Polytron : MonoBehaviour
{
    private Rigidbody rb;
    private float stableOrbitRadius;
    private float timeElapsed = 0f;
    private Vector3 nucleusPosition;
    private int primaryAxis; // Determina l'asse principale di oscillazione

    // this gives different zeros and max amplitude positions
    private float modulationFrequencyMultiplier = 2f;   
    private float modulationPhase = Mathf.PI / 2.0f;
    private bool useRealTimeElapsed = true;

    public void Initialize(float radius, Vector3 nucleusPos, int primaryAxisIndex, float customTimeElapsed)
    {
        rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        stableOrbitRadius = radius;
        nucleusPosition = nucleusPos;
        primaryAxis = primaryAxisIndex;
        
        if (customTimeElapsed >= 0)
        {
            timeElapsed = customTimeElapsed;
            useRealTimeElapsed = false;
            ApplyOrbitalMotion();
        }
    }

    void FixedUpdate()
    {
        if (useRealTimeElapsed)
        {
            timeElapsed += Time.fixedDeltaTime;
            ApplyOrbitalMotion();
        }
    }

    void ApplyOrbitalMotion()
    {
        float x = 0, y = 0, z = 0;
        float baseFreq = 1.0f;
        // float zeroDisplacement = 0.5f;
        float axisOscillation = stableOrbitRadius * 1.5f * Mathf.Sin(baseFreq * timeElapsed * Mathf.PI * 2 * 0.01f);
        float axisModulation =  Mathf.Sin(modulationFrequencyMultiplier * axisOscillation + modulationPhase);
        if(axisModulation < 0.1f)
        {
            axisModulation = 0.1f;
        }

        if (primaryAxis == 0) // X asse principale, Y-Z oscillano
        {
            x = axisOscillation;
            y = stableOrbitRadius * axisModulation * Mathf.Sin(baseFreq * timeElapsed * Mathf.PI * 2);
            z = stableOrbitRadius * axisModulation * Mathf.Sin(baseFreq * timeElapsed * Mathf.PI * 2 + Mathf.PI / 2);
        }
        else if (primaryAxis == 1) // Y asse principale, X-Z oscillano
        {
            x = stableOrbitRadius * axisModulation * Mathf.Sin(baseFreq * timeElapsed * Mathf.PI * 2);
            y = axisOscillation;
            z = stableOrbitRadius * axisModulation * Mathf.Sin(baseFreq * timeElapsed * Mathf.PI * 2 + Mathf.PI / 2);
        }
        else // Z asse principale, X-Y oscillano
        {
            x = stableOrbitRadius * axisModulation * Mathf.Sin(baseFreq * timeElapsed * Mathf.PI * 2);
            y = stableOrbitRadius * axisModulation * Mathf.Sin(baseFreq * timeElapsed * Mathf.PI * 2 + Mathf.PI / 2);
            z = axisOscillation;
        }
        
        transform.position = nucleusPosition + new Vector3(x, y, z);
    }
}
