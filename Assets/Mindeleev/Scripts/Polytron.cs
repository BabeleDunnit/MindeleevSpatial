using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Polytron : MonoBehaviour
{
    public int quantumNumber;
    public int orbitalShape;
    private Rigidbody rb;
    private float stableOrbitRadius;
    private float timeElapsed = 0f;

    public void Initialize(int qNumber, float radius, int shape)
    {
        quantumNumber = qNumber;
        orbitalShape = shape;
        rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 1f / qNumber; // I polytroni più alti hanno massa minore
        stableOrbitRadius = radius;
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        ApplyOrbitalMotion();
    }

    void ApplyOrbitalMotion()
    {
        Vector3 nucleusPosition = Vector3.zero;
        float x, y;
        float freqX = 1f + orbitalShape * 0.5f;
        float freqY = 1f + (orbitalShape + 1) * 0.3f;

        x = stableOrbitRadius * Mathf.Sin(freqX * timeElapsed);
        y = stableOrbitRadius * Mathf.Cos(freqY * timeElapsed);
        
        transform.position = new Vector3(x, y, 0);
    }
}
