using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolyhedronGenerator : MonoBehaviour
{
    /* ------------------------------------------------------------------
     *  STATIC DATA: 5 canonical polyhedra expressed as (vertices, faces)
     * ----------------------------------------------------------------*/
    // Cube (C)
    private static readonly (Vector3[], int[][], int[]) Cube = (
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
    private static readonly (Vector3[], int[][], int[]) Tetrahedron = (
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
    private static readonly (Vector3[], int[][], int[]) Octahedron = (
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
    private static readonly (Vector3[], int[][], int[]) Dodecahedron = (
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
    private static readonly (Vector3[], int[][], int[]) Icosahedron = (
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

    /* ------------------------------------------------------------------
     *  Inspector settings
     * ----------------------------------------------------------------*/
    public string polyhedronRecipe = "C"; // default Cube
    public PolyhedronPalette palette;
    private bool showVertexIndices = false;
    public Material polyhedronMaterial; // Add this field

    /* ------------------------------------------------------------------ */
    void Start()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        var polyData = ParsePolyhedronRecipe(polyhedronRecipe);
        filter.mesh = ApplyFlatShade(polyData, palette);
        ApplyPolyhedronMaterial(renderer);

        if (showVertexIndices)
        {
            ShowVertexIndices(polyData.Item1); // Use logical vertices
        }
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
    private (Vector3[], int[][], int[]) ParsePolyhedronRecipe(string recipe)
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
        var tokens = new List<(char op, int? faces, float factor)>();
        int i = 0;
        while (i < basePos)
        {
            char c = recipe[i];
            if (char.IsLower(c))
            {
                int? faces = null;
                float factor = 0.1f;

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

                    // Parse first parameter (faces)
                    if (parameters.Length > 0 && int.TryParse(parameters[0], out int facesParam))
                        faces = facesParam;

                    // Parse second parameter (factor)
                    if (parameters.Length > 1 && float.TryParse(parameters[1], 
                        NumberStyles.Float, CultureInfo.InvariantCulture, out float factorParam))
                        factor = factorParam;

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
                            faces = simpleParam;
                    }
                    i = j;
                }

                tokens.Add((c, faces, factor));
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

            var (op, faces, factor) = tokens[t];
            // Substitute truncate operator with its equivalent sequence
            if (op == 't')
            {
                // Apply d->k->d sequence for truncation
                current = ApplyDual(current);
                current = ApplyKis(current, factor, faces);
                current = ApplyDual(current);
            }
            else
            {
                current = op switch
                {
                    'k' => ApplyKis(current, factor, faces),
                    'a' => ApplyAmbo(current),
                    'd' => ApplyDual(current),
                    'f' => ApplyFuckedLoft(current, 0.5f, extrudeDistance: 0, targetFaces: faces), // Fix parameter order
                    'n' => ApplyInsetN(current),
                    _ => current,
                    
                };
            }
        }

        return current;
    }

    /* ---------------------- OPERATORS -------------------------------- */
    public static (Vector3[], int[][], int[]) ApplyKis(
        (Vector3[], int[][], int[]) input, 
        float height, 
        int? targetFaces = null)
    {
        var (vertices, faces, colorIndices) = input;
        var newVertices = new List<Vector3>(vertices);
        var newFaces = new List<int[]>();
        var newColorIndices = new List<int>();

        var signatureToColor = new Dictionary<string, int>();
        int nextColorIndex = 0;

        // Process each face
        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            
            // Calculate face signature even for unchanged faces
            Vector3[] faceVerts = face.Select(idx => vertices[idx]).ToArray();
            string signature = GetFaceSignature(faceVerts);
            
            if (targetFaces.HasValue && face.Length != targetFaces.Value)
            {
                // Copy face unchanged but still use its signature for coloring
                newFaces.Add(face);
                
                // Assign color based on signature
                if (!signatureToColor.ContainsKey(signature))
                    signatureToColor[signature] = nextColorIndex++;
                newColorIndices.Add(signatureToColor[signature]);
                continue;
            }

            // Calculate face center and normal for pyramidal faces
            Vector3 center = Vector3.zero;
            foreach (int idx in face)
                center += vertices[idx];
            center /= face.Length;

            // Add apex vertex
            Vector3 normal = CalculateFaceNormal(vertices, face);
            Vector3 apex = center + normal * height;
            int apexIndex = newVertices.Count;
            newVertices.Add(apex);

            // Create new triangular faces
            for (int i = 0; i < face.Length; i++)
            {
                int v1 = face[i];
                int v2 = face[(i + 1) % face.Length];
                int[] newFace = new[] { v1, v2, apexIndex };
                
                // Calculate signature for new triangular face
                Vector3[] newFaceVerts = newFace.Select(idx => newVertices[idx]).ToArray();
                string newSignature = GetFaceSignature(newFaceVerts);
                
                if (!signatureToColor.ContainsKey(newSignature))
                    signatureToColor[newSignature] = nextColorIndex++;
                
                newFaces.Add(newFace);
                newColorIndices.Add(signatureToColor[newSignature]);
            }
        }

        return NormalizePolyhedron((newVertices.ToArray(), newFaces.ToArray(), newColorIndices.ToArray()));
    }

    public static (Vector3[], int[][], int[]) ApplyAmbo((Vector3[], int[][], int[]) input)
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

    public static (Vector3[], int[][], int[]) ApplyDual((Vector3[], int[][], int[]) input)
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
            string signature = GetFaceSignature(faceVerts);
            if (!signatureToColor.ContainsKey(signature))
                signatureToColor[signature] = nextColorIndex++;

            dualFaces.Add(orderedFaces.ToArray());
            dualColors.Add(signatureToColor[signature]);
        }

        var result = (dualVertices, dualFaces.ToArray(), dualColors.ToArray());
        return NormalizePolyhedron(result);
    }

    public static (Vector3[], int[][], int[]) ApplyStellation(
        (Vector3[], int[][], int[]) input, 
        float height, 
        int? targetFaces = null)
    {
        var (vertices, faces, colorIndices) = input;
        var newVertices = new List<Vector3>(vertices);
        var newFaces = new List<int[]>();
        var newColorIndices = new List<int>();

        var signatureToColor = new Dictionary<string, int>();
        int nextColorIndex = 0;

        // Process each face
        for (int f = 0; f < faces.Length; f++)
        {
            int[] face = faces[f];
            Vector3[] faceVerts = face.Select(idx => vertices[idx]).ToArray();
            string signature = GetFaceSignature(faceVerts);

            if (targetFaces.HasValue && face.Length != targetFaces.Value)
            {
                // Copy face unchanged but still use its signature for coloring
                newFaces.Add(face);
                if (!signatureToColor.ContainsKey(signature))
                    signatureToColor[signature] = nextColorIndex++;
                newColorIndices.Add(signatureToColor[signature]);
                continue;
            }

            // Calculate face center and normal
            Vector3 center = Vector3.zero;
            foreach (int idx in face)
                center += vertices[idx];
            center /= face.Length;

            Vector3 normal = CalculateFaceNormal(vertices, face);
            Vector3 offset = normal * height;

            // Create new vertices by pushing existing ones outward
            var stellatedFaceIndices = new int[face.Length];
            for (int i = 0; i < face.Length; i++)
            {
                Vector3 originalVertex = vertices[face[i]];
                Vector3 stellatedVertex = originalVertex + offset;
                stellatedFaceIndices[i] = newVertices.Count;
                newVertices.Add(stellatedVertex);
            }

            // Add new stellated face
            Vector3[] newFaceVerts = stellatedFaceIndices.Select(idx => newVertices[idx]).ToArray();
            string newSignature = GetFaceSignature(newFaceVerts);
            if (!signatureToColor.ContainsKey(newSignature))
                signatureToColor[newSignature] = nextColorIndex++;

            newFaces.Add(stellatedFaceIndices);
            newColorIndices.Add(signatureToColor[newSignature]);

            // Add side faces connecting original to stellated
            for (int i = 0; i < face.Length; i++)
            {
                int nextI = (i + 1) % face.Length;
                int[] sideFace = new[] 
                { 
                    face[i], 
                    face[nextI], 
                    stellatedFaceIndices[nextI], 
                    stellatedFaceIndices[i] 
                };

                Vector3[] sideFaceVerts = sideFace.Select(idx => newVertices[idx]).ToArray();
                string sideSignature = GetFaceSignature(sideFaceVerts);
                if (!signatureToColor.ContainsKey(sideSignature))
                    signatureToColor[sideSignature] = nextColorIndex++;

                newFaces.Add(sideFace);
                newColorIndices.Add(signatureToColor[sideSignature]);
            }
        }

        var result = (newVertices.ToArray(), newFaces.ToArray(), newColorIndices.ToArray());
        return NormalizePolyhedron(result);
    }

    public static (Vector3[], int[][], int[]) ApplyFuckedLoft(
        (Vector3[], int[][], int[]) input,
        float insetDistance = 0.5f,      // Distance to inset vertices toward the face center
        float extrudeDistance = -0.2f,   // Distance to extrude vertices along the face normal
        int? targetFaces = null)         // Target face count (e.g., 3 for triangles, 4 for quads)
    {
        var (vertices, faces, colorIndices) = input;
        var newVertices = new List<Vector3>(vertices); // Start with original vertices
        var newFaces = new List<int[]>();
        var newColorIndices = new List<int>();

        var signatureToColor = new Dictionary<string, int>();
        int nextColorIndex = 0;

        // Precompute centers and normals for every face
        var centers = new Vector3[faces.Length];
        var normals = new Vector3[faces.Length];
        for (int f = 0; f < faces.Length; f++)
        {
            var faceVerts = faces[f].Select(idx => vertices[idx]).ToArray();
            centers[f] = CalculateFaceCenter(faceVerts);
            normals[f] = CalculateFaceNormal(faceVerts.ToList(), Enumerable.Range(0, faceVerts.Length).ToArray());
        }

        // Process each face
        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            if (targetFaces.HasValue && face.Length != targetFaces.Value)
            {
                // Copy unchanged face
                newFaces.Add(face);
                newColorIndices.Add(colorIndices[f]);
                continue;
            }

            // Create inset vertices for the current face
            int[] insetFace = new int[face.Length];
            for (int i = 0; i < face.Length; i++)
            {
                int v = face[i];
                Vector3 insetPoint = Vector3.Lerp(vertices[v], centers[f], insetDistance);
                insetPoint += normals[f] * extrudeDistance;

                // Add the inset vertex to the newVertices list
                insetFace[i] = newVertices.Count;
                newVertices.Add(insetPoint);
            }

            // Add the inset face (replacing the original face)
            Vector3[] insetVerts = insetFace.Select(idx => newVertices[idx]).ToArray();
            string insetSignature = GetFaceSignature(insetVerts);
            if (!signatureToColor.ContainsKey(insetSignature))
                signatureToColor[insetSignature] = nextColorIndex++;
            newFaces.Add(insetFace);
            newColorIndices.Add(signatureToColor[insetSignature]);

            // Add side faces (quads) connecting original vertices to inset vertices
            for (int i = 0; i < face.Length; i++)
            {
                int next = (i + 1) % face.Length;
                int[] quad = new[]
                {
                    face[i],
                    face[next],
                    insetFace[next],
                    insetFace[i]
                };

                Vector3[] quadVerts = quad.Select(idx => newVertices[idx]).ToArray();
                string quadSignature = GetFaceSignature(quadVerts);
                if (!signatureToColor.ContainsKey(quadSignature))
                    signatureToColor[quadSignature] = nextColorIndex++;
                newFaces.Add(quad);
                newColorIndices.Add(signatureToColor[quadSignature]);
            }
        }

        return NormalizePolyhedron((newVertices.ToArray(), newFaces.ToArray(), newColorIndices.ToArray()));
    }

    public static (Vector3[], int[][], int[]) ApplyInsetN(
        (Vector3[], int[][], int[]) input,
        int n = 0,
        float insetDistance = 0.5f,
        float extrudeDistance = 0.0f)
    {
        var (vertices, faces, colorIndices) = input;
        var newVertices = new List<Vector3>(vertices);
        var newFaces = new List<int[]>();
        var newColorIndices = new List<int>();

        var signatureToColor = new Dictionary<string, int>();
        int nextColorIndex = 0;

        // Precompute centers and normals for every face
        var centers = new Vector3[faces.Length];
        var normals = new Vector3[faces.Length];
        for (int f = 0; f < faces.Length; f++)
        {
            var faceVerts = faces[f].Select(idx => vertices[idx]).ToArray();
            centers[f] = CalculateFaceCenter(faceVerts);
            normals[f] = CalculateFaceNormal(faceVerts.ToList(), Enumerable.Range(0, faceVerts.Length).ToArray());
        }

        // Map for new inset vertices: [faceIndex][vertexIndexInFace] = newVertexIndex
        var insetVertexMap = new Dictionary<(int, int), int>();

        // Create inset vertices for each target face
        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            if (n == 0 || face.Length == n)
            {
                for (int i = 0; i < face.Length; i++)
                {
                    int v = face[i];
                    Vector3 insetPoint = Vector3.Lerp(vertices[v], centers[f], insetDistance) + normals[f] * extrudeDistance;
                    int newIdx = newVertices.Count;
                    newVertices.Add(insetPoint);
                    insetVertexMap[(f, i)] = newIdx;
                }
            }
        }

        // Build new faces
        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            int nVerts = face.Length;
            bool isTarget = (n == 0 || nVerts == n);

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
                    string sig = GetFaceSignature(quad.Select(idx => newVertices[idx]).ToArray());
                    if (!signatureToColor.ContainsKey(sig))
                        signatureToColor[sig] = nextColorIndex++;
                    newFaces.Add(quad);
                    newColorIndices.Add(signatureToColor[sig]);
                }
                // Inset face (reverse order for correct normal)
                int[] insetFace = Enumerable.Range(0, nVerts)
                    .Select(i => insetVertexMap[(f, i)]).Reverse().ToArray();
                string insetSig = GetFaceSignature(insetFace.Select(idx => newVertices[idx]).ToArray());
                if (!signatureToColor.ContainsKey(insetSig))
                    signatureToColor[insetSig] = nextColorIndex++;
                newFaces.Add(insetFace);
                newColorIndices.Add(signatureToColor[insetSig]);
            }
            else
            {
                // Non-target faces: just copy
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
        Vector3 outwardDir = (faceCenter - center).normalized;

        // Return true if normal points inward
        return Vector3.Dot(normal, outwardDir) < 0;
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
        (Vector3[], int[][], int[]) input,
        PolyhedronPalette palette)
    {
        var (vertices, faces, colorIndices) = input;
        
        List<Vector3> meshVertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Color> colors = new List<Color>();

        // Validate color indices
        for (int f = 0; f < faces.Length; f++)
        {
            if (colorIndices[f] < 0)
            {
                Debug.LogError($"Invalid color index {colorIndices[f]} for face {f}. Using fallback color index 0.");
                colorIndices[f] = 0;
            }
        }

        Debug.Log($"[FlatShade] Mesh has {faces.Length} faces, {vertices.Length} vertices");

        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            Color faceColor = palette.GetColor(colorIndices[f]);

            // Get face vertices
            Vector3[] faceVerts = face.Select(idx => vertices[idx]).ToArray();
            
            // Calculate face normal using Newell's method
            Vector3 normal = CalculateFaceNormal(faceVerts.ToList(), Enumerable.Range(0, faceVerts.Length).ToArray());

            // Triangulate the planar face
            TriangulatePlanarFace(
                faceVerts,
                normal,
                meshVertices,
                triangles,
                normals,
                colors,
                faceColor
            );
        }

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

    private void ShowVertexIndices(Vector3[] vertices)
    {
        Debug.Log($"[ShowVertexIndices] vertices count: {vertices.Length}");
        for (int i = 0; i < vertices.Length; i++)
        {
            GameObject label = new GameObject($"VertexLabel_{i}");
            label.transform.SetParent(this.transform, false);
            label.transform.localPosition = vertices[i];
            label.transform.localScale = Vector3.one * 0.15f; // Adjust for readability

            var text = label.AddComponent<TextMesh>();
            text.text = i.ToString();
            text.fontSize = 48;
            text.characterSize = 0.2f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.black;
        }
    }

    // Add this as a class-level method, before any operator methods
    private static string GetFaceSignature(Vector3[] faceVerts, float rounding = 1)
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
            Math.Round(l, (int)rounding).ToString($"F{(int)rounding}", CultureInfo.InvariantCulture)));
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
}

