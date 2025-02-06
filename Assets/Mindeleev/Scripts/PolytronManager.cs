using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolytronManager : MonoBehaviour
{
    public GameObject polytronPrefab; // Prefab di base per i polytroni
    private List<Polytron> polytrons = new List<Polytron>();
    private float nucleusRadius = 2.0f; // Distanza base degli orbitali

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Creazione di un nuovo polytrone
        {
            CreateNewPolytron();
        }
    }

    void CreateNewPolytron()
    {
        float radius = nucleusRadius + polytrons.Count + 1; // Orbite discrete basate su numeri interi
        float angle = (polytrons.Count * 137.5f) % 360; // Disposizione a spirale (Golden Angle)

        Vector3 nucleusPosition = transform.position;
        Vector3 position = nucleusPosition + new Vector3(radius * Mathf.Cos(angle), 0, radius * Mathf.Sin(angle));
        GameObject newPolytron = Instantiate(polytronPrefab, position, Quaternion.identity);
        
        Polytron polytronComponent = newPolytron.AddComponent<Polytron>();
        polytronComponent.Initialize(radius, nucleusPosition, polytrons.Count % 3);
        polytrons.Add(polytronComponent);
    }
}
