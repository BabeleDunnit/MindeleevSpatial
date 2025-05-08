using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolyhedronGenerator : MonoBehaviour
{
    public enum PolyhedronType { Tetrahedron, Cube, Octahedron, Dodecahedron, Icosahedron }
    public PolyhedronType polyhedronType = PolyhedronType.Cube;

    [Header("Material Settings")]
    public Color meshColor = Color.cyan;

    void Start()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        filter.mesh = GeneratePolyhedron(polyhedronType);
        ApplySimpleMaterial(renderer, meshColor);
    }

    void ApplySimpleMaterial(MeshRenderer renderer, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            Debug.LogError("No compatible shader found. Please ensure URP or Standard pipeline is configured.");
            return;
        }

        Material material = new Material(shader);
        material.color = color;
        renderer.material = material;
    }

    Mesh GeneratePolyhedron(PolyhedronType type)
    {
        switch (type)
        {
            case PolyhedronType.Tetrahedron:
                return CreateTetrahedron();
            case PolyhedronType.Cube:
                return CreateCube();
            case PolyhedronType.Octahedron:
                return CreateOctahedron();
            case PolyhedronType.Dodecahedron:
                return CreateDodecahedron();
            case PolyhedronType.Icosahedron:
                return CreateIcosahedron();
            default:
                return CreateCube();
        }
    }

    Mesh CreateCube()
    {
        Vector3[] baseVertices = {
            new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
            new Vector3(-1, -1, 1),  new Vector3(1, -1, 1),  new Vector3(1, 1, 1),  new Vector3(-1, 1, 1)
        };

        int[] faceIndices = {
            0, 2, 1, 0, 3, 2, // back
            4, 5, 6, 4, 6, 7, // front
            0, 1, 5, 0, 5, 4, // bottom
            2, 3, 7, 2, 7, 6, // top
            0, 4, 7, 0, 7, 3, // left
            1, 2, 6, 1, 6, 5  // right
        };

        return CreateFlatShadedMesh(baseVertices, faceIndices);
    }

    Mesh CreateTetrahedron()
    {
        Vector3[] baseVertices = {
            new Vector3(1, 1, 1),     // 0
            new Vector3(-1, -1, 1),  // 1
            new Vector3(-1, 1, -1),  // 2
            new Vector3(1, -1, -1)   // 3
        };

        int[] faceIndices = {
            0, 2, 1,  // base
            0, 1, 3,
            0, 3, 2,
            1, 2, 3
        };

        return CreateFlatShadedMesh(baseVertices, faceIndices);
    }

    Mesh CreateOctahedron()
    {
        Vector3[] baseVertices = {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0),
            new Vector3(0, -1, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1)
        };

        int[] faceIndices = {
            0, 2, 4,
            2, 1, 4,
            1, 3, 4,
            3, 0, 4,
            2, 0, 5,
            1, 2, 5,
            3, 1, 5,
            0, 3, 5
        };

        return CreateFlatShadedMesh(baseVertices, faceIndices);
    }

    Mesh CreateDodecahedron()
    {
        float phi = (1 + Mathf.Sqrt(5)) / 2f;

        Vector3[] verts = new Vector3[]
        {
            new Vector3(-1, -1, -1), new Vector3(-1, -1,  1), new Vector3(-1,  1, -1), new Vector3(-1,  1,  1),
            new Vector3( 1, -1, -1), new Vector3( 1, -1,  1), new Vector3( 1,  1, -1), new Vector3( 1,  1,  1),
            new Vector3(0, -1/phi, -phi), new Vector3(0, -1/phi, phi), new Vector3(0, 1/phi, -phi), new Vector3(0, 1/phi, phi),
            new Vector3(-1/phi, -phi, 0), new Vector3(-1/phi, phi, 0), new Vector3(1/phi, -phi, 0), new Vector3(1/phi, phi, 0),
            new Vector3(-phi, 0, -1/phi), new Vector3(phi, 0, -1/phi), new Vector3(-phi, 0, 1/phi), new Vector3(phi, 0, 1/phi)
        };

        int[][] faces = new int[][]
        {
            new int[]{0, 8, 10, 2, 16},
            new int[]{0, 16, 18, 1, 12},
            new int[]{0, 12, 14, 4, 8},
            new int[]{1, 18, 19, 5, 9},
            new int[]{1, 9, 11, 3, 13},
            new int[]{2, 10, 6, 17, 16},
            new int[]{3, 11, 7, 15, 13},
            new int[]{4, 14, 12, 1, 9},
            new int[]{5, 19, 17, 6, 10},
            new int[]{6, 10, 8, 4, 14},
            new int[]{7, 11, 9, 5, 19},
            new int[]{7, 15, 13, 3, 11}
        };

        List<int> triangles = new List<int>();
        foreach (var face in faces)
        {
            for (int i = 1; i < face.Length - 1; i++)
            {
                triangles.Add(face[0]);
                triangles.Add(face[i]);
                triangles.Add(face[i + 1]);
            }
        }

        return CreateFlatShadedMesh(verts, triangles.ToArray());
    }

    Mesh CreateIcosahedron()
    {
        float t = (1 + Mathf.Sqrt(5)) / 2;
        List<Vector3> verts = new List<Vector3>
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

        int[] triangles = {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };

        return CreateFlatShadedMesh(verts.ToArray(), triangles);
    }

    Mesh CreateFlatShadedMesh(Vector3[] baseVertices, int[] faceIndices)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        for (int i = 0; i < faceIndices.Length; i += 3)
        {
            Vector3 v0 = baseVertices[faceIndices[i]];
            Vector3 v1 = baseVertices[faceIndices[i + 1]];
            Vector3 v2 = baseVertices[faceIndices[i + 2]];

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            int index = vertices.Count;
            vertices.Add(v0); normals.Add(normal);
            vertices.Add(v1); normals.Add(normal);
            vertices.Add(v2); normals.Add(normal);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        return mesh;
    }
}
