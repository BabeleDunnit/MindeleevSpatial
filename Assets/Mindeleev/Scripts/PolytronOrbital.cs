using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Globalization;
using TMPro;



public class PolytronOrbital : MonoBehaviour
{
    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public float orbitalRadius = 2.0f;
    public float orbitalYOffset = 0.0f;


    public struct Elettrone
    {
        public int n;
        public int l;
        public int m;
        public int s;

        public string OrbitaleKey => $"{n}{OrbitalSymbol(l)}";

        private static string OrbitalSymbol(int l)
        {
            return l switch
            {
                0 => "s",
                1 => "p",
                2 => "d",
                3 => "f",
                4 => "g", // teorici
                _ => $"l{l}"
            };
        }
    }
    static int ParseOrder(string key)
    {
        // Per ordinare gli orbitali in output (es: 1s, 2s, 2p, 3s...)
        int n = int.Parse(key.Substring(0, key.Length - 1));
        char lChar = key[^1];
        int l = lChar switch
        {
            's' => 0,
            'p' => 1,
            'd' => 2,
            'f' => 3,
            'g' => 4,
            _ => 99
        };
        return (n + l) * 10 + n; // ordina come Madelung
    }

    public void GenerateAufbau()
    {
        Debug.Log("inizio Aufbau");
        int maxZ = 120; // cambia a piacere
        var elettroni = new List<Elettrone>();

        // Generazione completa
        for (int n = 1; n <= 7; n++)
        {
            for (int l = 0; l < n; l++)
            {
                for (int m = -l; m <= l; m++)
                {
                    for (int s = -1; s <= 1; s += 2)
                    {
                        elettroni.Add(new Elettrone { n = n, l = l, m = m, s = s });
                    }
                }
            }
        }

        // Ordinamento Madelung (n + l, poi n)
        var sorted = elettroni
            .OrderBy(e => e.n + e.l)
            .ThenBy(e => e.n)
            .ToList();

        // Costruzione configurazioni per ogni Z
        var config = new Dictionary<string, int>();

        for (int z = 1; z <= maxZ; z++)
        {
            var e = sorted[z - 1];
            string key = e.OrbitaleKey;

            if (!config.ContainsKey(key))
                config[key] = 0;
            config[key]++;

            // Output configurazione per Z corrente
            Debug.Log($"z: {z}");
            Debug.Log(string.Join(" ",
                config
                    .OrderBy(kvp => int.Parse(kvp.Key.Substring(0, kvp.Key.Length - 1))) // n
                    .ThenBy(kvp => "spdfgh".IndexOf(kvp.Key[^1])) // l: s=0, p=1, d=2, ...
                    .Select(kvp => $"{kvp.Key}{kvp.Value}")
            ));
            Debug.Log("");
        }
        Debug.Log("fine aufbau");
    }

