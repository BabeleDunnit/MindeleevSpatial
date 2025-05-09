using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

// Face class used for future color mapping (Face=original, Edge=new on edge, Vertex=new on vertex)
public enum FaceKind { Face, Edge, Vertex }


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolyhedronGenerator : MonoBehaviour
{
    /* ------------------------------------------------------------------
     *  STATIC DATA: 5 canonical polyhedra expressed as (vertices, faces)
     * ----------------------------------------------------------------*/
    // Cube (C)
    private static readonly (Vector3[], int[][], FaceKind[]) Cube = (
        new Vector3[]
        {
            new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
            new Vector3(-1, -1, 1),  new Vector3(1, -1, 1),  new Vector3(1, 1, 1),  new Vector3(-1, 1, 1)
        },
        new int[][]
        {
            new int[] {3, 2, 1, 0}, // back -Z
            new int[] {4, 5, 6, 7}, // front +Z
            new int[] {1, 5, 4, 0}, // bottom -Y
            new int[] {3, 7, 6, 2}, // top +Y
            new int[] {4, 7, 3, 0}, // left -X
            new int[] {2, 6, 5, 1}  // right +X
        },
        Enumerable.Repeat(FaceKind.Face, 6).ToArray()
    );

    // Tetrahedron (T)
    private static readonly (Vector3[], int[][], FaceKind[]) Tetrahedron = (
        new Vector3[]
        {
            new Vector3( 1,  1,  1),
            new Vector3(-1, -1,  1),
            new Vector3(-1,  1, -1),
            new Vector3( 1, -1, -1)
        },
        new int[][]
        {
            new int[] {0, 2, 1},
            new int[] {0, 1, 3},
            new int[] {0, 3, 2},
            new int[] {1, 2, 3}
        },
        Enumerable.Repeat(FaceKind.Face, 4).ToArray()
    );

    // Octahedron (O)
    private static readonly (Vector3[], int[][], FaceKind[]) Octahedron = (
        new Vector3[]
        {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0),
            new Vector3(0, -1, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1)
        },
        new int[][]
        {
            new int[] {0, 2, 4}, new int[] {2, 1, 4}, new int[] {1, 3, 4}, new int[] {3, 0, 4},
            new int[] {2, 0, 5}, new int[] {1, 2, 5}, new int[] {3, 1, 5}, new int[] {0, 3, 5}
        },
        Enumerable.Repeat(FaceKind.Face, 8).ToArray()
    );

    // Dodecahedron (D)
    private static readonly (Vector3[], int[][], FaceKind[]) Dodecahedron = (
        new Vector3[]
        {
            new Vector3( 0.618034f,  0.618034f,  0.618034f), new Vector3( 0.618034f,  0.618034f, -0.618034f),
            new Vector3( 0.618034f, -0.618034f,  0.618034f), new Vector3( 0.618034f, -0.618034f, -0.618034f),
            new Vector3(-0.618034f,  0.618034f,  0.618034f), new Vector3(-0.618034f,  0.618034f, -0.618034f),
            new Vector3(-0.618034f, -0.618034f,  0.618034f), new Vector3(-0.618034f, -0.618034f, -0.618034f),
            new Vector3( 0f,  0.381966f,  1.0f),      new Vector3( 0f,  0.381966f, -1.0f),
            new Vector3( 0f, -0.381966f,  1.0f),      new Vector3( 0f, -0.381966f, -1.0f),
            new Vector3( 0.381966f,  1.0f, 0f),       new Vector3( 0.381966f, -1.0f, 0f),
            new Vector3(-0.381966f,  1.0f, 0f),       new Vector3(-0.381966f, -1.0f, 0f),
            new Vector3( 1.0f, 0f,  0.381966f),       new Vector3( 1.0f, 0f, -0.381966f),
            new Vector3(-1.0f, 0f,  0.381966f),       new Vector3(-1.0f, 0f, -0.381966f)
        },
        new int[][]
        {
            new int[] {0, 8, 10, 2, 16}, new int[] {0, 16, 17, 1, 12}, new int[] {0, 12, 14, 4, 8},
            new int[] {1, 17, 3, 11, 9}, new int[] {1, 9, 5, 14, 12},  new int[] {2, 10, 6, 15, 13},
            new int[] {2, 13, 3, 17, 16}, new int[] {3, 13, 15, 7, 11}, new int[] {4, 14, 5, 19, 18},
            new int[] {4, 18, 6, 10, 8},  new int[] {5, 9, 11, 7, 19},  new int[] {6, 18, 19, 7, 15}
        },
        Enumerable.Repeat(FaceKind.Face, 12).ToArray()
    );

    // Icosahedron (I)
    private static readonly (Vector3[], int[][], FaceKind[]) Icosahedron = (
        new Vector3[]
        {
            new Vector3(-1,  1.618034f,  0).normalized,
            new Vector3( 1,  1.618034f,  0).normalized,
            new Vector3(-1, -1.618034f,  0).normalized,
            new Vector3( 1, -1.618034f,  0).normalized,
            new Vector3( 0, -1,  1.618034f).normalized,
            new Vector3( 0,  1,  1.618034f).normalized,
            new Vector3( 0, -1, -1.618034f).normalized,
            new Vector3( 0,  1, -1.618034f).normalized,
            new Vector3( 1.618034f,  0, -1).normalized,
            new Vector3( 1.618034f,  0,  1).normalized,
            new Vector3(-1.618034f,  0, -1).normalized,
            new Vector3(-1.618034f,  0,  1).normalized
        },
        new int[][]
        {
            new int[]{0,11,5}, new int[]{0,5,1}, new int[]{0,1,7}, new int[]{0,7,10}, new int[]{0,10,11},
            new int[]{1,5,9},  new int[]{5,11,4}, new int[]{11,10,2}, new int[]{10,7,6}, new int[]{7,1,8},
            new int[]{3,9,4},  new int[]{3,4,2},  new int[]{3,2,6},  new int[]{3,6,8},  new int[]{3,8,9},
            new int[]{4,9,5},  new int[]{2,4,11}, new int[]{6,2,10}, new int[]{8,6,7}, new int[]{9,8,1}
        },
        Enumerable.Repeat(FaceKind.Face, 20).ToArray()
    );

    /* ------------------------------------------------------------------
     *  Inspector settings
     * ----------------------------------------------------------------*/
    [Header("Recipe (e.g. kC, kkT, C)")]
    public string polyhedronRecipe = "C"; // default Cube

    [Header("Material Settings")]
    public Color meshColor = Color.cyan;

    /* ------------------------------------------------------------------ */
    void Start()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        filter.mesh = ParsePolyhedronRecipe(polyhedronRecipe);
        ApplySimpleMaterial(renderer, meshColor);
    }

    /* ------------------------- MATERIAL ------------------------------ */
    void ApplySimpleMaterial(MeshRenderer renderer, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("No compatible shader found. Ensure URP or Standard pipeline is configured.");
            return;
        }
        var material = new Material(shader) { color = color };
        renderer.material = material;
    }

    /* ---------------------- PARSE RECIPE ----------------------------- */
    Mesh ParsePolyhedronRecipe(string recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe)) recipe = "C";
        recipe = recipe.Trim();

        // Locate last uppercase letter = base polyhedron
        int basePos = recipe.Length - 1;
        while (basePos >= 0 && !char.IsUpper(recipe[basePos])) basePos--;
        if (basePos < 0) { Debug.LogError("No base polyhedron in recipe – defaulting to Cube"); recipe += "C"; basePos = recipe.Length - 1; }

        char baseChar = recipe[basePos];
        (Vector3[], int[][], FaceKind[]) current = baseChar switch
        {
            'C' => Cube,
            'T' => Tetrahedron,
            'O' => Octahedron,
            'D' => Dodecahedron,
            'I' => Icosahedron,
            _   => Cube
        };

        // Parse tokens to the left of base char (left -> right), collect list
        var tokens = new List<(char op, float factor)>();
        int i = 0;
        while (i < basePos)
        {
            char c = recipe[i];
            if (char.IsLower(c))
            {
                // read optional signed decimal right after operator
                int j = i + 1;
                while (j < basePos && (char.IsDigit(recipe[j]) || recipe[j] == '.' || recipe[j] == '-')) j++;
                string numStr = recipe.Substring(i + 1, j - (i + 1));
                float factor = 0.2f;
                if (!string.IsNullOrEmpty(numStr))
                {
                    if (!float.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out factor)) factor = 0.2f;
                }
                tokens.Add((c, factor));
                i = j;
            }
            else { i++; }
        }

        // Apply operators in reverse order (right‑to‑left)
        for (int t = tokens.Count - 1; t >= 0; t--)
        {
            var (op, factor) = tokens[t];
            current = op switch
            {
                'k' => ApplyKis(current, factor),
                't' => ApplyTruncate(current, factor),
                _   => current
            };
        }

        return ApplyFlatShade(current);
    }

    /* ---------------------- OPERATORS -------------------------------- */
    public static (Vector3[], int[][], FaceKind[]) ApplyKis((Vector3[], int[][], FaceKind[]) input, float heightFactor = 0.2f)
    {
        var (vertices, faces, kinds) = input;
        var newVertices = new List<Vector3>(vertices);
        var newFaces    = new List<int[]>();
        var newKinds    = new List<FaceKind>();

        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            // Face center
            Vector3 center = Vector3.zero;
            foreach (int idx in face) center += vertices[idx];
            center /= face.Length;

            // Average normal (fan method)
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < face.Length; i++)
            {
                Vector3 v0 = vertices[face[i]];
                Vector3 v1 = vertices[face[(i + 1) % face.Length]];
                normal += Vector3.Cross(v1 - center, v0 - center);
            }
            normal.Normalize();
            center -= normal * heightFactor;

            int centerIndex = newVertices.Count;
            newVertices.Add(center);

            // For each edge produce triangle and FaceKind same as parent (Face)
            for (int i = 0; i < face.Length; i++)
            {
                int i0 = face[i];
                int i1 = face[(i + 1) % face.Length];
                newFaces.Add(new int[] { centerIndex, i0, i1 });
                newKinds.Add(FaceKind.Face); // Kis does not introduce new kinds
            }
        }

        return (newVertices.ToArray(), newFaces.ToArray(), newKinds.ToArray());
    }

    // Truncate operator: faithful implementation of Polyhedronisme
