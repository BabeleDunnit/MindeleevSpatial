using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Attach this to a GameObject with a PolyhedronGenerator to evolve its recipeString using L-System rules.
/// </summary>
public class LSystemEvolutor : MonoBehaviour
{
    [Header("L-System Rules")]
    public List<LSystemRule> rules = new List<LSystemRule>
    {
        new LSystemRule { key = "tk", value = "n" },
        new LSystemRule { key = "t", value = "tk" },
        new LSystemRule { key = "k", value = "n" },
        new LSystemRule { key = "n", value = "a" },
        new LSystemRule { key = "a", value = "d" },
        new LSystemRule { key = "d", value = "" },
        new LSystemRule { key = "l", value = "tl" },
        new LSystemRule { key = "T", value = "T" },
        new LSystemRule { key = "C", value = "O" },
        new LSystemRule { key = "O", value = "tO" },
        new LSystemRule { key = "I", value = "I" },
        new LSystemRule { key = "D", value = "D" },
        new LSystemRule { key = "dlC", value = "C" },
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
        var sortedRules = new List<KeyValuePair<string, string>>(rules.ToDictionary(r => r.key, r => r.value));
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

    // Use this for runtime lookups
    public Dictionary<string, string> GetRuleDictionary()
    {
        return rules.ToDictionary(r => r.key, r => r.value);
    }
}

[System.Serializable]
public class LSystemRule
{
    public string key;
    public string value;
}


// TODO: use this hereabove
public static class LSystem
{
    public static List<LSystemRule> rules = new List<LSystemRule>
    {
        new LSystemRule { key = "tk", value = "n" },
        new LSystemRule { key = "t", value = "tk" },
        new LSystemRule { key = "k", value = "n" },
        new LSystemRule { key = "n", value = "a" },
        new LSystemRule { key = "a", value = "d" },
        new LSystemRule { key = "d", value = "" },
        new LSystemRule { key = "l", value = "tl" },
        new LSystemRule { key = "T", value = "T" },
        new LSystemRule { key = "C", value = "O" },
        new LSystemRule { key = "O", value = "tO" },
        new LSystemRule { key = "I", value = "I" },
        new LSystemRule { key = "D", value = "D" },
        new LSystemRule { key = "dlC", value = "C" },
    };

    /// <summary>
    /// Applies L-System rules to a recipe string (string-to-string replacement).
    /// </summary>
    private static string ApplyLSystem(string recipe)
    {
        // Build a list of rules sorted by descending key length
        var sortedRules = new List<KeyValuePair<string, string>>(rules.ToDictionary(r => r.key, r => r.value));
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

    // Use this for runtime lookups
    public static Dictionary<string, string> GetRuleDictionary()
    {
        return rules.ToDictionary(r => r.key, r => r.value);
    }
}
