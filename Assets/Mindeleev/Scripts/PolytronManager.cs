using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolytronManager : MonoBehaviour
{
    public GameObject polytronPrefab; // Prefab di base per i polytroni
    private List<Polytron> polytrons = new List<Polytron>();
    private int nextQuantumNumber = 1;
    private float nucleusRadius = 2.0f; // Distanza base degli orbitali
    private float orbitalSpeedFactor = 1.0f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Creazione di un nuovo polytrone
        {
            CreateNewPolytron();
        }
    }

    void CreateNewPolytron()
    {
        int n = nextQuantumNumber; 
        nextQuantumNumber++;

        // Determinazione dell'orbita e posizione iniziale basata su numeri quantici
        int l = (n - 1) % 3; // Numero quantico secondario che determina la forma dell'orbitale
        float radius = nucleusRadius + n; // Orbite discrete basate su numeri interi
        float angle = (n * 137.5f) % 360; // Disposizione a spirale (Golden Angle)

        Vector3 nucleusPosition = transform.position;
        Vector3 position = nucleusPosition + new Vector3(radius * Mathf.Cos(angle), 0, radius * Mathf.Sin(angle));
        GameObject newPolytron = Instantiate(polytronPrefab, position, Quaternion.identity);
        
        Polytron polytronComponent = newPolytron.AddComponent<Polytron>();
        polytronComponent.Initialize(n, radius, l, nucleusPosition);
        polytrons.Add(polytronComponent);
    }
}