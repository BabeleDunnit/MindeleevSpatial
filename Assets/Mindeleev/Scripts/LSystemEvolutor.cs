using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach this to a GameObject with a PolyhedronGenerator to evolve its recipeString using L-System rules.
/// </summary>
public class LSystemEvolutor : MonoBehaviour
{
    [Header("L-System Rules")]
    // Example rules: key = symbol string, value = replacement string
    // You can edit these in code for different L-Systems
    public Dictionary<string, string> rules = new Dictionary<string, string>
    {
        { "tk", "n" },   // tk -> n
        { "t", "tk" },   // t -> t followed by k
        { "k", "n" },    // k -> n
        { "n", "a" },    // n -> a
        { "a", "d" },    // a -> d
        { "d", "" },     // d -> (empty)
        { "l", "tl" },   // l -> t followed by l
        { "T", "T" },    // base polyhedra remain unchanged
        { "C", "O" },
        { "O", "tO" },
        { "I", "I" },
        { "D", "D" },
        { "dlC", "C" },
    };

    [Header("Evolve Settings")]
    public bool evolveOnStart = false;

    private PolyhedronGenerator polyGen;

    void Start()
    {
        polyGen = GetComponent<PolyhedronGenerator>();
        if (evolveOnStart)
            Evolve();
    }

    /// <summary>
    /// Applies the L-System rules to the current recipeString and rebuilds the mesh.
    /// </summary>
    public void Evolve()
    {
        if (polyGen == null)
            polyGen = GetComponent<PolyhedronGenerator>();
        if (polyGen == null || string.IsNullOrEmpty(polyGen.recipeString))
            return;

        string evolved = ApplyLSystem(polyGen.recipeString);
        if (evolved == polyGen.recipeString)
            return; // Stable, no change

        polyGen.recipeString = evolved;
        polyGen.RebuildMesh(); // You need to implement this in PolyhedronGenerator
    }

    /// <summary>
    /// Applies L-System rules to a recipe string (string-to-string replacement).
    /// </summary>
    private string ApplyLSystem(string recipe)
    {
        // Build a list of rules sorted by descending key length
        var sortedRules = new List<KeyValuePair<string, string>>(rules);
        sortedRules.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));

        int matchIndex = -1;
        string matchKey = null;
        string replacement = null;

        foreach (var kv in sortedRules)
        {
            int idx = recipe.IndexOf(kv.Key, System.StringComparison.Ordinal);
            if (idx != -1)
            {
                matchIndex = idx;
                matchKey = kv.Key;
                replacement = kv.Value;
                break; // Only apply one rule per evolve
            }
        }

        if (matchIndex != -1 && matchKey != null)
        {
            // Replace only the first occurrence
            return recipe.Substring(0, matchIndex) + replacement + recipe.Substring(matchIndex + matchKey.Length);
        }
        else
        {
            // No rule matched, return original
            return recipe;
        }
    }
}