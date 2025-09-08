using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CircleRenderer_unused : MonoBehaviour
{
    [Range(0.01f, 5f)]
    public float radius = 1f;

    [Range(3, 128)]
    int segments;

    [Range(0.001f, 0.2f)]
    float lineWidth = 0.05f;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = false;   // così resta relativo all'oggetto
        lr.loop = true;             // chiude il cerchio
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        // Puoi cambiare materiale in Inspector (default = unlit/white)
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = Color.blue;

        // DrawCircle();
    }

    /*
        void OnValidate()
        {
            if (lr == null) lr = GetComponent<LineRenderer>();
            DrawCircle();
        }
    */
}


