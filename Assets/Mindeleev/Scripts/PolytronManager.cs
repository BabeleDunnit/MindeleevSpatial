using UnityEngine;
using System.Collections.Generic;


public class PolytronManager : MonoBehaviour
{
    private List<Polytron> polytrons = new List<Polytron>();
    private float nucleusRadius = 2.0f; // Distanza base degli orbitali

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Creazione di un nuovo polytrone
        {
            CreateNewPolytron();
        }
        if (Input.GetKeyDown(KeyCode.Alpha0)) // Creazione di molti polytroni in posizioni stazionarie
        {
            CreateMultiplePolytrons(5000, 0.05f, 0);
        }
    }

    void CreateNewPolytron(float customTimeElapsed = -1f, int primaryAxisOverride = -1)
    {
        float radius = nucleusRadius; // Orbite discrete basate su numeri interi
         // Disposizione a spirale (Golden Angle)

        Vector3 nucleusPosition = transform.position;
        Vector3 position = nucleusPosition;

        GameObject newPolytron = new GameObject("Polytron");
        newPolytron.transform.SetParent(transform);
        newPolytron.transform.position = position;
        newPolytron.transform.localScale = Vector3.one * 0.5f; // Imposta la scala di base dei polytroni
        //newPolytron.AddComponent<MeshFilter>();
        //newPolytron.AddComponent<MeshRenderer>();
        newPolytron.AddComponent<Rigidbody>().useGravity = false;

        Polytron polytronComponent = newPolytron.AddComponent<Polytron>();
        polytronComponent.Initialize(radius, nucleusPosition, primaryAxisOverride >= 0 ? primaryAxisOverride : polytrons.Count % 3, customTimeElapsed);
        // polytronComponent.ApplyShapeAndColor();

        // si puo anche instanziare di botto così
        // GameObject newInstancePolytron = Instantiate(newPolytron, position, Quaternion.identity);

        polytrons.Add(polytronComponent);
    }

    private void CreateMultiplePolytrons(int count, float interval, int primaryAxis)
    {
        for (int i = 0; i < count; i++)
        {
            CreateNewPolytron(i * interval, primaryAxis);
        }
    }
}
