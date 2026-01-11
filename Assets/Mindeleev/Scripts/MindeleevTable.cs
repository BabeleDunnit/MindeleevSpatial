using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A small value object responsible for holding the set of unique emanations
/// (recipe strings) collected for a Polytron. Implemented as a distinct class
/// because the MindeleevTable will evolve into a richer structure later.
/// </summary>
[Serializable]
public class MindeleevTable
{
    // Backing map to ensure uniqueness of recorded emanations and store their
    // corresponding polytronicNumber (operators-sequence -> int)
    private Dictionary<string, int> emanations = new Dictionary<string, int>();
    
    public MindeleevTable()
    {
    }

    public bool AddEmanation(string recipe)
    {
        if (string.IsNullOrEmpty(recipe)) return false;
        if (emanations.ContainsKey(recipe))
        {
            Debug.Log($"[MindeleevTable] emanation already present='{recipe}'");
            return false;
        }

        int polytronicNumber = PolyhedronRecipeKabbalah.RecipeToInt(recipe, false);
        emanations[recipe] = polytronicNumber;
        Debug.Log($"[MindeleevTable] recorded new emanation='{recipe}' polytronicNumber={polytronicNumber} (total={emanations.Count})");
        return true;
    }

    public bool Contains(string recipe) => !string.IsNullOrEmpty(recipe) && emanations.ContainsKey(recipe);

    public IReadOnlyCollection<string> GetEmanations() => emanations.Keys;

    public int Count => emanations.Count;

    /// <summary>
    /// Return a deterministic list of emanations suitable for cycling. We use
    /// lexical order to provide a predictable ordering for UI cycling.
    /// </summary>
    public List<string> GetEmanationsList()
    {
        var list = emanations.Keys.ToList();
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    /// <summary>
    /// Return the polytronicNumber associated with a recorded recipe, or null
    /// if the recipe is not present.
    /// </summary>
    public int? GetPolytronicNumber(string recipe)
    {
        if (string.IsNullOrEmpty(recipe)) return null;
        if (emanations.TryGetValue(recipe, out var n)) return n;
        return null;
    }

    /// <summary>
    /// Return a list of polytronicNumbers ordered to match GetEmanationsList().
    /// </summary>
    public List<int> GetPolytronicNumbersList()
    {
        var recipes = GetEmanationsList();
        var nums = new List<int>(recipes.Count);
        foreach (var r in recipes)
        {
            nums.Add(emanations[r]);
        }
        return nums;
    }

    /// <summary>
    /// Return the smallest stored polytronicNumber strictly greater than <paramref name="current"/>,
    /// or null if none exists.
    /// </summary>
    public int? NextPolytronicNumberGreaterThan(int current)
    {
        var nums = GetPolytronicNumbersList();
        var greater = nums.Where(n => n > current).OrderBy(n => n).ToList();
        return greater.Count > 0 ? (int?)greater[0] : null;
    }

    /// <summary>
    /// Return the smallest non-negative polytronicNumber that is not present in the
    /// table (starting search from 0). This is useful to determine the "next"
    /// seed to collect independent of recipe lexicographic ordering.
    /// </summary>
    public int NextMissingPolytronicNumber()
    {
        // Collect unique numbers and sort
        var nums = emanations.Values.Distinct().ToList();
        nums.Sort();

        int expected = 0;
        foreach (var n in nums)
        {
            if (n < expected) continue; // duplicates or smaller values
            if (n == expected) { expected++; continue; }
            // n > expected -> gap found
            break;
        }
        return expected;
    }

    public override string ToString()
    {
        return $"MindeleevTable(count={Count})";
    }
}
