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

        // Definire le facce in senso orario (visto dall'esterno del cubo)
        int[][] faces = new int[][]
        {
            new int[] {3, 2, 1, 0}, // back (faccia -Z)
            new int[] {4, 5, 6, 7}, // front (faccia +Z)
            new int[] {1, 5, 4, 0}, // bottom (faccia -Y)
            new int[] {3, 7, 6, 2}, // top (faccia +Y)
            new int[] {4, 7, 3, 0}, // left (faccia -X)
            new int[] {2, 6, 5, 1}  // right (faccia +X)
        };

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        foreach (var face in faces)
        {
            // Triangolazione delle facce in senso antiorario per garantire normali corrette
            for (int i = 1; i < face.Length - 1; i++)
            {
                tris.Add(face[0]);
                tris.Add(face[i]);
                tris.Add(face[i + 1]);
            }
        }

        return CreateFlatShadedMesh(baseVertices, tris.ToArray());
    }

    Mesh CreateTetrahedron()
    {
        Vector3[] baseVertices = {
            new Vector3(1, 1, 1),
            new Vector3(-1, -1, 1),
            new Vector3(-1, 1, -1),
            new Vector3(1, -1, -1)
        };

        int[][] faces = new int[][]
        {
            new int[] {0, 2, 1},
            new int[] {0, 1, 3},
            new int[] {0, 3, 2},
            new int[] {1, 2, 3}
        };

        List<int> tris = new List<int>();
        foreach (var face in faces)
        {
            tris.Add(face[0]);
            tris.Add(face[1]);
            tris.Add(face[2]);
        }

        return CreateFlatShadedMesh(baseVertices, tris.ToArray());
    }

    Mesh CreateOctahedron()
    {
        Vector3[] baseVertices = {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0),
            new Vector3(0, -1, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1)
        };

        int[][] faces = new int[][]
        {
            new int[] {0, 2, 4},
            new int[] {2, 1, 4},
            new int[] {1, 3, 4},
            new int[] {3, 0, 4},
            new int[] {2, 0, 5},
            new int[] {1, 2, 5},
            new int[] {3, 1, 5},
            new int[] {0, 3, 5}
        };

        List<int> tris = new List<int>();
        foreach (var face in faces)
        {
            tris.Add(face[0]);
            tris.Add(face[1]);
            tris.Add(face[2]);
        }

        return CreateFlatShadedMesh(baseVertices, tris.ToArray());
    }

    Mesh CreateDodecahedron()
    {
        float phi = (1 + Mathf.Sqrt(5)) / 2f;

        Vector3[] vertices = new Vector3[]
        {
            new Vector3( 1,  1,  1), new Vector3( 1,  1, -1), new Vector3( 1, -1,  1), new Vector3( 1, -1, -1),
            new Vector3(-1,  1,  1), new Vector3(-1,  1, -1), new Vector3(-1, -1,  1), new Vector3(-1, -1, -1),
            new Vector3(0,  1 / phi,  phi), new Vector3(0,  1 / phi, -phi), new Vector3(0, -1 / phi,  phi), new Vector3(0, -1 / phi, -phi),
            new Vector3( 1 / phi,  phi, 0), new Vector3( 1 / phi, -phi, 0), new Vector3(-1 / phi,  phi, 0), new Vector3(-1 / phi, -phi, 0),
            new Vector3( phi, 0,  1 / phi), new Vector3( phi, 0, -1 / phi), new Vector3(-phi, 0,  1 / phi), new Vector3(-phi, 0, -1 / phi)
        };

        int[][] faces = new int[][]
        {
            new int[] {0, 8, 10, 2, 16},
            new int[] {0, 16, 17, 1, 12},
            new int[] {0, 12, 14, 4, 8},
            new int[] {1, 17, 3, 11, 9},
            new int[] {1, 9, 5, 14, 12},
            new int[] {2, 10, 6, 15, 13},
            new int[] {2, 13, 3, 17, 16},
            new int[] {3, 13, 15, 7, 11},
            new int[] {4, 14, 5, 19, 18},
            new int[] {4, 18, 6, 10, 8},
            new int[] {5, 9, 11, 7, 19},
            new int[] {6, 18, 19, 7, 15}
        };

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        foreach (var face in faces)
        {
            Vector3 center = Vector3.zero;
            foreach (int idx in face)
                center += vertices[idx];
            center /= face.Length;

            int centerIndex = verts.Count;
            verts.Add(center);

            for (int i = 0; i < face.Length; i++)
            {
                Vector3 v0 = vertices[face[i]];
                Vector3 v1 = vertices[face[(i + 1) % face.Length]];

                verts.Add(v0);
                verts.Add(v1);

                tris.Add(centerIndex);
                tris.Add(centerIndex + 1 + i * 2);
                tris.Add(centerIndex + 2 + i * 2);
            }
        }

        return CreateFlatShadedMesh(verts.ToArray(), tris.ToArray());
    }

    Mesh CreateIcosahedron()
    {
        float t = (1 + Mathf.Sqrt(5)) / 2;
        Vector3[] baseVertices = new Vector3[]
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

        int[][] faces = new int[][]
        {
            new int[] {0, 11, 5}, new int[] {0, 5, 1}, new int[] {0, 1, 7}, new int[] {0, 7, 10}, new int[] {0, 10, 11},
            new int[] {1, 5, 9}, new int[] {5, 11, 4}, new int[] {11, 10, 2}, new int[] {10, 7, 6}, new int[] {7, 1, 8},
            new int[] {3, 9, 4}, new int[] {3, 4, 2}, new int[] {3, 2, 6}, new int[] {3, 6, 8}, new int[] {3, 8, 9},
            new int[] {4, 9, 5}, new int[] {2, 4, 11}, new int[] {6, 2, 10}, new int[] {8, 6, 7}, new int[] {9, 8, 1}
        };

        List<int> tris = new List<int>();
        foreach (var face in faces)
        {
            tris.Add(face[0]);
            tris.Add(face[1]);
            tris.Add(face[2]);
        }

        return CreateFlatShadedMesh(baseVertices, tris.ToArray());
    }

    Mesh ApplyKis(Vector3[] vertices, int[] triangles, float heightFactor = 1f)
    {
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            // Centroide
            Vector3 center = (v0 + v1 + v2) / 3f;
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            center += normal * heightFactor;

            int centerIndex = newVertices.Count;
            newVertices.Add(center);

            int i0n = newVertices.Count; newVertices.Add(v0);
            int i1n = newVertices.Count; newVertices.Add(v1);
            int i2n = newVertices.Count; newVertices.Add(v2);

            newTriangles.Add(centerIndex); newTriangles.Add(i0n); newTriangles.Add(i1n);
            newTriangles.Add(centerIndex); newTriangles.Add(i1n); newTriangles.Add(i2n);
            newTriangles.Add(centerIndex); newTriangles.Add(i2n); newTriangles.Add(i0n);
        }

        return CreateFlatShadedMesh(newVertices.ToArray(), newTriangles.ToArray());
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