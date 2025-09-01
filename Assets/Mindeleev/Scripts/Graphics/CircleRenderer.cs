using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CircleRenderer : MonoBehaviour
{
    [Range(0.01f, 5f)]
    public float radius = 1f;

    [Range(3, 128)]
    int segments = 20;

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

    public void DrawCircle()
    {
        return;
        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0f, y));
        }
    }

    public void DrawLine(Vector3 start, Vector3 end, Color color, float width = 0.05f)
    {
        return;
        var go = new GameObject("Line");
        var lr = go.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
    }

}