public static (Vector3[], int[][], FaceKind[]) ApplyTruncate((Vector3[], int[][], FaceKind[]) input, float t = 0.333f)
{
    t = Mathf.Clamp(t, 0.0001f, 0.5f);
    var (vertices, faces, kinds) = input;
    var newVertices = new List<Vector3>(vertices);
    var newFaces = new List<int[]>();
    var newKinds = new List<FaceKind>();

    Dictionary<(int, int), int> edgeMidpoints = new();
    int GetMidpoint(int a, int b)
    {
        var key = (Mathf.Min(a, b), Mathf.Max(a, b));
        if (!edgeMidpoints.TryGetValue(key, out int mid))
        {
            mid = newVertices.Count;
            newVertices.Add(Vector3.Lerp(vertices[a], vertices[b], t));
            edgeMidpoints[key] = mid;
        }
        return mid;
    }

    foreach (var face in faces)
    {
        var shrunkFace = new int[face.Length];
        for (int i = 0; i < face.Length; i++)
        {
            shrunkFace[i] = GetMidpoint(face[i], face[(i + 1) % face.Length]);
        }
        newFaces.Add(shrunkFace);
        newKinds.Add(FaceKind.Face);
    }

    var vertexRings = new Dictionary<int, List<int>>();
    foreach (var face in faces)
    {
        int count = face.Length;
        for (int i = 0; i < count; i++)
        {
            int v = face[i];
            if (!vertexRings.ContainsKey(v)) vertexRings[v] = new();
            vertexRings[v].Add(GetMidpoint(face[i], face[(i + 1) % count]));
            vertexRings[v].Add(GetMidpoint(face[i], face[(i - 1 + count) % count]));
        }
    }

/*
    foreach (var (v, ring) in vertexRings)
    {
        Vector3 normal = vertices[v].normalized;
        Vector3 axisX = Vector3.Cross(normal, Vector3.up);
        if (axisX.sqrMagnitude < 1e-6f)
            axisX = Vector3.Cross(normal, Vector3.right);
        axisX.Normalize();
        Vector3 axisY = Vector3.Cross(normal, axisX);

        var orderedRing = ring.Distinct().OrderBy(mid =>
        {
            var vec = newVertices[mid] - vertices[v];
            return Mathf.Atan2(Vector3.Dot(vec, axisY), Vector3.Dot(vec, axisX));
        }).ToArray();

        newFaces.Add(orderedRing);
        newKinds.Add(FaceKind.Vertex);
    }
*/

    return (newVertices.ToArray(), newFaces.ToArray(), newKinds.ToArray());
}

    /* ---------------------- FINAL FLAT SHADE ------------------------ */
    public static Mesh ApplyFlatShade((Vector3[], int[][], FaceKind[]) input)
    {
        var (baseVertices, faces, kinds) = input;

        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            for (int i = 1; i < face.Length - 1; i++)
            {
                Vector3 v0 = baseVertices[face[0]];
                Vector3 v1 = baseVertices[face[i]];
                Vector3 v2 = baseVertices[face[i + 1]];
                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                int start = vertices.Count;
                vertices.Add(v0); normals.Add(normal);
                vertices.Add(v1); normals.Add(normal);
                vertices.Add(v2); normals.Add(normal);
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(normals);
        return mesh;
    }
}

