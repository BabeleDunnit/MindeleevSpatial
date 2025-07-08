using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Represents a single operator token in a polyhedron recipe.
/// </summary>
public class RecipeToken
{
    public string Operator { get; set; }
    public List<object> Parameters { get; set; } = new List<object>();

    public RecipeToken(string op)
    {
        Operator = op;
    }

    public override string ToString()
    {
        if (Parameters.Count == 0)
            return Operator;
        return $"{Operator}({string.Join(",", Parameters)})";
    }
}

/// <summary>
/// Represents a parsed polyhedron recipe: a sequence of tokens and the base polyhedron.
/// </summary>
public class PolyhedronRecipe
{
    public List<RecipeToken> Tokens { get; set; } = new List<RecipeToken>();
    public char BasePolyhedron { get; set; }

    public override string ToString()
    {
        return string.Concat(Tokens.Select(t => t.ToString())) + BasePolyhedron;
    }
}

/// <summary>
/// Utility for parsing and tokenizing polyhedron recipes.
/// </summary>
public static class PolyhedronRecipeParser
{
    // Default parameter values for each operator and parameter index
    // Example: { "t", new object[] { 0, 0, -0.3f } }
    private static readonly Dictionary<string, object[]> OperatorDefaultParameters = new Dictionary<string, object[]>
    {
        { "t", new object[] { 0, 0, -0.3f } }, // truncate: (int, int, float)
        { "k", new object[] { 0, 0, -0.5f } }, // kis: (int, int, float)
        { "a", new object[] { 0 } },           // ambo: (int)
        { "d", new object[] { 0 } },           // dual: (int)
        { "n", new object[] { 0, 0, -0.5f, -0.5f } }, // n: (int, int, float, float)
        { "l", new object[] { 0 } },           // l: (int)
        // Add more operators and their default parameters as needed
    };

    // Regex for parsing tokens: operator + optional (params)
    private static readonly Regex TokenRegex = new Regex(
        @"([a-z])(?:\(([^)]*)\))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Regex for parsing key:value pairs
    private static readonly Regex KeyValueRegex = new Regex(
        @"^\s*(\w+)\s*:\s*(.+)\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Parse a recipe string into a PolyhedronRecipe object.
    /// </summary>
    public static PolyhedronRecipe Parse(string recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe))
            throw new ArgumentException("Recipe string is empty.");

        // Find the base polyhedron (last uppercase letter)
        int basePos = recipe.Length - 1;
        while (basePos >= 0 && !char.IsUpper(recipe[basePos]))
            basePos--;
        if (basePos < 0)
            throw new ArgumentException("No base polyhedron found in recipe.");

        char basePoly = recipe[basePos];
        string opsPart = recipe.Substring(0, basePos);

        var tokens = new List<RecipeToken>();

        foreach (Match match in TokenRegex.Matches(opsPart))
        {
            string op = match.Groups[1].Value;
            string paramStr = match.Groups[2].Success ? match.Groups[2].Value : null;

            var token = new RecipeToken(op);

            // Get default parameters for this operator
            object[] defaults = OperatorDefaultParameters.ContainsKey(op)
                ? OperatorDefaultParameters[op]
                : Array.Empty<object>();

            // Parse parameters
            if (!string.IsNullOrEmpty(paramStr))
            {
                var paramParts = SplitParams(paramStr);
                for (int i = 0; i < paramParts.Count; i++)
                {
                    object parsed = ParseParameter(paramParts[i]);
                    token.Parameters.Add(parsed);
                }
            }

            // Fill in missing parameters with defaults
            for (int i = token.Parameters.Count; i < defaults.Length; i++)
            {
                token.Parameters.Add(defaults[i]);
            }

            tokens.Add(token);
        }

        return new PolyhedronRecipe
        {
            Tokens = tokens,
            BasePolyhedron = basePoly
        };
    }

    /// <summary>
    /// Splits a parameter string into parts, handling commas inside quotes.
    /// </summary>
    private static List<string> SplitParams(string paramStr)
    {
        var result = new List<string>();
        int start = 0;
        int depth = 0;
        for (int i = 0; i < paramStr.Length; i++)
        {
            if (paramStr[i] == '(') depth++;
            if (paramStr[i] == ')') depth--;
            if (paramStr[i] == ',' && depth == 0)
            {
                result.Add(paramStr.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }
        if (start < paramStr.Length)
            result.Add(paramStr.Substring(start).Trim());
        return result;
    }

    /// <summary>
    /// Parses a single parameter: tries int, float, key:value, or string.
    /// </summary>
    private static object ParseParameter(string param)
    {
        // Try key:value
        var kv = KeyValueRegex.Match(param);
        if (kv.Success)
            return new KeyValuePair<string, object>(kv.Groups[1].Value, ParseParameter(kv.Groups[2].Value));

        // Try int
        if (int.TryParse(param, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iVal))
            return iVal;

        // Try float
        if (float.TryParse(param, NumberStyles.Float, CultureInfo.InvariantCulture, out float fVal))
            return fVal;

        // Otherwise, treat as string
        return param;
    }
}