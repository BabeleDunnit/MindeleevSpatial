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
    private Vector3 nucleusPosition;

    public void Initialize(int qNumber, float radius, int shape, Vector3 nucleusPos)
    {
        quantumNumber = qNumber;
        orbitalShape = shape;
        rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 1f / qNumber; // I polytroni più alti hanno massa minore
        stableOrbitRadius = radius;
        nucleusPosition = nucleusPos;
    }

    void FixedUpdate()
    {
        timeElapsed += Time.fixedDeltaTime;
        ApplyOrbitalMotion();
    }

    void ApplyOrbitalMotion()
    {
        float x, y, z;
        float freqX = (orbitalShape + 1f) / (orbitalShape + 2f);
        float freqY = (orbitalShape + 2f) / (orbitalShape + 3f);
        float freqZ = (orbitalShape + 3f) / (orbitalShape + 4f);

        x = stableOrbitRadius * Mathf.Sin(freqX * timeElapsed * Mathf.PI * 2);
        y = stableOrbitRadius * Mathf.Cos(freqY * timeElapsed * Mathf.PI * 2);
        z = stableOrbitRadius * Mathf.Sin(freqZ * timeElapsed * Mathf.PI * 2);
        
        transform.position = nucleusPosition + new Vector3(x, y, z);
    }
}
