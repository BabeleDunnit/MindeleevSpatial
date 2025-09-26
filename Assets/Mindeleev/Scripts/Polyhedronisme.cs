using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public static class Polyhedronisme
{
    /* ------------------------------------------------------------------
     *  STATIC DATA: 5 canonical polyhedra expressed as (vertices, faces)
     * ----------------------------------------------------------------*/
    // Cube (C)
    public static readonly (Vector3[], int[][], int[]) Cube = (
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
        Enumerable.Repeat(0, 6).ToArray() // All faces start with color index 0
    );

    // Tetrahedron (T)
    public static readonly (Vector3[], int[][], int[]) Tetrahedron = (
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
        Enumerable.Repeat(0, 4).ToArray() // All faces start with color index 0
    );

    // Octahedron (O)
    public static readonly (Vector3[], int[][], int[]) Octahedron = (
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
        Enumerable.Repeat(0, 8).ToArray() // All faces start with color index 0
    );

    // Dodecahedron (D)
    public static readonly (Vector3[], int[][], int[]) Dodecahedron = (
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
        Enumerable.Repeat(0, 12).ToArray() // All faces start with color index 0
    );

    // Icosahedron (I)
    public static readonly (Vector3[], int[][], int[]) Icosahedron = (
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
        Enumerable.Repeat(0, 20).ToArray() // All faces start with color index 0
    );

    /* ---------------------- PARSE RECIPE ----------------------------- */
    public static (Vector3[], int[][], int[]) ParsePolyhedronRecipe_obsolete(string recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe))
            return Cube;

        // Find rightmost uppercase letter (base polyhedron)
        int basePos = recipe.Length - 1;
        while (basePos >= 0 && !char.IsUpper(recipe[basePos]))
            basePos--;

        if (basePos < 0)
        {
            Debug.LogError($"Invalid recipe '{recipe}': no base polyhedron found");
            return Cube;
        }

        // Get base polyhedron
        var current = recipe[basePos] switch
        {
            'C' => Cube,
            'T' => Tetrahedron,
            'O' => Octahedron,
            'D' => Dodecahedron,
            'I' => Icosahedron,
            _ => Cube
        };

        // Parse operators and parameters from right to left
        var tokens = new List<(char op, int facesSidesFilter, int faceSignatureRounding, float? param0, float? param1)>();
        int i = 0;
        while (i < basePos)
        {
            char c = recipe[i];
            if (char.IsLower(c))
            {
                int facesSidesFilter = 0;
                int faceSignatureRounding = 1;
                float? param0 = null;
                float? param1 = null;

                // Check if next char starts a parameter list
                if (i + 1 < basePos && recipe[i + 1] == '(')
                {
                    // Find closing parenthesis
                    int closePos = recipe.IndexOf(')', i + 2);
                    if (closePos == -1)
                    {
                        Debug.LogError($"Invalid recipe '{recipe}': unclosed parameter list");
                        return current;
                    }

                    // Parse parameters
                    string paramStr = recipe.Substring(i + 2, closePos - (i + 2));
                    string[] parameters = paramStr.Split(',').Select(p => p.Trim()).ToArray();

                    // Parse parameter 1 (facesSidesFilter)
                    if (parameters.Length > 0 && int.TryParse(parameters[0], out int facesSidesFilterParam))
                        facesSidesFilter = facesSidesFilterParam;

                    // Parse parameter 2 (faceSignatureRounding)
                    if (parameters.Length > 1 && int.TryParse(parameters[1], out int faceSignatureRoundingParam))
                        faceSignatureRounding = faceSignatureRoundingParam;

                    // Parse parameter 3 (float0)
                    if (parameters.Length > 2 && float.TryParse(parameters[2],
                        NumberStyles.Float, CultureInfo.InvariantCulture, out float factorParam0))
                        param0 = factorParam0;

                    // Parse parameter 4 (float1)
                    if (parameters.Length > 3 && float.TryParse(parameters[3],
                        NumberStyles.Float, CultureInfo.InvariantCulture, out float factorParam1))
                        param1 = factorParam1;

                    i = closePos + 1;
                }
                else
                {
                    // Look for simple numeric parameter
                    int j = i + 1;
                    while (j < basePos && char.IsDigit(recipe[j])) j++;
                    if (j > i + 1)
                    {
                        string numStr = recipe.Substring(i + 1, j - (i + 1));
                        if (int.TryParse(numStr, out int simpleParam))
                            facesSidesFilter = simpleParam;
                    }
                    i = j;
                }

                tokens.Add((c, facesSidesFilter, faceSignatureRounding, param0, param1));
            }
            else
            {
                i++;
            }
        }

        // Apply operators in reverse order
        const int MAX_VERTICES = 300; // Add this constant at class level
        for (int t = tokens.Count - 1; t >= 0; t--)
        {
            // Check vertex count before applying next operator
            if (current.Item1.Length > MAX_VERTICES)
            {
                Debug.LogWarning($"Recipe '{recipe}' exceeded {MAX_VERTICES} vertices limit after {tokens.Count - t - 1} operators. " +
                                $"Skipping remaining {t + 1} operators.");
                break;
            }

            var (op, facesSidesFilter, faceSignatureRounding, param0, param1) = tokens[t];
            // Substitute truncate operator with its equivalent sequence
            if (op == 't')
            {
                // Apply d->k->d sequence for truncation
                current = ApplyDual(current, faceSignatureRounding);
                current = ApplyKis(current, facesSidesFilter, faceSignatureRounding, param0 ?? 0.1f);
                current = ApplyDual(current, faceSignatureRounding);
            }
            else
            {
                current = op switch
                {
                    'k' => ApplyKis(current, facesSidesFilter, faceSignatureRounding, param0 ?? 0.1f),
                    'a' => ApplyAmbo(current, faceSignatureRounding),
                    'd' => ApplyDual(current, faceSignatureRounding),
                    'f' => ApplyFuckedStellation(current, faceSignatureRounding),
                    'n' => ApplyInsetN(current, facesSidesFilter, faceSignatureRounding, param0 ?? 0.6f, param1 ?? -0.3f),
                    'l' => ApplyStellation(current, faceSignatureRounding),
                    _ => current,

                };
            }
        }

        return current;
    }

    /* ---------------------- OPERATORS -------------------------------- */
    public static (Vector3[], int[][], int[]) ApplyKis(
        (Vector3[], int[][], int[]) input,
        int targetFacesFilter,
        int faceSignatureRounding,
        float height)
    {
        var (vertices, faces, colorIndices) = input;
        var newVertices = new List<Vector3>(vertices);
        var newFaces = new List<int[]>();
        var newColorIndices = new List<int>();

        var signatureToColor = new Dictionary<string, int>();
        int nextColorIndex = 0;

        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            Vector3[] faceVerts = face.Select(idx => vertices[idx]).ToArray();
            string signature = GetFaceSignature(faceVerts, faceSignatureRounding);

            if (targetFacesFilter != 0 && face.Length != targetFacesFilter)
            {
                newFaces.Add(face);
                if (!signatureToColor.ContainsKey(signature))
                    signatureToColor[signature] = nextColorIndex++;
                newColorIndices.Add(signatureToColor[signature]);
                continue;
            }

            Vector3 center = Vector3.zero;
            foreach (int idx in face)
                center += vertices[idx];
            center /= face.Length;

            Vector3 normal = CalculateFaceNormal(vertices, face);
            Vector3 apex = center + normal * height;
            int apexIndex = newVertices.Count;
            newVertices.Add(apex);

            for (int i = 0; i < face.Length; i++)
            {
                int v1 = face[i];
                int v2 = face[(i + 1) % face.Length];
                int[] newFace = new[] { v1, v2, apexIndex };
                Vector3[] newFaceVerts = newFace.Select(idx => newVertices[idx]).ToArray();
                string newSignature = GetFaceSignature(newFaceVerts, faceSignatureRounding);

                if (!signatureToColor.ContainsKey(newSignature))
                    signatureToColor[newSignature] = nextColorIndex++;

                newFaces.Add(newFace);
                newColorIndices.Add(signatureToColor[newSignature]);
            }
        }

        return NormalizePolyhedron((newVertices.ToArray(), newFaces.ToArray(), newColorIndices.ToArray()));
    }

    public static (Vector3[], int[][], int[]) ApplyAmbo(
        (Vector3[], int[][], int[]) input,
        int faceSignatureRounding)
    {
        var (vertices, faces, colorIndices) = input;
        var newVertices = new List<Vector3>();
        var newFaces = new List<int[]>();
        var newColorIndices = new List<int>();
        var edgeToMidpoint = new Dictionary<(int, int), int>();

        // Step 1: Create edge midpoints consistently
        int GetMidpoint(int v1, int v2)
        {
            var edge = v1 < v2 ? (v1, v2) : (v2, v1); // Ensure consistent edge ordering
            if (!edgeToMidpoint.TryGetValue(edge, out int idx))
            {
                Vector3 midpoint = (vertices[v1] + vertices[v2]) * 0.5f;
                idx = newVertices.Count;
                newVertices.Add(midpoint);
                edgeToMidpoint[edge] = idx;
            }
            return idx;
        }

        // Step 2: Build edge connectivity information
        var edgeConnections = new Dictionary<int, List<(int vertex, int face)>>();

        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            for (int i = 0; i < face.Length; i++)
            {
                int v1 = face[i];
                int v2 = face[(i + 1) % face.Length];
                int midpoint = GetMidpoint(v1, v2);

                if (!edgeConnections.ContainsKey(midpoint))
                    edgeConnections[midpoint] = new List<(int, int)>();

                edgeConnections[midpoint].Add((v1, f));
                edgeConnections[midpoint].Add((v2, f));
            }
        }

        // Step 3: Create faces systematically
        // A) Original face centers become new vertices
        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            var newFaceIndices = new int[face.Length];

            for (int i = 0; i < face.Length; i++)
            {
                int v1 = face[i];
                int v2 = face[(i + 1) % face.Length];
                newFaceIndices[i] = GetMidpoint(v1, v2);
            }

            newFaces.Add(newFaceIndices);
            newColorIndices.Add(colorIndices[f]);
        }

        Vector3 polyCenter = Vector3.zero;
        foreach (var vtx in vertices) polyCenter += vtx;
        polyCenter /= vertices.Length;

        // B) Original vertices become new faces
        for (int v = 0; v < vertices.Length; v++)
        {
            // Find all faces containing this vertex
            var incidentFaces = new List<int>();
            for (int f = 0; f < faces.Length; f++)
                if (faces[f].Contains(v)) incidentFaces.Add(f);

            if (incidentFaces.Count < 3) continue; // skip isolated or boundary vertices

            // For each incident face, find the two midpoints on edges incident to v
            var orderedMidpoints = new List<int>();
            int startFace = incidentFaces[0];
            int currentFace = startFace;
            int prevVertex = -1;
            var usedFaces = new HashSet<int>();

            // Start with any face containing v, walk around v
            do
            {
                int[] face = faces[currentFace];
                int idx = Array.IndexOf(face, v);
                int prev = face[(idx - 1 + face.Length) % face.Length];
                int next = face[(idx + 1) % face.Length];

                int midPrev = GetMidpoint(v, prev);
                int midNext = GetMidpoint(v, next);

                // Add midPrev if not already added
                if (!orderedMidpoints.Contains(midPrev))
                    orderedMidpoints.Add(midPrev);

                // Find the next face sharing (v, next) that hasn't been used
                usedFaces.Add(currentFace);
                int nextFace = -1;
                foreach (int f in incidentFaces)
                {
                    if (usedFaces.Contains(f)) continue;
                    var fVerts = faces[f];
                    if (fVerts.Contains(v) && fVerts.Contains(next))
                    {
                        nextFace = f;
                        break;
                    }
                }
                prevVertex = next;
                currentFace = nextFace;
            }
            while (currentFace != -1 && currentFace != startFace && orderedMidpoints.Count < incidentFaces.Count);

            // If not all midpoints found, fall back to distinct midpoints
            if (orderedMidpoints.Count < incidentFaces.Count)
            {
                orderedMidpoints = incidentFaces
                    .SelectMany(f =>
                    {
                        int[] face = faces[f];
                        int idx = Array.IndexOf(face, v);
                        int prev = face[(idx - 1 + face.Length) % face.Length];
                        int next = face[(idx + 1) % face.Length];
                        return new[] { GetMidpoint(v, prev), GetMidpoint(v, next) };
                    })
                    .Distinct()
                    .ToList();
            }

            // Ensure correct winding
            var faceIndices = orderedMidpoints.ToArray();
            Vector3 center = Vector3.zero;
            foreach (var idx in faceIndices) center += newVertices[idx];
            center /= faceIndices.Length;
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < faceIndices.Length; i++)
            {
                Vector3 cur = newVertices[faceIndices[i]];
                Vector3 nxt = newVertices[faceIndices[(i + 1) % faceIndices.Length]];
                normal += Vector3.Cross(cur - center, nxt - center);
            }
            normal.Normalize();
            Vector3 outward = (center - polyCenter).normalized;
            if (Vector3.Dot(normal, outward) < 0)
                faceIndices = faceIndices.Reverse().ToArray();

            newFaces.Add(faceIndices);
            newColorIndices.Add(colorIndices.Max() + 1);
        }

        var result = (newVertices.ToArray(), newFaces.ToArray(), newColorIndices.ToArray());
        return NormalizePolyhedron(result);
    }

    public static (Vector3[], int[][], int[]) ApplyDual(
        (Vector3[], int[][], int[]) input,
        int faceSignatureRounding)
    {
        var (vertices, faces, colorIndices) = input;

        // Create dual vertices array (one per original face)
        var dualVertices = new Vector3[faces.Length];

        // Calculate face centers - these become the dual vertices
        for (int i = 0; i < faces.Length; i++)
        {
            Vector3 center = Vector3.zero;
            foreach (int idx in faces[i])
                center += vertices[idx];
            dualVertices[i] = center / faces[i].Length;
        }

        // Setup for congruence coloring
        var signatureToColor = new Dictionary<string, int>();
        int nextColorIndex = 0;
        // int rounding = Mathf.Clamp((int)Mathf.Abs(Mathf.Log10(colorSensitivity)), 0, 6);

        // Calculate dual polyhedron center
        Vector3 dualCenter = Vector3.zero;
        foreach (var v in dualVertices)
            dualCenter += v;
        dualCenter /= dualVertices.Length;

        // Create faces from original vertices
        var dualFaces = new List<int[]>();
        var dualColors = new List<int>();

        // For each original vertex, create a face from incident face centers
        for (int v = 0; v < vertices.Length; v++)
        {
            // Find all faces containing this vertex
            var incidentFaces = new List<int>();
            for (int f = 0; f < faces.Length; f++)
                if (faces[f].Contains(v))
                    incidentFaces.Add(f);

            if (incidentFaces.Count < 3) continue;

            // Order faces around the vertex
            var orderedFaces = new List<int>();
            var used = new HashSet<int>();
            int currentFace = incidentFaces[0];

            do
            {
                orderedFaces.Add(currentFace);
                used.Add(currentFace);

                // Find next face sharing an edge with current face
                int[] face = faces[currentFace];
                int idx = System.Array.IndexOf(face, v);
                int nextVert = face[(idx + 1) % face.Length];

                currentFace = -1;
                foreach (int f in incidentFaces)
                {
                    if (!used.Contains(f) && faces[f].Contains(nextVert))
                    {
                        currentFace = f;
                        break;
                    }
                }
            }
            while (currentFace != -1 && orderedFaces.Count < incidentFaces.Count);

            // Check winding
            Vector3[] faceVerts = orderedFaces.Select(f => dualVertices[f]).ToArray();
            Vector3 faceCenter = Vector3.zero;
            foreach (var vert in faceVerts)
                faceCenter += vert;
            faceCenter /= faceVerts.Length;

            Vector3 normal = CalculateFaceNormal(faceVerts.ToList(), Enumerable.Range(0, faceVerts.Length).ToArray());
            Vector3 outward = (faceCenter - dualCenter).normalized;

            if (Vector3.Dot(normal, outward) < 0)
                orderedFaces.Reverse();

            // Calculate color based on face signature
            string signature = GetFaceSignature(faceVerts, faceSignatureRounding);
            if (!signatureToColor.ContainsKey(signature))
                signatureToColor[signature] = nextColorIndex++;

            dualFaces.Add(orderedFaces.ToArray());
            dualColors.Add(signatureToColor[signature]);
        }

        var result = (dualVertices, dualFaces.ToArray(), dualColors.ToArray());
        return NormalizePolyhedron(result);
    }

    // Helper: returns true if the face is CCW as seen from outside the polyhedron
    private static bool IsFaceWindingOutward(Vector3[] allVertices, int[] face, Vector3 polyCenter)
    {
        // Calculate face center
        Vector3 faceCenter = Vector3.zero;
        foreach (var idx in face)
            faceCenter += allVertices[idx];
        faceCenter /= face.Length;

        // Calculate face normal (using Newell's method)
        Vector3 normal = Vector3.zero;
        for (int i = 0; i < face.Length; i++)
        {
            Vector3 current = allVertices[face[i]];
            Vector3 next = allVertices[face[(i + 1) % face.Length]];
            normal.x += (current.y - next.y) * (current.z + next.z);
            normal.y += (current.z - next.z) * (current.x + next.x);
            normal.z += (current.x - next.x) * (current.y + next.y);
        }
        normal.Normalize();

        // Vector from face center to polyhedron center
        Vector3 toCenter = (polyCenter - faceCenter).normalized;

        // If the normal points away from the center, it's outward
        return Vector3.Dot(normal, toCenter) < 0;
    }

    public static (Vector3[], int[][], int[]) ApplyStellation(
    (Vector3[], int[][], int[]) input,
    int faceSignatureRounding)
    {
        var (inputVertices, faces, colorIndices) = input;
        var newVertices = new List<Vector3>(inputVertices);
        var newFaces = new List<int[]>();
        var newColorIndices = new List<int>();
        var signatureToColor = new Dictionary<string, int>();
        int nextColorIndex = 0;


        // Compute polyhedron centroid for winding checks
        Vector3 polyCenter = Vector3.zero;
        foreach (var v in inputVertices) polyCenter += v;
        polyCenter /= inputVertices.Length;

        // For each edge, store the two "inner" vertices (from each adjacent face) and which face they belong to
        var edgeToInnerVertices = new Dictionary<(int, int), List<(int innerIdx, int faceIdx, int localEdgeIdx)>>();

        // For each face, store the indices of its inner (central) vertices
        var faceInnerVertices = new List<List<int>>();

        // Build inner vertices and mapping
        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            // Debug.Log($"face vertices: {string.Join(",", face)}");
            var faceCenter = CalculateFaceCenter(face.Select(idx => inputVertices[idx]).ToArray());
            var innerVerts = new List<int>();

            for (int i = 0; i < face.Length; i++)
            {
                int v1 = face[i];
                int v2 = face[(i + 1) % face.Length];
                Vector3 edgeMid = (inputVertices[v1] + inputVertices[v2]) * 0.5f;
                Vector3 moved = Vector3.Lerp(edgeMid, faceCenter, 0.5f);
                int idx = newVertices.Count;
                newVertices.Add(moved);
                innerVerts.Add(idx);

                // Store for edge, always with (min, max) order
                var edgeKey = v1 < v2 ? (v1, v2) : (v2, v1);
                if (!edgeToInnerVertices.ContainsKey(edgeKey))
                    edgeToInnerVertices[edgeKey] = new List<(int, int, int)>();
                edgeToInnerVertices[edgeKey].Add((idx, f, i));
            }
            faceInnerVertices.Add(innerVerts);

            // Central face (keep winding as original)
            var centralFace = innerVerts.ToArray();
            if (!IsFaceWindingOutward(newVertices.ToArray(), centralFace, polyCenter))
                System.Array.Reverse(centralFace);
            newFaces.Add(centralFace);

            // Debug.Log($"cenral face vertices: {string.Join(",", centralFace)}");

            string signature = GetFaceSignature(centralFace.Select(idx => newVertices[idx]).ToArray(), faceSignatureRounding);
            // Debug.Log(signature);
            if (!signatureToColor.ContainsKey(signature))
                signatureToColor[signature] = nextColorIndex++;

            newColorIndices.Add(signatureToColor[signature]);
            //         newColorIndices.Add(0);
            //newColorIndices.Add(colorIndices[f]);

            // Star triangles: original vertex, its inner, previous inner (CCW)
            for (int i = 0; i < face.Count(); i++)
            {
                int orig = face[i];
                int inner1 = innerVerts[i];
                int inner2 = innerVerts[(i - 1 + face.Count()) % face.Count()];
                var tri = new[] { orig, inner1, inner2 };
                if (!IsFaceWindingOutward(newVertices.ToArray(), tri, polyCenter))
                    System.Array.Reverse(tri);
                newFaces.Add(tri);
                // newColorIndices.Add((colorIndices[f] + 1) % colorIndices.Length);

                signature = GetFaceSignature(tri.Select(idx => newVertices[idx]).ToArray(), faceSignatureRounding);
                if (!signatureToColor.ContainsKey(signature))
                    signatureToColor[signature] = nextColorIndex++;

                newColorIndices.Add(signatureToColor[signature]);


            }
        }

        // Edge triangles: for each edge, two triangles to fill the quad
        foreach (var kvp in edgeToInnerVertices)
        {
            var edge = kvp.Key;
            var inners = kvp.Value;
            if (inners.Count == 2)
            {
                int v1 = edge.Item1;
                int v2 = edge.Item2;
                var (iA, fA, localA) = inners[0];
                var (iB, fB, localB) = inners[1];

                var tri1 = new[] { v1, iA, iB };
                if (!IsFaceWindingOutward(newVertices.ToArray(), tri1, polyCenter))
                    System.Array.Reverse(tri1);
                newFaces.Add(tri1);
                // newColorIndices.Add((colorIndices[fA] + 1) % colorIndices.Length);

                string signature = GetFaceSignature(tri1.Select(idx => newVertices[idx]).ToArray(), faceSignatureRounding);
                if (!signatureToColor.ContainsKey(signature))
                    signatureToColor[signature] = nextColorIndex++;

                newColorIndices.Add(signatureToColor[signature]);


                var tri2 = new[] { v2, iB, iA };
                if (!IsFaceWindingOutward(newVertices.ToArray(), tri2, polyCenter))
                    System.Array.Reverse(tri2);
                newFaces.Add(tri2);
                // newColorIndices.Add((colorIndices[fB] + 1) % colorIndices.Length);

                signature = GetFaceSignature(tri2.Select(idx => newVertices[idx]).ToArray(), faceSignatureRounding);
                if (!signatureToColor.ContainsKey(signature))
                    signatureToColor[signature] = nextColorIndex++;

                newColorIndices.Add(signatureToColor[signature]);

            }
        }

        return NormalizePolyhedron((newVertices.ToArray(), newFaces.ToArray(), newColorIndices.ToArray()));
    }

    public static (Vector3[], int[][], int[]) ApplyFuckedStellation(
        (Vector3[], int[][], int[]) input,
        int faceSignatureRounding)
    {
        var (vertices, faces, colorIndices) = input;
        var newVertices = new List<Vector3>(vertices);
        var newFaces = new List<int[]>();
        var newColorIndices = new List<int>();

        var signatureToColor = new Dictionary<string, int>();
        int nextColorIndex = 0;

        // First pass: collect all edge-face pairs and their centers
        var edgeToFaceCenters = new Dictionary<string, List<Vector3>>();
        var centers = new Vector3[faces.Length];
        var faceNewVertices = new Dictionary<int, List<int>>();

        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            centers[f] = CalculateFaceCenter(faces[f].Select(idx => vertices[idx]).ToArray());
            faceNewVertices[f] = new List<int>();

            for (int i = 0; i < face.Length; i++)
            {
                int v1 = face[i];
                int v2 = face[(i + 1) % face.Length];
                string edgeKey = v1 < v2 ? $"{v1}-{v2}" : $"{v2}-{v1}";

                if (!edgeToFaceCenters.ContainsKey(edgeKey))
                    edgeToFaceCenters[edgeKey] = new List<Vector3>();
                edgeToFaceCenters[edgeKey].Add(centers[f]);
            }
        }

        // Second pass: create vertices
        var edgeToVertexIndex = new Dictionary<string, int>();

        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];

            for (int i = 0; i < face.Length; i++)
            {
                int v1 = face[i];
                int v2 = face[(i + 1) % face.Length];
                string edgeKey = v1 < v2 ? $"{v1}-{v2}" : $"{v2}-{v1}";

                if (!edgeToVertexIndex.ContainsKey(edgeKey))
                {
                    Vector3 edgeMidpoint = (vertices[v1] + vertices[v2]) * 0.5f;

                    Vector3 averageCenter = Vector3.zero;
                    var faceCenters = edgeToFaceCenters[edgeKey];
                    foreach (var center in faceCenters)
                        averageCenter += center;
                    averageCenter /= faceCenters.Count;

                    Vector3 newVertex = Vector3.Lerp(edgeMidpoint, averageCenter, 0.5f);
                    edgeToVertexIndex[edgeKey] = newVertices.Count;
                    newVertices.Add(newVertex);
                }

                faceNewVertices[f].Add(edgeToVertexIndex[edgeKey]);
            }
        }

        // Third pass: create all faces including central faces
        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            var faceVerts = faceNewVertices[f];

            // Create triangular faces with consistent winding
            for (int i = 0; i < face.Length; i++)
            {
                int v1 = face[i];
                int v2 = face[(i + 1) % face.Length];
                int midVertex = faceVerts[i];
                int nextMidVertex = faceVerts[(i + 1) % face.Length];

                // Create faces with consistent winding order
                AddTriangleWithColor(newFaces, newColorIndices, signatureToColor, ref nextColorIndex,
                    new[] { v1, v2, midVertex }, newVertices, faceSignatureRounding);
                AddTriangleWithColor(newFaces, newColorIndices, signatureToColor, ref nextColorIndex,
                    new[] { midVertex, v2, nextMidVertex }, newVertices, faceSignatureRounding);
            }

            // Create central face using all edge midpoints
            AddPolygonWithColor(newFaces, newColorIndices, signatureToColor, ref nextColorIndex,
                faceVerts.ToArray(), newVertices, faceSignatureRounding);
        }

        return NormalizePolyhedron((newVertices.ToArray(), newFaces.ToArray(), newColorIndices.ToArray()));
    }

    private static void AddPolygonWithColor(
        List<int[]> faces,
        List<int> colorIndices,
        Dictionary<string, int> signatureToColor,
        ref int nextColorIndex,
        int[] polygon,
        List<Vector3> vertices,
        int faceSignatureRounding)
    {
        string sig = GetFaceSignature(polygon.Select(idx => vertices[idx]).ToArray(), faceSignatureRounding);
        if (!signatureToColor.ContainsKey(sig))
            signatureToColor[sig] = nextColorIndex++;
        faces.Add(polygon);
        colorIndices.Add(signatureToColor[sig]);
    }

    private static void AddTriangleWithColor(
        List<int[]> faces,
        List<int> colorIndices,
        Dictionary<string, int> signatureToColor,
        ref int nextColorIndex,
        int[] triangle,
        List<Vector3> vertices,
        int faceSignatureRounding)
    {
        string sig = GetFaceSignature(triangle.Select(idx => vertices[idx]).ToArray(), faceSignatureRounding);
        if (!signatureToColor.ContainsKey(sig))
            signatureToColor[sig] = nextColorIndex++;
        faces.Add(triangle);
        colorIndices.Add(signatureToColor[sig]);
    }

    public static (Vector3[], int[][], int[]) ApplyInsetN(
        (Vector3[], int[][], int[]) input,
        int facesSidesFilter,
        int faceSignatureRounding,
        float insetDistance,
        float extrudeDistance)
    {

        // insetDistance = 0.6f;
        // extrudeDistance = -0.3f;

        var (vertices, faces, colorIndices) = input;
        var newVertices = new List<Vector3>(vertices);
        var newFaces = new List<int[]>();
        var newColorIndices = new List<int>();

        var signatureToColor = new Dictionary<string, int>();
        int nextColorIndex = 0;

        // Debug.Log($"ApplyInsetN with n={facesSidesFilter}, inset={insetDistance}, extrude={extrudeDistance}"); // Debug

        // Precompute centers and normals
        var centers = new Vector3[faces.Length];
        var normals = new Vector3[faces.Length];
        for (int f = 0; f < faces.Length; f++)
        {
            var faceVerts = faces[f].Select(idx => vertices[idx]).ToArray();
            centers[f] = CalculateFaceCenter(faceVerts);
            normals[f] = CalculateFaceNormal(faceVerts.ToList(), Enumerable.Range(0, faceVerts.Length).ToArray());
        }

        var insetVertexMap = new Dictionary<(int, int), int>();

        // Phase 1: Create all inset vertices first
        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            if (facesSidesFilter == 0 || face.Length == facesSidesFilter)
            {
                for (int i = 0; i < face.Length; i++)
                {
                    Vector3 originalPos = vertices[face[i]];
                    Vector3 insetPos = Vector3.Lerp(originalPos, centers[f], insetDistance);
                    int newIdx = newVertices.Count;
                    newVertices.Add(insetPos);
                    insetVertexMap[(f, i)] = newIdx;
                }
            }
        }

        // Phase 2: Apply extrusion to all inset vertices
        if (extrudeDistance != 0)
        {
            foreach (var kvp in insetVertexMap)
            {
                var (faceIdx, _) = kvp.Key;
                int vertexIdx = kvp.Value;
                newVertices[vertexIdx] += normals[faceIdx] * extrudeDistance;
            }
        }

        // Phase 3: Build all faces
        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            int nVerts = face.Length;
            bool isTarget = (facesSidesFilter == 0 || nVerts == facesSidesFilter);

            if (isTarget)
            {
                // Side faces (quads)
                for (int i = 0; i < nVerts; i++)
                {
                    int v0 = face[i];
                    int v1 = face[(i + 1) % nVerts];
                    int inset0 = insetVertexMap[(f, i)];
                    int inset1 = insetVertexMap[(f, (i + 1) % nVerts)];
                    int[] quad = new int[] { v0, v1, inset1, inset0 };
                    string sig = GetFaceSignature(quad.Select(idx => newVertices[idx]).ToArray(), faceSignatureRounding);
                    if (!signatureToColor.ContainsKey(sig))
                        signatureToColor[sig] = nextColorIndex++;
                    newFaces.Add(quad);
                    newColorIndices.Add(signatureToColor[sig]);
                }

                // Inset face (maintain original winding)
                int[] insetFace = Enumerable.Range(0, nVerts)
                    .Select(i => insetVertexMap[(f, i)])
                    .ToArray();
                string insetSig = GetFaceSignature(insetFace.Select(idx => newVertices[idx]).ToArray(), faceSignatureRounding);
                if (!signatureToColor.ContainsKey(insetSig))
                    signatureToColor[insetSig] = nextColorIndex++;
                newFaces.Add(insetFace);
                newColorIndices.Add(signatureToColor[insetSig]);
            }
            else
            {
                newFaces.Add(face);
                newColorIndices.Add(colorIndices[f]);
            }
        }

        return NormalizePolyhedron((newVertices.ToArray(), newFaces.ToArray(), newColorIndices.ToArray()));
    }

    private static bool NeedsWindingFlip(List<Vector3> vertices, int[] faceIndices, Vector3 center)
    {
        // Get face center
        Vector3 faceCenter = Vector3.zero;
        foreach (int idx in faceIndices)
            faceCenter += vertices[idx];
        faceCenter /= faceIndices.Length;

        // Calculate face normal and outward direction
        Vector3 normal = CalculateFaceNormal(vertices, faceIndices);
        Vector3 outward = (faceCenter - center).normalized;

        // Return true if normal points inward
        return Vector3.Dot(normal, outward) < 0;
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
    public static (List<Vector3> meshVertices, List<int> triangles, List<Vector3> normals, List<int> colorIndices)
        ApplyFlatShade((Vector3[], int[][], int[]) input)
    {
        var (vertices, faces, faceColorIndices) = input;

        List<Vector3> meshVertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<int> colorIndices = new List<int>();

        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            int colorIndex = faceColorIndices[f];

            Vector3[] faceVerts = face.Select(idx => vertices[idx]).ToArray();
            Vector3 normal = CalculateFaceNormal(faceVerts.ToList(), Enumerable.Range(0, faceVerts.Length).ToArray());

            // Triangulate the planar face
            TriangulatePlanarFace(
                faceVerts,
                normal,
                meshVertices,
                triangles,
                normals,
                colorIndices,
                colorIndex // pass color index instead of Color
            );
        }

        return (meshVertices, triangles, normals, colorIndices);
    }

    public static Mesh BuildMesh(
    (List<Vector3> meshVertices, List<int> triangles, List<Vector3> normals, List<int> colorIndices) input,
    PolyhedronPalette palette)
    {
        var (meshVertices, triangles, normals, colorIndices) = input;

        // Map color indices to actual colors
        List<Color> colors = colorIndices.Select(idx => palette.GetColor(idx)).ToList();

        Mesh mesh = new Mesh();
        mesh.SetVertices(meshVertices);
        mesh.SetTriangles(triangles, 0);
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
        List<int> colorIndices,
        int faceColorIndex)
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
        colorIndices.Add(faceColorIndex);

        // Add perimeter vertices
        int baseIndex = vertices.Count;
        foreach (var v in faceVertices)
        {
            vertices.Add(v);
            normals.Add(faceNormal);
            colorIndices.Add(faceColorIndex);
        }

        // Check winding direction for first triangle
        Vector3 e1 = vertices[baseIndex] - center;
        Vector3 e2 = vertices[baseIndex + 1] - center;
        Vector3 computedNormal = Vector3.Cross(e1, e2).normalized;
        bool needsFlip = Vector3.Dot(computedNormal, faceNormal) < 0;

        // Create triangles fan from center with correct winding
        for (int i = 0; i < faceVertices.Length; i++)
        {
            if (!needsFlip)
            {
                tris.Add(centerIndex);
                tris.Add(baseIndex + i);
                tris.Add(baseIndex + ((i + 1) % faceVertices.Length));
            }
            else
            {
                tris.Add(centerIndex);
                tris.Add(baseIndex + ((i + 1) % faceVertices.Length));
                tris.Add(baseIndex + i);
            }
        }
    }

    // Add this as a class-level method, before any operator methods
    private static string GetFaceSignature(Vector3[] faceVerts, int rounding)
    {
        var lengths = new List<float>();
        for (int i = 0; i < faceVerts.Length; i++)
        {
            Vector3 v1 = faceVerts[i];
            Vector3 v2 = faceVerts[(i + 1) % faceVerts.Length];
            lengths.Add(Vector3.Distance(v1, v2));
        }
        lengths.Sort();
        // Use rounding parameter to control precision
        var signature = string.Join(",", lengths.Select(l =>
            Math.Round(l, rounding).ToString($"F{rounding}", CultureInfo.InvariantCulture)));
        return signature;
    }

    // Add this helper method at class level
    private static (Vector3[], int[][], int[]) NormalizePolyhedron((Vector3[], int[][], int[]) poly)
    {
        var (vertices, faces, colors) = poly;

        // Find center of polyhedron
        Vector3 center = Vector3.zero;
        foreach (var v in vertices)
            center += v;
        center /= vertices.Length;

        // Find maximum distance from center to any vertex (circumradius)
        float maxRadius = 0f;
        foreach (var v in vertices)
        {
            float distance = Vector3.Distance(center, v);
            maxRadius = Mathf.Max(maxRadius, distance);
        }

        // Target radius (use 1 as standard size)
        float targetRadius = 1f;
        float scale = targetRadius / maxRadius;

        // Create normalized vertices
        Vector3[] normalizedVertices = vertices
            .Select(v => (v - center) * scale + center)
            .ToArray();

        return (normalizedVertices, faces, colors);
    }

    // Add this helper method at class level
    private static Vector3 CalculateFaceCenter(Vector3[] faceVerts)
    {
        Vector3 center = Vector3.zero;
        foreach (var v in faceVerts)
            center += v;
        return center / faceVerts.Length;
    }

    public static (Vector3[], int[][], int[]) ApplyColorRemap(
        (Vector3[], int[][], int[]) input,
        Dictionary<int, int> colorRemap,
        int paletteLength
    )
    {
        var (vertices, faces, colorIndices) = input;
        var newColorIndices = new int[colorIndices.Length];
        for (int i = 0; i < colorIndices.Length; i++)
        {
            int oldIdx = colorIndices[i];
            bool remapped = false;
            foreach (var kv in colorRemap)
            {
                if ((oldIdx % paletteLength) == (kv.Key % paletteLength))
                {
                    // Preserve the offset from the base index
                    int offset = oldIdx - kv.Key;
                    newColorIndices[i] = kv.Value + offset;
                    remapped = true;
                    break;
                }
            }
            if (!remapped)
                newColorIndices[i] = oldIdx;
        }
        return (vertices, faces, newColorIndices);
    }
}

