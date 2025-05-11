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


public static (Vector3[], int[][], FaceKind[]) ApplyTruncate((Vector3[], int[][], FaceKind[]) input, float t = 0.333f)
{
    // Clamp t to avoid degenerate geometry
    t = Mathf.Clamp(t, 0.0001f, 0.5f);
    var (vertices, faces, kinds) = input;
    
    // Create collections for new geometry
    var newVertices = new List<Vector3>();
    var newFaces = new List<int[]>();
    var newKinds = new List<FaceKind>();

    // Dictionary to store truncated points: key is (vertex, edge_end_vertex)
    Dictionary<(int, int), int> vertexEdgeToPoint = new Dictionary<(int, int), int>();

    // Function to get or create truncated point
    int GetTruncatedPoint(int vertexIdx, int adjacentIdx)
    {
        var key = (vertexIdx, adjacentIdx);
        
        if (!vertexEdgeToPoint.TryGetValue(key, out int idx))
        {
            // Create point at distance t from vertex along edge to adjacent vertex
            Vector3 vertexPos = vertices[vertexIdx];
            Vector3 adjacentPos = vertices[adjacentIdx];
            Vector3 truncatedPos = Vector3.Lerp(vertexPos, adjacentPos, t);
            
            idx = newVertices.Count;
            newVertices.Add(truncatedPos);
            vertexEdgeToPoint[key] = idx;
        }
        
        return idx;
    }

    // Step 1: Build vertex-to-faces map to efficiently construct vertex figures
    Dictionary<int, List<int>> vertexToFaces = new Dictionary<int, List<int>>();
    
    for (int f = 0; f < faces.Length; f++)
    {
        foreach (int v in faces[f])
        {
            if (!vertexToFaces.ContainsKey(v))
                vertexToFaces[v] = new List<int>();
                
            vertexToFaces[v].Add(f);
        }
    }

    // Step 2: For each original face, create a truncated face with 2n vertices
    for (int f = 0; f < faces.Length; f++)
    {
        int[] face = faces[f];
        int n = face.Length;
        int[] truncatedFace = new int[n * 2];

        for (int i = 0; i < n; i++)
        {
            int current = face[i];
            int next = face[(i + 1) % n];

            int p1 = GetTruncatedPoint(current, next);            // da current verso next
            int p2 = GetTruncatedPoint(next, current);            // da next verso current

            truncatedFace[i * 2] = p1;     // punto sul lato (verso next)
            truncatedFace[i * 2 + 1] = p2; // punto opposto (verso current)
        }

        newFaces.Add(truncatedFace);
        newKinds.Add(FaceKind.Face);
    }

    // Step 3: Create vertex figures (new faces at each truncated vertex)
        for (int v = 0; v < vertices.Length; v++)
        {
            if (!vertexToFaces.TryGetValue(v, out var faceIndices)) continue;

            // Build list of adjacent vertices around this vertex
            List<(Vector3 point, float angle)> sorted = new();
            foreach (int fIdx in faceIndices)
            {
                var face = faces[fIdx];
                int n = face.Length;
                for (int i = 0; i < n; i++)
                {
                    if (face[i] == v)
                    {
                        int prev = face[(i - 1 + n) % n];
                        int next = face[(i + 1) % n];
                        int p1 = GetTruncatedPoint(v, next);
                        Vector3 dir = newVertices[p1] - vertices[v];
                        float angle = Mathf.Atan2(dir.z, dir.x);
                        sorted.Add((newVertices[p1], angle));
                    }
                }
            }
            sorted.Sort((a, b) => a.angle.CompareTo(b.angle));
            int[] vertexFace = sorted.Select(p => newVertices.IndexOf(p.point)).ToArray();

            // Calcolo della normale media attorno al vertice originale
            Vector3 avgNormal = Vector3.zero;
            foreach (var fIdx in faceIndices)
            {
                var fVerts = faces[fIdx];
                int n = fVerts.Length;
                for (int i = 0; i < n; i++)
                {
                    if (fVerts[i] == v)
                    {
                        Vector3 v0 = vertices[fVerts[i]];
                        Vector3 v1 = vertices[fVerts[(i + 1) % n]];
                        Vector3 v2 = vertices[fVerts[(i + n - 1) % n]];
                        avgNormal += Vector3.Cross(v1 - v0, v2 - v0);
                    }
                }
            }
            avgNormal.Normalize();

            // Normale della faccia triangolare appena costruita
            if (vertexFace.Length >= 3)
            {
                Vector3 a = newVertices[vertexFace[0]];
                Vector3 b = newVertices[vertexFace[1]];
                Vector3 c = newVertices[vertexFace[2]];
                Vector3 faceNormal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(faceNormal, avgNormal) < 0)
                    vertexFace = vertexFace.Reverse().ToArray();
            }
            newFaces.Add(vertexFace);
            newKinds.Add(FaceKind.Vertex);
        }
     
        // Step 4: Create edge faces (between truncated points along shared edges)
        var seenEdges = new HashSet<(int, int)>();
        foreach (var face in faces)
        {
            int n = face.Length;
            for (int i = 0; i < n; i++)
            {
                int a = face[i];
                int b = face[(i + 1) % n];
                var edge = a < b ? (a, b) : (b, a);
                if (seenEdges.Contains(edge)) continue;
                seenEdges.Add(edge);

                // Create quad between p_ab, p_ba, and adjacent truncated points
                int p_ab = GetTruncatedPoint(a, b);
                int p_ba = GetTruncatedPoint(b, a);

                // Find next vertex in each direction to get side continuity
                int nextA = -1, nextB = -1;
                foreach (var f in vertexToFaces[a])
                {
                    var fVerts = faces[f];
                    for (int j = 0; j < fVerts.Length; j++)
                    {
                        if (fVerts[j] == a && fVerts[(j + 1) % fVerts.Length] == b)
                        {
                            nextA = fVerts[(j + 2) % fVerts.Length];
                        }
                    }
                }
                foreach (var f in vertexToFaces[b])
                {
                    var fVerts = faces[f];
                    for (int j = 0; j < fVerts.Length; j++)
                    {
                        if (fVerts[j] == b && fVerts[(j + 1) % fVerts.Length] == a)
                        {
                            nextB = fVerts[(j + 2) % fVerts.Length];
                        }
                    }
                }

                // Fallback: skip if not enough info
                if (nextA == -1 || nextB == -1) continue;

                int pa2 = GetTruncatedPoint(a, nextA);
                int pb2 = GetTruncatedPoint(b, nextB);

                int[] edgeFace = new int[] { p_ab, pa2, pb2, p_ba };
                newFaces.Add(edgeFace);
                newKinds.Add(FaceKind.Edge);
            }
        
    }
    
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

