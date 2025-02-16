using UnityEngine;
using System.Collections.Generic;

public class Polytron : MonoBehaviour
{
    private Rigidbody rb;
    private float stableOrbitRadius;
    private float timeElapsed = 0f;
    private Vector3 nucleusPosition;
    private int primaryAxis; // Determina l'asse principale di oscillazione
    private float modulationFrequencyMultiplier = 2.0f;
    private float modulationPhase = Mathf.PI / 2;
    private bool useRealTimeElapsed = true;

    public enum PolytronShape { Sphere, Cube, Tetrahedron, Octahedron, Dodecahedron, Icosahedron }
    private PolytronShape shape;
    private Color color;

    public void Initialize(float radius, Vector3 nucleusPos, int primaryAxisIndex, float customTimeElapsed)
    {
        Debug.Log("Initialize");
        // rb = gameObject.AddComponent<Rigidbody>();
        // rb.useGravity = false;
        stableOrbitRadius = radius;
        nucleusPosition = nucleusPos;
        primaryAxis = primaryAxisIndex;
        
        if (customTimeElapsed >= 0)
        {
            timeElapsed = customTimeElapsed;
            useRealTimeElapsed = false;
            ApplyOrbitalMotion();
        }

        SetRandomShapeAndColor();
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
        float axisOscillation = stableOrbitRadius * 1.5f * Mathf.Sin(baseFreq * timeElapsed * Mathf.PI * 2 * 0.01f);
        float axisModulation = Mathf.Sin(modulationFrequencyMultiplier * axisOscillation + modulationPhase);

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

    private void SetRandomShapeAndColor()
    {
        // shape = (PolytronShape)Random.Range(0, System.Enum.GetValues(typeof(PolytronShape)).Length);
        shape = PolytronShape.Icosahedron;
        // color = new Color(Random.value, Random.value, Random.value, 1.0f); // Colore semitrasparente
        color = new Color(Random.value, Random.value, Random.value, Random.value);
        ApplyShapeAndColor();
    }

    public void ApplyShapeAndColor()
    {
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();

        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        renderer.material.color = color;
        
        switch (shape)
        {
            case PolytronShape.Sphere:
                meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                break;
            case PolytronShape.Cube:
                meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                break;
            case PolytronShape.Tetrahedron:
            case PolytronShape.Octahedron:
            case PolytronShape.Dodecahedron:
            case PolytronShape.Icosahedron:
                GeneratePolyhedronMesh(shape);
                break;
        }
    }

  private void RecalculateFlatNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = new Vector3[vertices.Length];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            normals[triangles[i]] = normal;
            normals[triangles[i + 1]] = normal;
            normals[triangles[i + 2]] = normal;
        }
        mesh.normals = normals;
    }

    private void GeneratePolyhedronMesh(PolytronShape polyShape)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        switch (polyShape)
        {
            case PolytronShape.Tetrahedron:
                mesh.vertices = new Vector3[]
                {
                    new Vector3(1, 1, 1),
                    new Vector3(-1, -1, 1),
                    new Vector3(-1, 1, -1),
                    new Vector3(1, -1, -1)
                };
                mesh.triangles = new int[]
                {
                    0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2
                };
                break;
            case PolytronShape.Octahedron:
                mesh.vertices = new Vector3[]
                {
                    new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
                    new Vector3(0, 1, 0), new Vector3(0, -1, 0),
                    new Vector3(0, 0, 1), new Vector3(0, 0, -1)
                };
                mesh.triangles = new int[]
                {
                    0, 2, 4,  2, 1, 4,  1, 3, 4,  3, 0, 4,
                    0, 5, 2,  2, 5, 1,  1, 5, 3,  3, 5, 0
                };
                break;
            case PolytronShape.Dodecahedron:
                // Dodecahedron implementation (requires 12 pentagonal faces)
                break;
            case PolytronShape.Icosahedron:
                float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
                mesh.vertices = new Vector3[]
                {
                    new Vector3(-1, t, 0).normalized,
                    new Vector3(1, t, 0).normalized,
                    new Vector3(-1, -t, 0).normalized,
                    new Vector3(1, -t, 0).normalized,
                    new Vector3(0, -1, t).normalized,
                    new Vector3(0, 1, t).normalized,
                    new Vector3(0, -1, -t).normalized,
                    new Vector3(0, 1, -t).normalized,
                    new Vector3(t, 0, -1).normalized,
                    new Vector3(t, 0, 1).normalized,
                    new Vector3(-t, 0, -1).normalized,
                    new Vector3(-t, 0, 1).normalized
                };
                mesh.triangles = new int[]
                {
                    0, 11, 5,  0, 5, 1,  0, 1, 7,  0, 7, 10,  0, 10, 11,
                    1, 5, 9,  5, 11, 4,  11, 10, 2,  10, 7, 6,  7, 1, 8,
                    3, 9, 4,  3, 4, 2,  3, 2, 6,  3, 6, 8,  3, 8, 9,
                    4, 9, 5,  2, 4, 11,  6, 2, 10,  8, 6, 7,  9, 8, 1
                };
                break;
        }
        
        //mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        //mesh.RecalculateBounds();
        RecalculateFlatNormals(mesh);
        transform.localScale = Vector3.one * 0.5f;
        meshFilter.mesh = mesh;
    }
    
}