    private void GenerateOrbital()
    {
        var generator = GetComponent<PolyhedronGenerator>();
        if (generator == null || polytronPrefab == null)
        {
            Debug.LogWarning("PolyhedronGenerator or PolytronPrefab not assigned.");
            return;
        }

        // Extract the base polyhedron (last uppercase letter in the recipe)
        string recipe = generator.polyhedronRecipe;
        char basePoly = recipe.LastOrDefault(c => char.IsUpper(c));
        if (basePoly == default)
        {
            Debug.LogWarning("No base polyhedron found in recipe: " + recipe);
            return;
        }

        // Prepare the base recipe string
        string baseRecipe = basePoly.ToString();

        // try to clone the Pauli/Aufbau sequence
        // use something analogous to quantic numbers

        /*
          for (int n = 1; n <= 7; n++) {
    for (int l = 0; l <= n - 1; l++) {
      for (int m = -l; m <= l; m++) {
        for (int s = -1; s <= 1; s += 2) {
*/



        string[] ops = { "k", "a", "n", "l" };

        for (int i = 2; i < 6; i++)
        {
            orbitalRadius = i * 2;
            for (int j = 0; j < i; j++)
            {
                if (i < 2) continue;
                float angle = j * Mathf.PI * 2f / (float)i;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * orbitalRadius,
                    orbitalYOffset,
                    Mathf.Sin(angle) * orbitalRadius
                );
                Vector3 position = transform.position + offset;

                GameObject orbital = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
                orbital.transform.localScale = transform.localScale * 0.3f; // Match nucleus scale

                // Set the base recipe
                var orbitalGen = orbital.GetComponent<PolyhedronGenerator>();
                if (orbitalGen != null)
                {
                    orbitalGen.polyhedronRecipe = string.Concat(Enumerable.Repeat(ops[i - 2], j)) + baseRecipe;
                }

                orbital.name = $"Orbital_{baseRecipe}_{i + 1}";
            }
        }

    }

    void Start()
    {

        // GenerateAufbau();


        // Get the PolyhedronGenerator on this nucleus
        var generator = GetComponent<PolyhedronGenerator>();
        if (generator == null || polytronPrefab == null)
        {
            Debug.LogWarning("PolyhedronGenerator or PolytronPrefab not assigned.");
            return;
        }

        float dist = 2.0f;
        int polytronNumber = 0;
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                for (int z = 0; z < 10; z++)
                {
                    string recipe = PolyhedronRecipeEnumerator.IntToRecipe(polytronNumber);
                    Debug.Log($"{polytronNumber} {recipe}");
                    Vector3 offset = new Vector3(x * dist, y * dist, z * dist);
                    Vector3 position = transform.position + offset;

                    GameObject poly = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
                    poly.transform.localScale = transform.localScale * 0.3f;

                    var polyGen = poly.GetComponent<PolyhedronGenerator>();
                    if (polyGen != null)
                    {
                        polyGen.polyhedronRecipe = recipe;
                    }

                    poly.name = $"P_{polytronNumber}_{recipe}";

                    CreateLabel(poly, recipe, position);

                    polytronNumber++;

                }

            }

        }


    }


    private void CreateLabel(GameObject parent, string recipe, Vector3 position)
    {
        // Create a TextMeshPro object for the label
        GameObject label = new GameObject($"Label_{recipe}");
        label.transform.parent = parent.transform;
        label.transform.localPosition = Vector3.down * 1.5f;
        
        // Add TextMeshPro component instead of TextMesh
        TextMeshPro tmpText = label.AddComponent<TextMeshPro>();
        tmpText.text = recipe;
        tmpText.fontSize = 3;  // TMP uses different scale for font size
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        
        // Configure the TMP text
        tmpText.enableAutoSizing = true;
        tmpText.fontSizeMin = 1;
        tmpText.fontSizeMax = 2;
        
        // Set material and other rendering properties
        tmpText.material = new Material(Shader.Find("TextMeshPro/Mobile/Distance Field"));
        
        // Make text more readable by placing it vertically and facing forward
        label.transform.localRotation = Quaternion.identity;
        
        // Set rect transform properties
        RectTransform rectTransform = tmpText.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(2, 0.5f);  // Width and height of the text area
    
        // Optional: Add horizontal billboard script for camera facing
        // label.AddComponent<HorizontalBillboard>();
    }


}




public static class PolyhedronRecipeEnumerator
{
    // Operator codes and their parameter spaces
    private static readonly char[] Operators = { 't', 'k', 'a', 'd', 'n', 'l' };
    private static readonly int[] FaceSidesFilter = { 0, 3, 4, 5 };
    private static readonly int[] FaceSignatureRounding = { 0, 1, 2, 3 };
    private static readonly float[] ParamValues = Enumerable.Range(0, 11).Select(i => -0.5f + 0.1f * i).ToArray();
    private static readonly char[] BasePolyhedra = { 'C', 'T', 'O', 'D', 'I' };

    // Token structure
    private struct Token
    {
        public int opIdx;
        public int faceSidesIdx;
        public int roundingIdx;
        public int param0Idx;
        public int param1Idx; // Only for n (InsetN)
    }

    // Maximum number of tokens in a recipe (adjust as needed)
    private const int MaxTokens = 5;

