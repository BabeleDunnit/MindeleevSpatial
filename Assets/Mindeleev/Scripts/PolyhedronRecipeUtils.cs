using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Utility functions for polyhedron recipes.
/// </summary>
public static class PolyhedronRecipeUtils
{
    /// <summary>
    /// Given a string recipe, parses and tokenizes it, and returns a list of PolyhedronRecipe
    /// with all possible permutations of the operator tokens (base polyhedron is kept fixed).
    /// </summary>
    public static List<PolyhedronRecipe> AllTokenPermutations(string recipe)
    {
        var parsed = PolyhedronRecipeParser.Parse(recipe);
        var tokens = parsed.Tokens;

        var permutations = GetPermutations(tokens, tokens.Count);

        var result = new List<PolyhedronRecipe>();
        foreach (var perm in permutations)
        {
            result.Add(new PolyhedronRecipe
            {
                Tokens = perm.ToList(),
                BasePolyhedron = parsed.BasePolyhedron
            });
        }
        return result;
    }

    /// <summary>
    /// Helper: Get all permutations of a list.
    /// </summary>
    private static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
    {
        if (length == 1)
            return list.Select(t => new T[] { t });

        return list.SelectMany((t, i) =>
            GetPermutations(list.Take(i).Concat(list.Skip(i + 1)), length - 1)
            .Select(p => (new T[] { t }).Concat(p)));
    }
}