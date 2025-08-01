using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Implements pseudo-algebraic operations on polyhedron recipes.
/// </summary>
public static class PolyhedronRecipeAlgebra
{
    // Operators in order of increasing complexity
    private static readonly char[] Operators = { 'd', 'a', 'k', 't', 'n', 'l' };
    // Base polyhedra in order of increasing complexity
    private static readonly char[] BasePolyhedra = { 'T', 'C', 'O', 'D', 'I' };

    // Explicit operator sum table (36 rules)
    public static readonly Dictionary<(char, char), char> OperatorSumTable = new Dictionary<(char, char), char>
    {
        { ('d','d'), 'd' }, { ('d','a'), 'a' }, { ('d','k'), 'k' }, { ('d','t'), 't' }, { ('d','n'), 'n' }, { ('d','l'), 'l' },
        { ('a','d'), 'a' }, { ('a','a'), 'a' }, { ('a','k'), 'k' }, { ('a','t'), 't' }, { ('a','n'), 'n' }, { ('a','l'), 'l' },
        { ('k','d'), 'k' }, { ('k','a'), 'k' }, { ('k','k'), 'k' }, { ('k','t'), 't' }, { ('k','n'), 'n' }, { ('k','l'), 'l' },
        { ('t','d'), 't' }, { ('t','a'), 't' }, { ('t','k'), 't' }, { ('t','t'), 't' }, { ('t','n'), 'n' }, { ('t','l'), 'l' },
        { ('n','d'), 'n' }, { ('n','a'), 'n' }, { ('n','k'), 'n' }, { ('n','t'), 'n' }, { ('n','n'), 'n' }, { ('n','l'), 'l' },
        { ('l','d'), 'l' }, { ('l','a'), 'l' }, { ('l','k'), 'l' }, { ('l','t'), 'l' }, { ('l','n'), 'l' }, { ('l','l'), 'l' },
    };

    // Explicit base polyhedron sum table (25 rules)
    public static readonly Dictionary<(char, char), char> BasePolySumTable = new Dictionary<(char, char), char>
    {
        { ('T','T'), 'T' }, { ('T','C'), 'C' }, { ('T','O'), 'O' }, { ('T','D'), 'D' }, { ('T','I'), 'I' },
        { ('C','T'), 'C' }, { ('C','C'), 'C' }, { ('C','O'), 'O' }, { ('C','D'), 'D' }, { ('C','I'), 'I' },
        { ('O','T'), 'O' }, { ('O','C'), 'O' }, { ('O','O'), 'O' }, { ('O','D'), 'D' }, { ('O','I'), 'I' },
        { ('D','T'), 'D' }, { ('D','C'), 'D' }, { ('D','O'), 'D' }, { ('D','D'), 'D' }, { ('D','I'), 'I' },
        { ('I','T'), 'I' }, { ('I','C'), 'I' }, { ('I','O'), 'I' }, { ('I','D'), 'I' }, { ('I','I'), 'I' },
    };

    /// <summary>
    /// Sums a list of polyhedron recipes (strings).
    /// Operators and base polyhedra are summed column-wise using explicit tables.
    /// </summary>
    public static string SumRecipes(List<string> recipes)
    {
        if (recipes == null || recipes.Count == 0)
            return "";

        // Parse operators and base polyhedra
        List<string> opsList = new List<string>();
        List<char> baseList = new List<char>();

        foreach (var recipe in recipes)
        {
            if (string.IsNullOrEmpty(recipe)) continue;
            int baseIdx = recipe.Length - 1;
            while (baseIdx >= 0 && !char.IsUpper(recipe[baseIdx]))
                baseIdx--;
            if (baseIdx < 0) continue;
            opsList.Add(recipe.Substring(0, baseIdx));
            baseList.Add(recipe[baseIdx]);
        }

        // Sum base polyhedra sequentially
        char baseResult = baseList[0];
        for (int i = 1; i < baseList.Count; i++)
        {
            baseResult = SumBasePoly(baseResult, baseList[i]);
        }

        // Pad operator strings to same length (left pad with spaces)
        int maxOpsLen = opsList.Max(s => s.Length);
        var paddedOps = opsList.Select(s => s.PadLeft(maxOpsLen, ' ')).ToList();

        // Sum operators column-wise, right to left
        string opResult = "";
        for (int col = maxOpsLen - 1; col >= 0; col--)
        {
            char sum = paddedOps[0][col];
            for (int r = 1; r < paddedOps.Count; r++)
            {
                sum = SumOperator(sum, paddedOps[r][col]);
            }
            if (sum != ' ') // skip padding
                opResult = sum + opResult;
        }

        return opResult + baseResult;
    }

