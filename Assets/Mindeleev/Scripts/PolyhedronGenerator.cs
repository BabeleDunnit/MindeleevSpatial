using UnityEngine;
using System;
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
                'a' => ApplyAmbo(current),  // Add this line
                _ => current
            };
        }

        return ApplyFlatShade(current, palette, useCongruenceColoring, colorSensitivity);
    }

    /* ---------------------- OPERATORS -------------------------------- */
    public static (Vector3[], int[][], FaceKind[]) ApplyKis((Vector3[], int[][], FaceKind[]) input, float height)
    {
        var (vertices, faces, kinds) = input;
        var newVertices = new List<Vector3>(vertices);
        var newFaces = new List<int[]>();
        var newKinds = new List<FaceKind>();

        // Process each face
        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            FaceKind currentKind = kinds[f];
            
            // Calculate face center and normal
            Vector3 center = Vector3.zero;
            foreach (int idx in face)
                center += vertices[idx];
            center /= face.Length;
            
            Vector3 normal = CalculateFaceNormal(vertices.ToList(), face);
            
            // Add new vertex at face center
            int centerIdx = newVertices.Count;
            newVertices.Add(center + normal * height);
            
            // Original face remains with its original kind
            newFaces.Add(face);
            newKinds.Add(currentKind);
            
            // Create new triangular faces from original edges to new center
            for (int i = 0; i < face.Length; i++)
            {
                int next = (i + 1) % face.Length;
                int[] newFace = new int[] { face[i], face[next], centerIdx };
                newFaces.Add(newFace);
                newKinds.Add(FaceKind.Face); // New pyramidal faces are Face kind
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

        // Add counters for face types
        int faceCount = 0;
        int vertexCount = 0;
        int edgeCount = 0;

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

        // Step 2: Create truncated faces
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
        faceCount = newFaces.Count;
        Debug.Log($"Created {faceCount} truncated original faces");

        // Step 3: Create vertex figures
        for (int v = 0; v < vertices.Length; v++)
        {
            if (vertexToFaces.TryGetValue(v, out var faceIndices))
            {
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
        }
        vertexCount = newFaces.Count - faceCount;
        Debug.Log($"Created {vertexCount} vertex faces");

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

                // Skip if we've already processed this edge
                if (processedEdges.Contains(edge)) continue;
                processedEdges.Add(edge);

                // Get truncated points for the current edge
                int p1 = GetTruncatedPoint(edge.Item1, edge.Item2); // start point from first vertex
                int p2 = GetTruncatedPoint(edge.Item2, edge.Item1); // start point from second vertex

                // Get the faces sharing this edge
                if (!edgeToFaces.TryGetValue(edge, out var edgeFaces) || edgeFaces.Count != 2)
                    continue; // Skip non-manifold edges or boundary edges

                // Create quad by finding truncated points from both adjacent faces
                List<int> quadPoints = new List<int>();

                // First face
                var firstFace = faces[edgeFaces[0]];
                int idx1 = System.Array.IndexOf(firstFace, edge.Item1);
                int idx2 = System.Array.IndexOf(firstFace, edge.Item2);

                // Second face
                var secondFace = faces[edgeFaces[1]];
                int idx3 = System.Array.IndexOf(secondFace, edge.Item1);
                int idx4 = System.Array.IndexOf(secondFace, edge.Item2);

                // Add points in correct order to ensure proper orientation
                // Find the next vertex after v1 in both faces
                int nextInFirst = firstFace[(idx1 + 1) % firstFace.Length];
                int nextInSecond = secondFace[(idx3 + 1) % secondFace.Length];

                // We want the points that are NOT on the edge itself
                if (nextInFirst == edge.Item2)
                    nextInFirst = firstFace[(idx1 + firstFace.Length - 1) % firstFace.Length];
                if (nextInSecond == edge.Item2)
                    nextInSecond = secondFace[(idx3 + secondFace.Length - 1) % secondFace.Length];

                // Create quad points in correct order
                quadPoints.Add(p1); // First truncated point from v1
                quadPoints.Add(GetTruncatedPoint(edge.Item1, nextInFirst)); // Adjacent point from first face
                quadPoints.Add(p2); // Second truncated point from v2
                quadPoints.Add(GetTruncatedPoint(edge.Item1, nextInSecond)); // Adjacent point from second face

                // Ensure correct orientation
                var faceCenter = Vector3.zero;
                foreach (int idx in quadPoints)
                    faceCenter += newVertices[idx];
                faceCenter /= 4;

                var outward = (faceCenter - vertices[edge.Item1]).normalized;
                var normal = Vector3.Cross(
                    newVertices[quadPoints[1]] - newVertices[quadPoints[0]],
                    newVertices[quadPoints[2]] - newVertices[quadPoints[0]]
                ).normalized;

                if (Vector3.Dot(normal, outward) < 0)
                {
                    // Flip face orientation by swapping two vertices
                    var temp = quadPoints[1];
                    quadPoints[1] = quadPoints[3];
                    quadPoints[3] = temp;
                }

                newFaces.Add(quadPoints.ToArray());
                newKinds.Add(FaceKind.Edge);
            }
        }
        edgeCount = newFaces.Count - (faceCount + vertexCount);
        Debug.Log($"Created {edgeCount} edge faces");

        return (newVertices.ToArray(), newFaces.ToArray(), newKinds.ToArray());
    }

    public static (Vector3[], int[][], FaceKind[]) ApplyAmbo((Vector3[], int[][], FaceKind[]) input)
    {
        var (vertices, faces, kinds) = input;
        var newVertices = new List<Vector3>();
        var newFaces = new List<int[]>();
        var newKinds = new List<FaceKind>();

        Dictionary<(int, int), int> edgeToMidpoint = new Dictionary<(int, int), int>();
        Dictionary<int, FaceKind> faceToKind = new Dictionary<int, FaceKind>();

        // Track original face kinds
        for (int f = 0; f < faces.Length; f++)
        {
            faceToKind[f] = kinds[f];
        }

        // Calculate polyhedron center
        Vector3 polyhedronCenter = Vector3.zero;
        foreach (var v in vertices)
            polyhedronCenter += v;
        polyhedronCenter /= vertices.Length;

        int GetMidpoint(int v1, int v2)
        {
            var edge = v1 < v2 ? (v1, v2) : (v2, v1);
            if (!edgeToMidpoint.TryGetValue(edge, out int idx))
            {
                Vector3 midpoint = (vertices[v1] + vertices[v2]) * 0.5f;
                idx = newVertices.Count;
                newVertices.Add(midpoint);
                edgeToMidpoint[edge] = idx;
            }
            return idx;
        }

        // Step 1: Transform original faces
        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            int n = face.Length;
            var newFaceIndices = new int[n];

            // Get face normal and center
            Vector3 faceCenter = Vector3.zero;
            foreach (int idx in face)
                faceCenter += vertices[idx];
            faceCenter /= n;

            Vector3 faceNormal = CalculateFaceNormal(vertices.ToList(), face);
            Vector3 outwardDir = (faceCenter - polyhedronCenter).normalized;

            if (Vector3.Dot(faceNormal, outwardDir) < 0)
                faceNormal = -faceNormal;

            // Create new face
            for (int i = 0; i < n; i++)
            {
                int v1 = face[i];
                int v2 = face[(i + 1) % n];
                newFaceIndices[i] = GetMidpoint(v1, v2);
            }

            // Calculate new face center
            Vector3 newCenter = Vector3.zero;
            foreach (int idx in newFaceIndices)
                newCenter += newVertices[idx];
            newCenter /= newFaceIndices.Length;

            // Fix winding
            Vector3 newNormal = CalculateFaceNormal(newVertices, newFaceIndices);
            Vector3 newOutwardDir = (newCenter - polyhedronCenter).normalized;

            if (Vector3.Dot(newNormal, newOutwardDir) < 0)
            {
                System.Array.Reverse(newFaceIndices);
            }

            newFaces.Add(newFaceIndices);
            
            // Transform face kind based on original kind
            switch (kinds[f])
            {
                case FaceKind.Face:
                    newKinds.Add(FaceKind.Edge); // Face becomes Edge
                    break;
                case FaceKind.Edge:
                    newKinds.Add(FaceKind.Vertex); // Edge becomes Vertex
                    break;
                case FaceKind.Vertex:
                    newKinds.Add(FaceKind.Face); // Vertex becomes Face
                    break;
            }
        }

        // Step 2: Create vertex faces
        for (int v = 0; v < vertices.Length; v++)
        {
            var vertexMidpoints = new HashSet<int>();
            var vertexFace = new List<int>();
            Vector3 vertexPos = vertices[v];

            // Collect connected midpoints
            for (int f = 0; f < faces.Length; f++)
            {
                int[] face = faces[f];
                for (int i = 0; i < face.Length; i++)
                {
                    if (face[i] == v)
                    {
                        int next = face[(i + 1) % face.Length];
                        int prev = face[(i - 1 + face.Length) % face.Length];
                        vertexMidpoints.Add(GetMidpoint(v, next));
                        vertexMidpoints.Add(GetMidpoint(v, prev));
                    }
                }
            }

            if (vertexMidpoints.Count > 0)
            {
                var remainingMidpoints = new HashSet<int>(vertexMidpoints);
                int startPoint = remainingMidpoints.First();
                vertexFace.Add(startPoint);
                remainingMidpoints.Remove(startPoint);

                // Create ordered sequence
                while (remainingMidpoints.Count > 0)
                {
                    int currentPoint = vertexFace[vertexFace.Count - 1];
                    Vector3 currentPos = newVertices[currentPoint];
                    float minDist = float.MaxValue;
                    int nextPoint = -1;

                    foreach (int midpoint in remainingMidpoints)
                    {
                        float dist = Vector3.Distance(currentPos, newVertices[midpoint]);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            nextPoint = midpoint;
                        }
                    }

                    if (nextPoint != -1)
                    {
                        vertexFace.Add(nextPoint);
                        remainingMidpoints.Remove(nextPoint);
                    }
                    else break;
                }

                if (vertexFace.Count >= 3)
                {
                    // Fix: Ensure vertex face normal points outward
                    Vector3 faceCenter = Vector3.zero;
                    foreach (int idx in vertexFace)
                        faceCenter += newVertices[idx];
                    faceCenter /= vertexFace.Count;

                    Vector3 outwardDir = (faceCenter - vertexPos).normalized;
                    Vector3 normal = CalculateFaceNormal(newVertices, vertexFace.ToArray());

                    if (Vector3.Dot(normal, outwardDir) > 0)
                        vertexFace.Reverse();

                    newFaces.Add(vertexFace.ToArray());
                    newKinds.Add(FaceKind.Face); // New vertex faces start as Face type
                }
            }
        }

        return (newVertices.ToArray(), newFaces.ToArray(), newKinds.ToArray());
    }

    // Helper function to calculate face normal
    private static Vector3 CalculateFaceNormal(IList<Vector3> vertices, int[] face)
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

