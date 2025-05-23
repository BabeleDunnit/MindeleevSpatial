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

    [Header("Color Settings")]
    public PolyhedronPalette palette;
    public bool useCongruenceColoring = true;
    public float colorSensitivity = 0.001f;

    [Header("Material")]
    public Material polyhedronMaterial; // Add this field

    /* ------------------------------------------------------------------ */
    void Start()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        filter.mesh = ParsePolyhedronRecipe(polyhedronRecipe);
        ApplyPolyhedronMaterial(renderer);
    }

    /* ------------------------- MATERIAL ------------------------------ */
    void ApplyPolyhedronMaterial(MeshRenderer renderer)
    {
        if (polyhedronMaterial == null)
        {
            Debug.LogError("PolyhedronFlatShaded material not assigned in inspector");
            return;
        }

        renderer.material = polyhedronMaterial;
        Debug.Log($"Successfully applied material on {Application.platform}");
    }

    /* ---------------------- PARSE RECIPE ----------------------------- */
    Mesh ParsePolyhedronRecipe(string recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe)) recipe = "C";
        recipe = recipe.Trim();

        // Locate last uppercase letter = base polyhedron
        int basePos = recipe.Length - 1;
        while (basePos >= 0 && !char.IsUpper(recipe[basePos])) basePos--;
        if (basePos < 0) { Debug.LogError("No base polyhedron in recipe - defaulting to Cube"); recipe += "C"; basePos = recipe.Length - 1; }

        char baseChar = recipe[basePos];
        (Vector3[], int[][], FaceKind[]) current = baseChar switch
        {
            'C' => Cube,
            'T' => Tetrahedron,
            'O' => Octahedron,
            'D' => Dodecahedron,
            'I' => Icosahedron,
            _ => Cube
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
                float factor = 0.33f;
                if (!string.IsNullOrEmpty(numStr))
                {
                    if (!float.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out factor)) factor = 0.33f;
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
                _ => current
            };
        }

        return ApplyFlatShade(current, palette, useCongruenceColoring, colorSensitivity);
    }

    /* ---------------------- OPERATORS -------------------------------- */
    public static (Vector3[], int[][], FaceKind[]) ApplyKis((Vector3[], int[][], FaceKind[]) input, float height)
    {
        Debug.Log($"Applying Kis with heightFactor {height}");

        var (vertices, faces, kinds) = input;
        var newVertices = new List<Vector3>(vertices);
        var newFaces = new List<int[]>();
        var newKinds = new List<FaceKind>();

        // For each original face
        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            
            // Calculate face center (apex of the pyramid)
            Vector3 center = Vector3.zero;
            foreach (int idx in face)
                center += vertices[idx];
            center /= face.Length;
            
            // Calculate face normal
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < face.Length; i++)
            {
                Vector3 current = vertices[face[i]];
                Vector3 next = vertices[face[(i + 1) % face.Length]];
                normal.x += (current.y - next.y) * (current.z + next.z);
                normal.y += (current.z - next.z) * (current.x + next.x);
                normal.z += (current.x - next.x) * (current.y + next.y);
            }
            normal.Normalize();
            
            // Add new vertex at face center
            int centerIdx = newVertices.Count;
            newVertices.Add(center + normal * height);
            
            // Create new triangular faces
            for (int i = 0; i < face.Length; i++)
            {
                int next = (i + 1) % face.Length;
                int[] newFace = new int[] { face[i], face[next], centerIdx };
                newFaces.Add(newFace);
                newKinds.Add(FaceKind.Vertex); // These faces come from raising vertices
            }
        }
        
        return (newVertices.ToArray(), newFaces.ToArray(), newKinds.ToArray());
    }


    public static (Vector3[], int[][], FaceKind[]) ApplyTruncate((Vector3[], int[][], FaceKind[]) input, float t)
    {
        Debug.Log($"Applying Truncate with truncationFactor {t}");

        // Clamp t to avoid degenerate geometry
        t = Mathf.Clamp(t, 0.001f, 0.5f);
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

        // Step 1: Build vertex-to-faces and edge-to-faces maps
        Dictionary<int, List<int>> vertexToFaces = new Dictionary<int, List<int>>();
        Dictionary<(int, int), List<int>> edgeToFaces = new Dictionary<(int, int), List<int>>();

        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            int n = face.Length;

            for (int i = 0; i < n; i++)
            {
                int v = face[i];
                if (!vertexToFaces.ContainsKey(v))
                    vertexToFaces[v] = new List<int>();
                vertexToFaces[v].Add(f);

                // Track edges
                int v2 = face[(i + 1) % n];
                var edge = v < v2 ? (v, v2) : (v2, v);
                if (!edgeToFaces.ContainsKey(edge))
                    edgeToFaces[edge] = new List<int>();
                edgeToFaces[edge].Add(f);
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

                int p1 = GetTruncatedPoint(current, next);
                int p2 = GetTruncatedPoint(next, current);

                truncatedFace[i * 2] = p1;
                truncatedFace[i * 2 + 1] = p2;
            }

            newFaces.Add(truncatedFace);
            newKinds.Add(FaceKind.Face);
        }

        // Step 3: Create vertex figures (new faces at each truncated vertex)
        for (int v = 0; v < vertices.Length; v++)
        {
            if (!vertexToFaces.TryGetValue(v, out var faceIndices)) continue;

            // Calculate vertex normal (average of incident face normals)
            Vector3 vertexNormal = Vector3.zero;
            foreach (int fIdx in faceIndices)
            {
                var face = faces[fIdx];
                int idx = System.Array.IndexOf(face, v);
                if (idx >= 0)
                {
                    int n = face.Length;
                    Vector3 prev = vertices[face[(idx + n - 1) % n]];
                    Vector3 curr = vertices[v];
                    Vector3 next = vertices[face[(idx + 1) % n]];
                    Vector3 faceNormal = Vector3.Cross(next - curr, prev - curr).normalized;
                    vertexNormal += faceNormal;
                }
            }
            vertexNormal.Normalize();

            // Find all truncated points around this vertex and sort them
            List<int> vertexFaceIndices = new List<int>();
            List<Vector3> vertexFacePositions = new List<Vector3>();

            foreach (int fIdx in faceIndices)
            {
                var face = faces[fIdx];
                int idx = System.Array.IndexOf(face, v);
                if (idx >= 0)
                {
                    int n = face.Length;
                    int prev = face[(idx + n - 1) % n];
                    int next = face[(idx + 1) % n];

                    int tPrev = GetTruncatedPoint(v, prev);
                    int tNext = GetTruncatedPoint(v, next);

                    if (!vertexFaceIndices.Contains(tPrev))
                    {
                        vertexFaceIndices.Add(tPrev);
                        vertexFacePositions.Add(newVertices[tPrev]);
                    }
                    if (!vertexFaceIndices.Contains(tNext))
                    {
                        vertexFaceIndices.Add(tNext);
                        vertexFacePositions.Add(newVertices[tNext]);
                    }
                }
            }

            // Order points around vertex based on angle in a plane perpendicular to vertex normal
            Vector3 center = Vector3.zero;
            foreach (Vector3 p in vertexFacePositions)
                center += p;
            center /= vertexFacePositions.Count;

            // Find a perpendicular vector to the normal to define a plane
            Vector3 tangent = Vector3.Cross(vertexNormal,
                Mathf.Abs(vertexNormal.x) < 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 bitangent = Vector3.Cross(vertexNormal, tangent);

            // Sort by angle in the tangent/bitangent plane
            List<(int index, float angle)> sortedIndices = new List<(int index, float angle)>();
            for (int i = 0; i < vertexFaceIndices.Count; i++)
            {
                Vector3 dir = vertexFacePositions[i] - center;
                float ax = Vector3.Dot(dir, tangent);
                float ay = Vector3.Dot(dir, bitangent);
                float angle = Mathf.Atan2(ay, ax);
                sortedIndices.Add((vertexFaceIndices[i], angle));
            }
            sortedIndices.Sort((a, b) => a.angle.CompareTo(b.angle));

            // Create the new face with correctly ordered vertices
            int[] newVertexFace = sortedIndices.Select(p => p.index).ToArray();

            // Ensure face normal points outward from the polyhedron center
            Vector3 outwardDir = (center - vertices[v]).normalized;
            Vector3 faceNormalCalc = CalculateFaceNormal(newVertices, newVertexFace);

            // INVERTIAMO la condizione rispetto alla versione precedente
            if (Vector3.Dot(faceNormalCalc, outwardDir) > 0)
                newVertexFace = newVertexFace.Reverse().ToArray();

            newFaces.Add(newVertexFace);
            newKinds.Add(FaceKind.Vertex);
        }

        // Step 4: Create edge faces
        HashSet<(int, int)> processedEdges = new HashSet<(int, int)>();

        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            int n = face.Length;

            for (int i = 0; i < n; i++)
            {
                int a = face[i];
                int b = face[(i + 1) % n];
                var edge = a < b ? (a, b) : (b, a);

                if (processedEdges.Contains(edge)) continue;
                processedEdges.Add(edge);

                if (!edgeToFaces.TryGetValue(edge, out var edgeFaces) || edgeFaces.Count != 2)
                    continue; // Skip non-manifold edges or boundary edges

                // Always use consistent ordering based on the canonical edge direction
                int first = edge.Item1;
                int second = edge.Item2;

                int p_start = GetTruncatedPoint(first, second);
                int p_end = GetTruncatedPoint(second, first);

                // Get adjacent vertices in a consistent order
                List<int> quadPoints = new List<int>();
                quadPoints.Add(p_start);

                // Only process the first face to get one adjacent vertex
                var firstFace = faces[edgeFaces[0]];
                int firstIdx = System.Array.IndexOf(firstFace, first);
                int secondIdx = System.Array.IndexOf(firstFace, second);

                if ((firstIdx + 1) % firstFace.Length == secondIdx)
                {
                    int nextIdx = (secondIdx + 1) % firstFace.Length;
                    quadPoints.Add(GetTruncatedPoint(second, firstFace[nextIdx]));
                }

                quadPoints.Add(p_end);

                // Get the other adjacent vertex from the second face
                var secondFace = faces[edgeFaces[1]];
                firstIdx = System.Array.IndexOf(secondFace, first);
                secondIdx = System.Array.IndexOf(secondFace, second);

                if ((firstIdx + 1) % secondFace.Length == secondIdx)
                {
                    int nextIdx = (secondIdx + 1) % secondFace.Length;
                    quadPoints.Add(GetTruncatedPoint(second, secondFace[nextIdx]));
                }

                // Ensure we have exactly 4 points
                quadPoints = quadPoints.Distinct().ToList();
                if (quadPoints.Count != 4) continue;

                // Calculate edge center and ensure consistent winding
                Vector3 edgeCenter = (vertices[first] + vertices[second]) / 2f;
                Vector3 quadCenter = Vector3.zero;
                foreach (int idx in quadPoints)
                    quadCenter += newVertices[idx];
                quadCenter /= 4;

                Vector3 outwardDir = (quadCenter - edgeCenter).normalized;
                Vector3 quadNormal = CalculateFaceNormal(newVertices, quadPoints.ToArray());

                if (Vector3.Dot(quadNormal, outwardDir) > 0)
                    quadPoints.Reverse();

                newFaces.Add(quadPoints.ToArray());
                newKinds.Add(FaceKind.Edge);
            }
        }

        return (newVertices.ToArray(), newFaces.ToArray(), newKinds.ToArray());
    }

    // Helper function to calculate face normal
    private static Vector3 CalculateFaceNormal(List<Vector3> vertices, int[] face)
    {
        if (face.Length < 3) return Vector3.up;

        // Calculate centroid
        Vector3 centroid = Vector3.zero;
        foreach (int idx in face)
            centroid += vertices[idx];
        centroid /= face.Length;

        // Calculate normal using Newell's method (more robust than just using 3 vertices)
        Vector3 normal = Vector3.zero;
        for (int i = 0; i < face.Length; i++)
        {
            Vector3 current = vertices[face[i]];
            Vector3 next = vertices[face[(i + 1) % face.Length]];
            normal.x += (current.y - next.y) * (current.z + next.z);
            normal.y += (current.z - next.z) * (current.x + next.x);
            normal.z += (current.x - next.x) * (current.y + next.y);
        }

        return normal.normalized;
    }


    /* ---------------------- FINAL FLAT SHADE ------------------------ */
    public static Mesh ApplyFlatShade(
        (Vector3[], int[][], FaceKind[]) input,
        PolyhedronPalette palette = null,
        bool useCongruenceColoring = true,
        float colorSensitivity = 0.001f)
    {
        var (baseVertices, faces, kinds) = input;

        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Color> colors = new List<Color>();

        // Group faces by congruence if using congrruence coloring
        Dictionary<string, int> congruenceToColor = new Dictionary<string, int>();
        int currentColorIndex = 0;

        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            Color faceColor;

            if (palette != null)
            {
                if (useCongruenceColoring)
                {
                    string signature = CalculateFaceSignature(baseVertices, face, colorSensitivity);
                    if (!congruenceToColor.TryGetValue(signature, out int colorIndex))
                    {
                        colorIndex = currentColorIndex++;
                        congruenceToColor[signature] = colorIndex;
                    }
                    faceColor = palette.GetColor(colorIndex, kinds[f]);
                }
                else
                {
                    faceColor = palette.GetColor(f, kinds[f]);
                }
            }
            else
            {
                faceColor = Color.white;
            }

            // Get face vertices
            Vector3[] faceVerts = face.Select(idx => baseVertices[idx]).ToArray();
            
            // Calculate face normal using Newell's method
            Vector3 normal = CalculateFaceNormal(faceVerts.ToList(), Enumerable.Range(0, faceVerts.Length).ToArray());

            // Triangulate the planar face
            TriangulatePlanarFace(
                faceVerts,
                normal,
                vertices,
                tris,
                normals,
                colors,
                faceColor
            );
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        return mesh;
    }

    private static void TriangulatePlanarFace(
        Vector3[] faceVertices, 
        Vector3 faceNormal,
        List<Vector3> vertices,
        List<int> tris,
        List<Vector3> normals,
        List<Color> colors,
        Color faceColor)
    {
        // Calculate face center
        Vector3 center = Vector3.zero;
        foreach (var v in faceVertices)
            center += v;
        center /= faceVertices.Length;

        // Add center vertex first
        int centerIndex = vertices.Count;
        vertices.Add(center);
        normals.Add(faceNormal);
        colors.Add(faceColor);

        // Add perimeter vertices
        int baseIndex = vertices.Count;
        foreach (var v in faceVertices)
        {
            vertices.Add(v);
            normals.Add(faceNormal);
            colors.Add(faceColor);
        }

        // Create triangles fan from center
        for (int i = 0; i < faceVertices.Length; i++)
        {
            tris.Add(centerIndex); // center vertex
            tris.Add(baseIndex + i);
            tris.Add(baseIndex + ((i + 1) % faceVertices.Length));
        }
    }

    // Add helper method for face signatures (congruence)
    private static string CalculateFaceSignature(Vector3[] vertices, int[] face, float sensitivity)
    {
        var angles = new List<float>();
        for (int i = 0; i < face.Length; i++)
        {
            Vector3 v1 = vertices[face[i]];
            Vector3 v2 = vertices[face[(i + 1) % face.Length]];
            Vector3 v3 = vertices[face[(i + 2) % face.Length]];

            Vector3 edge1 = v2 - v1;
            Vector3 edge2 = v3 - v2;
            float angle = Vector3.Angle(edge1, edge2);
            angles.Add(angle);
        }

        angles.Sort();
        return string.Join(",", angles.ConvertAll(a => Mathf.Round(a / sensitivity) * sensitivity));
    }

    private Color[] AssignColors(Vector3[] vertices, int[][] faces, FaceKind[] kinds)
    {
        Color[] colors = new Color[faces.Length];
        
        if (!useCongruenceColoring)
        {
            // Simple face type coloring (default behavior)
            for (int f = 0; f < faces.Length; f++)
            {
                colors[f] = palette.GetColor(0, kinds[f]); // Use single palette element
            }
        }
        else
        {
            // Congruence-based coloring (/cc behavior)
            Dictionary<string, int> congruenceToColor = new Dictionary<string, int>();
            int currentColorIndex = 0;
            
            for (int f = 0; f < faces.Length; f++)
            {
                string signature = CalculateFaceSignature(vertices, faces[f], colorSensitivity);
                if (!congruenceToColor.TryGetValue(signature, out int colorIndex))
                {
                    colorIndex = currentColorIndex++;
                    congruenceToColor[signature] = colorIndex;
                }
                colors[f] = palette.GetColor(colorIndex % palette.ColorSetCount, kinds[f]);
            }
        }
        
        return colors;
    }
}