    // Encode a single token as an integer
    private static int EncodeToken(Token t)
    {
        // For each operator, encode only relevant params
        switch (Operators[t.opIdx])
        {
            case 'k': // Kis
                return t.opIdx
                    + Operators.Length * t.faceSidesIdx
                    + Operators.Length * FaceSidesFilter.Length * t.roundingIdx
                    + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * t.param0Idx;
            case 'n': // InsetN
                return t.opIdx
                    + Operators.Length * t.faceSidesIdx
                    + Operators.Length * FaceSidesFilter.Length * t.roundingIdx
                    + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * t.param0Idx
                    + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * ParamValues.Length * t.param1Idx;
            case 't': // Truncate (same as Kis)
                return t.opIdx
                    + Operators.Length * t.faceSidesIdx
                    + Operators.Length * FaceSidesFilter.Length * t.roundingIdx
                    + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * t.param0Idx;
            default: // a, d, l: only rounding
                return t.opIdx
                    + Operators.Length * t.roundingIdx;
        }
    }

    // Decode a single token from an integer (for demonstration, not used in main mapping)
    // ...

    // Get the number of possible tokens for each operator
    private static int TokenSpaceSize(int opIdx)
    {
        char op = Operators[opIdx];
        if (op == 'k' || op == 't')
            return FaceSidesFilter.Length * FaceSignatureRounding.Length * ParamValues.Length;
        if (op == 'n')
            return FaceSidesFilter.Length * FaceSignatureRounding.Length * ParamValues.Length * ParamValues.Length;
        // a, d, l
        return FaceSignatureRounding.Length;
    }

    // Get the total number of possible tokens
    private static int TotalTokenSpace()
    {
        int total = 0;
        for (int i = 0; i < Operators.Length; i++)
            total += TokenSpaceSize(i);
        return total;
    }

    // Map an integer to a recipe string
    public static string IntToRecipe(int n)
    {
        // 1. Choose base polyhedron
        int basePolyIdx = n % BasePolyhedra.Length;
        n /= BasePolyhedra.Length;

        // 2. Build operator tokens (from least to most significant)
        List<string> tokens = new List<string>();
        int tokenCount = 0;
        while (n > 0 && tokenCount < MaxTokens)
        {
            int opIdx = n % Operators.Length;
            n /= Operators.Length;

            char op = Operators[opIdx];
            string token = op.ToString();

            if (op == 'k' || op == 't')
            {
                int param0Idx = n % ParamValues.Length; n /= ParamValues.Length;
                int roundingIdx = n % FaceSignatureRounding.Length; n /= FaceSignatureRounding.Length;
                int faceSidesIdx = n % FaceSidesFilter.Length; n /= FaceSidesFilter.Length;
                token += $"({FaceSidesFilter[faceSidesIdx]},{FaceSignatureRounding[roundingIdx]},{ParamValues[param0Idx].ToString("0.0", CultureInfo.InvariantCulture)})";
            }
            else if (op == 'n')
            {
                int param1Idx = n % ParamValues.Length; n /= ParamValues.Length;
                int param0Idx = n % ParamValues.Length; n /= ParamValues.Length;
                int roundingIdx = n % FaceSignatureRounding.Length; n /= FaceSignatureRounding.Length;
                int faceSidesIdx = n % FaceSidesFilter.Length; n /= FaceSidesFilter.Length;
                token += $"({FaceSidesFilter[faceSidesIdx]},{FaceSignatureRounding[roundingIdx]},{ParamValues[param0Idx].ToString("0.0", CultureInfo.InvariantCulture)},{ParamValues[param1Idx].ToString("0.0", CultureInfo.InvariantCulture)})";
            }
            else // a, d, l
            {
                int roundingIdx = n % FaceSignatureRounding.Length; n /= FaceSignatureRounding.Length;
                token += $"({FaceSignatureRounding[roundingIdx]})";
            }

            tokens.Add(token);
            tokenCount++;
        }

        // 3. Compose recipe (reverse tokens for left-to-right application)
        tokens.Reverse();
        string recipe = string.Concat(tokens) + BasePolyhedra[basePolyIdx];
        return recipe;
    }

    // Map a recipe string to an integer (inverse mapping)
    // This is more complex and requires parsing the recipe string.
    // You can implement this by parsing each token and base polyhedron, then combining the indices using the same mixed-radix logic as above.
    // For brevity, only IntToRecipe is shown here.
}