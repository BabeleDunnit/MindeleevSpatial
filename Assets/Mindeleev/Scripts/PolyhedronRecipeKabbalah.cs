using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Implements kabbalah-like symbolic algebra for polyhedron recipes.
/// </summary>
public static class PolyhedronRecipeKabbalah
{
    // Base polyhedra and their kabbalah values (T=1, C=2, O=3, D=4, I=5)
    public static readonly char[] BasePolyhedra = { 'T', 'C', 'O', 'D', 'I' };
    public static readonly Dictionary<char, int> BasePolyToValue = new Dictionary<char, int>
    {
        { 'T', 1 }, { 'C', 2 }, { 'O', 3 }, { 'D', 4 }, { 'I', 5 }
    };
    public static readonly Dictionary<int, char> ValueToBasePoly = new Dictionary<int, char>
    {
        { 1, 'T' }, { 2, 'C' }, { 3, 'O' }, { 4, 'D' }, { 5, 'I' }
    };

    // Conway operators and their kabbalah values (d=0, a=1, k=2, t=3, n=4, l=5)
    public static readonly char[] Operators = { 'd', 'a', 'k', 't', 'n', 'l' };
    public static readonly Dictionary<char, int> OperatorToValue = new Dictionary<char, int>
    {
        { 'd', 0 }, { 'a', 1 }, { 'k', 2 }, { 't', 3 }, { 'n', 4 }, { 'l', 5 }
    };
    public static readonly Dictionary<int, char> ValueToOperator = new Dictionary<int, char>
    {
        { 0, 'd' }, { 1, 'a' }, { 2, 'k' }, { 3, 't' }, { 4, 'n' }, { 5, 'l' }
    };

    /// <summary>
    /// Fuses a list of recipes using kabbalah-like mean for base polyhedra and base-6 symbolic sum for operators.
    /// </summary>
    public static string RecipeFusion(List<string> recipes)
    {
        if (recipes == null || recipes.Count == 0)
            return "";

        // Parse base polyhedra and operator sequences
        List<int> baseValues = new List<int>();
        List<string> opsList = new List<string>();

        foreach (var recipe in recipes)
        {
            if (string.IsNullOrEmpty(recipe)) continue;
            int baseIdx = recipe.Length - 1;
            while (baseIdx >= 0 && !char.IsUpper(recipe[baseIdx]))
                baseIdx--;
            if (baseIdx < 0) continue;
            char basePoly = recipe[baseIdx];
            if (BasePolyToValue.TryGetValue(basePoly, out int val))
                baseValues.Add(val);
            opsList.Add(recipe.Substring(0, baseIdx));
        }

        // 1. Base polyhedron: mean and round down
        int meanBase = (int)Math.Floor(baseValues.Average());
        meanBase = Math.Max(1, Math.Min(5, meanBase));
        char fusedBase = ValueToBasePoly[meanBase];

        // 2. Operators: symbolic base-6 sum with overflow, right to left
        int maxOpsLen = opsList.Any() ? opsList.Max(s => s.Length) : 0;
        var paddedOps = opsList.Select(s => s.PadLeft(maxOpsLen, 'd')).ToList();

        int carry = 0;
        char[] resultOps = new char[maxOpsLen + 10]; // +10 for possible extra carry columns
        int resultLen = 0;

        for (int col = maxOpsLen - 1; col >= 0; col--)
        {
            int sum = carry;
            foreach (var ops in paddedOps)
            {
                sum += OperatorToValue[ops[col]];
            }
            carry = sum / 6;
            int digit = sum % 6;
            resultOps[resultLen++] = ValueToOperator[digit];
        }
        // Handle any remaining carry
        while (carry > 0)
        {
            int digit = carry % 6;
            resultOps[resultLen++] = ValueToOperator[digit];
            carry /= 6;
        }

        // Build operator string (reverse, since we built right-to-left)
        string fusedOps = new string(new string(resultOps, 0, resultLen).Reverse().ToArray());

        return fusedOps + fusedBase;
    }

    public static int RecipeToInt(string recipe, bool keepBasePolyhedron)
    {
        if (string.IsNullOrEmpty(recipe)) return -1;

        int baseIdx = recipe.Length - 1;
        if (keepBasePolyhedron)
        {
            while (baseIdx >= 0 && !char.IsUpper(recipe[baseIdx]))
                baseIdx--;
            if (baseIdx < 0) return 0;
        }
        else
        {
            baseIdx = recipe.Length; // treat whole string as operators
        }

        int value = 0;
        int pow = 1;
        // Operators: right to left (least significant digit)
        for (int i = baseIdx - 1; i >= 0; i--)
        {
            char op = recipe[i];
            if (OperatorToValue.TryGetValue(op, out int v))
            {
                value += v * pow;
                pow *= 6;
            }
        }

        if (keepBasePolyhedron && baseIdx < recipe.Length)
        {
            char basePoly = recipe[baseIdx];
            if (BasePolyToValue.TryGetValue(basePoly, out int baseVal))
            {
                value += baseVal;
            }
        }

        return value;
    }

    public static string IntToRecipe(int value)
    {
        if (value < 0) return "";

        // Convert to base-6, right to left, using operator symbols
        List<char> ops = new List<char>();
        int v = value;
        do
        {
            int digit = v % 6;
            ops.Add(ValueToOperator[digit]);
            v /= 6;
        } while (v > 0);

        // Build recipe string (operators left to right, no base polyhedron)
        ops.Reverse();
        return new string(ops.ToArray());
    }
}