    /// <summary>
    /// Sums two operators using the explicit table.
    /// </summary>
    public static char SumOperator(char opA, char opB)
    {
        if (opA == ' ' && opB == ' ') return ' '; // both empty
        if (opA == ' ') return opB;
        if (opB == ' ') return opA;
        if (OperatorSumTable.TryGetValue((opA, opB), out var result))
            return result;
        return opA; // fallback
    }

    /// <summary>
    /// Sums two base polyhedra using the explicit table.
    /// </summary>
    public static char SumBasePoly(char baseA, char baseB)
    {
        if (BasePolySumTable.TryGetValue((baseA, baseB), out var result))
            return result;
        return baseA; // fallback
    }


    // Explicit 1-to-many split rules for base polyhedra
    public static readonly Dictionary<char, List<char>> BasePolySplitTable = new Dictionary<char, List<char>>
    {
        { 'T', new List<char> { 'T', 'T', 'T' } },
        { 'C', new List<char> { 'T', 'T' } },
        { 'O', new List<char> { 'C', 'C' } },
        { 'D', new List<char> { 'O','O' } },
        { 'I', new List<char> { 'D','D' } },
    };

    // Explicit 1-to-many split rules for operators
    public static readonly Dictionary<char, List<char>> OperatorSplitTable = new Dictionary<char, List<char>>
    {
        { 'd', new List<char> { 'd', 'd', 'd' } },
        { 'a', new List<char> { 'd', 'd' } },
        { 'k', new List<char> { 'a', 'a' } },
        { 't', new List<char> { 'k', 'k' } },
        { 'n', new List<char> { 't', 't' } },
        { 'l', new List<char> { 'n','n' } },
    };

    /// <summary>
    /// Splits a recipe string into several recipes according to explicit 1-to-many rules.
    /// </summary>
    public static List<string> SplitRecipe(string recipe)
    {
        if (string.IsNullOrEmpty(recipe))
            return new List<string>();

        // Find base polyhedron (last uppercase letter)
        int baseIdx = recipe.Length - 1;
        while (baseIdx >= 0 && !char.IsUpper(recipe[baseIdx]))
            baseIdx--;
        if (baseIdx < 0)
            return new List<string>();

        string ops = recipe.Substring(0, baseIdx);
        char basePoly = recipe[baseIdx];

        // Get base polyhedron split
        var baseSplits = BasePolySplitTable.ContainsKey(basePoly)
            ? BasePolySplitTable[basePoly]
            : new List<char> { basePoly };

        int splitCount = baseSplits.Count;

        // Prepare output recipes
        var output = Enumerable.Repeat("", splitCount).ToList();

        // Process operators from right to left
        for (int opIdx = ops.Length - 1; opIdx >= 0; opIdx--)
        {
            char op = ops[opIdx];
            var opSplits = OperatorSplitTable.ContainsKey(op)
                ? OperatorSplitTable[op]
                : new List<char> { op };

            // Pad opSplits to splitCount with '\0'
            var paddedOpSplits = new List<char>(opSplits);
            while (paddedOpSplits.Count < splitCount)
                paddedOpSplits.Add('\0');

            // Prepend operator to each recipe
            for (int i = 0; i < splitCount; i++)
            {
                if (paddedOpSplits[i] != '\0')
                    output[i] = paddedOpSplits[i] + output[i];
            }
        }

        // Add base polyhedron to each recipe
        for (int i = 0; i < splitCount; i++)
        {
            output[i] += baseSplits[i];
        }

        return output;
    }
}
