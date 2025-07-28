using UnityEngine;
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

    /// <summary>
    /// Computes a complexity score for a recipe.
    /// 0 = trivial (just base polyhedron), higher = more complex.
    /// </summary>
    public static float ComputeComplexity(PolyhedronRecipe recipe)
    {
        // Operator weights (tweak as desired)
        var opWeights = new Dictionary<string, float>
        {
            { "k", 1.2f }, // kis
            { "t", 1.5f }, // truncate
            { "a", 1.1f }, // ambo
            { "d", 1.0f }, // dual
            { "n", 1.7f }, // insetN
            { "l", 2.0f }, // stellation
            { "f", 2.5f }, // "fucked" stellation
        };

        // 1. Trivial recipe: only base polyhedron
        if (recipe.Tokens.Count == 0)
            return 0f;

        // 2. Operator complexity
        float opComplexity = 0f;
        foreach (var token in recipe.Tokens)
        {
            float weight = opWeights.TryGetValue(token.Operator, out var w) ? w : 1.0f;
            opComplexity += weight;
        }

        // 3. Length penalty/bonus (longer recipes are more complex)
        float lengthComplexity = 0.2f * recipe.Tokens.Count * recipe.Tokens.Count;

        // 4. Parameter complexity (more/less extreme parameters = more complex)
        float paramComplexity = 0f;
        foreach (var token in recipe.Tokens)
        {
            foreach (var p in token.PositionalParameters)
            {
                if (p is float f)
                    paramComplexity += Mathf.Abs(f) * 0.2f;
                else if (p is int i)
                    paramComplexity += Mathf.Abs(i) * 0.1f;
            }
            foreach (var kv in token.NamedParameters)
            {
                if (kv.Value is float f)
                    paramComplexity += Mathf.Abs(f) * 0.2f;
                else if (kv.Value is int i)
                    paramComplexity += Mathf.Abs(i) * 0.1f;
            }
        }

        // 5. Visual complexity (faces, vertices, colors)
        float visualComplexity = 0f;
        try
        {
            var tuple = PolyhedronRecipeBuilder.Build(recipe);
            int v = tuple.Item1.Length;
            int f = tuple.Item2.Length;
            int c = tuple.Item3.Distinct().Count();

            // Weight: faces and vertices contribute more, color variety a bit less
            visualComplexity = 0.03f * v + 0.05f * f + 0.01f * c;
        }
        catch
        {
            // If build fails, ignore visual complexity
        }

        // Final weighted sum (tweak as desired)
        float total = opComplexity + lengthComplexity + paramComplexity + visualComplexity;
        return total;
    }

    /*
        public static HashSet<string> AllPermutationsWithRepetition(HashSet<char> chars, int maxLength)
        {
            var result = new HashSet<string> { "" }; // include the empty string for length 0

            if (maxLength <= 0 || chars == null || chars.Count == 0)
                return result;

            var charArray = chars.ToArray();

            for (int length = 1; length <= maxLength; length++)
            {
                var prev = result.Where(s => s.Length == length - 1).ToList();
                foreach (var s in prev)
                {
                    foreach (var c in charArray)
                    {
                        result.Add(s + c);
                    }
                }
            }

            return result;
        }

        */
    
    public static List<string> AllPermutationsWithRepetition(HashSet<char> chars, int maxLength)
    {
        var result = new List<string> { "" }; // include the empty string for length 0

        if (maxLength <= 0 || chars == null || chars.Count == 0)
            return result;

        var charArray = chars.ToArray();

        for (int length = 1; length <= maxLength; length++)
        {
            var prev = result.Where(s => s.Length == length - 1).ToList();
            foreach (var s in prev)
            {
                foreach (var c in charArray)
                {
                    result.Add(s + c);
                }
            }
        }

        return result;
    }

}