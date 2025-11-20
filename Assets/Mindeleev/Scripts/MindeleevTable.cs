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
    // Backing set to ensure uniqueness of recorded emanations.
    private HashSet<string> emanations = new HashSet<string>();

    public MindeleevTable()
    {
    }

    public bool AddEmanation(string recipe)
    {
        if (string.IsNullOrEmpty(recipe)) return false;
        bool added = emanations.Add(recipe);
        if (added)
        {
            Debug.Log($"[MindeleevTable] recorded new emanation='{recipe}' (total={emanations.Count})");
        }
        else
        {
            Debug.Log($"[MindeleevTable] emanation already present='{recipe}'");
        }
        return added;
    }

    public bool Contains(string recipe) => !string.IsNullOrEmpty(recipe) && emanations.Contains(recipe);

    public IReadOnlyCollection<string> GetEmanations() => emanations;

    public int Count => emanations.Count;

    /// <summary>
    /// Return a deterministic list of emanations suitable for cycling. We use
    /// lexical order to provide a predictable ordering for UI cycling.
    /// </summary>
    public List<string> GetEmanationsList()
    {
        var list = emanations.ToList();
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    public override string ToString()
    {
        return $"MindeleevTable(count={Count})";
    }
}
