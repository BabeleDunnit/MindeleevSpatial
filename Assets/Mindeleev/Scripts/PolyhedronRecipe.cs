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
    public List<object> PositionalParameters { get; set; } = new List<object>();
    public Dictionary<string, object> NamedParameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    // Operator parameter mapping: operator -> (positional index, named key)
    public static readonly Dictionary<string, (int index, string key)[]> OperatorParamMap = new Dictionary<string, (int, string)[]>
    {
        // For each operator, define the mapping of positional index to named key
        // Order: faceSignatureRounding, facesSidesFilter, centerVertexHeight/insetHeight, extrudeHeight
        { "t", new[] { (0, "faceSignatureRounding"), (1, "facesSidesFilter"), (2, "centerVertexHeight") } },
        { "k", new[] { (0, "faceSignatureRounding"), (1, "facesSidesFilter"), (2, "centerVertexHeight") } },
        { "n", new[] { (0, "faceSignatureRounding"), (1, "facesSidesFilter"), (2, "insetHeight"), (3, "extrudeHeight") } },
        { "a", new[] { (0, "faceSignatureRounding") } },
        { "d", new[] { (0, "faceSignatureRounding") } },
        { "l", new[] { (0, "faceSignatureRounding") } },
    };

    // Default values for named parameters
    public static readonly Dictionary<string, object> NamedDefaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
        { "faceSignatureRounding", 1 },
        { "facesSidesFilter", 0 },
        { "centerVertexHeight", 0.1f },
        { "insetHeight", 0.6f },
        { "extrudeHeight", -0.3f }
    };

    public RecipeToken(string op)
    {
        Operator = op;
    }

    /// <summary>
    /// Get the value of a named parameter, considering overrides and defaults.
    /// </summary>
    public object Parameter(string name)
    {
        // 1. If present as named parameter, return it
        if (NamedParameters.TryGetValue(name, out var val))
            return val;

        // 2. If mapped to a positional parameter, return that if present
        if (OperatorParamMap.TryGetValue(Operator, out var map))
        {
            for (int i = 0; i < map.Length; i++)
            {
                if (string.Equals(map[i].key, name, StringComparison.OrdinalIgnoreCase))
                {
                    if (i < PositionalParameters.Count)
                        return PositionalParameters[i];
                }
            }
        }

        // 3. Otherwise, return default
        if (NamedDefaults.TryGetValue(name, out var def))
            return def;

        // 4. Not found
        throw new ArgumentException($"Parameter '{name}' not found for operator '{Operator}'.");
    }

    public override string ToString()
    {
        var paramList = new List<string>();
        // Add positional parameters
        paramList.AddRange(PositionalParameters.Select(p => FormatParam(p)));
        // Add named parameters (not already present as positional)
        foreach (var kv in NamedParameters)
        {
            paramList.Add($"{kv.Key}:{FormatParam(kv.Value)}");
        }
        if (paramList.Count == 0)
            return Operator;
        return $"{Operator}({string.Join(",", paramList)})";
    }

    private string FormatParam(object p)
    {
        if (p is float f)
            return f.ToString("0.###", CultureInfo.InvariantCulture);
        return p.ToString();
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

    /*
    // Default parameter values for each operator and parameter index
    private static readonly Dictionary<string, object[]> OperatorDefaultParameters = new Dictionary<string, object[]>
    {
        // first parameter is faceSignatureRounding for all
        // second parameter is facesSidesFilter for the operators which support that
        // other parameters depend from the operator
        { "t", new object[] { 1, 0, 0.1f } }, // truncate: (int, int, float)
        { "k", new object[] { 1, 0, 0.1f } }, // kis: (int, int, float)
        { "a", new object[] { 1 } },           // ambo: (int)
        { "d", new object[] { 1 } },           // dual: (int)
        { "n", new object[] { 1, 0, 0.6f, -0.3f } }, // n: (int, int, float, float)
        { "l", new object[] { 1 } },           // l: (int)
        // Add more operators and their default parameters as needed
    };
    */

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

            // Parse parameters (mixed positional and named)
            if (!string.IsNullOrEmpty(paramStr))
            {
                var paramParts = SplitParams(paramStr);
                foreach (var part in paramParts)
                {
                    var kv = KeyValueRegex.Match(part);
                    if (kv.Success)
                    {
                        string key = kv.Groups[1].Value;
                        object value = ParseParameter(kv.Groups[2].Value);

                        // Only treat as named parameter if key is in the known set
                        if (RecipeToken.NamedDefaults.ContainsKey(key))
                        {
                            token.NamedParameters[key] = value;
                        }
                        else
                        {
                            // Otherwise, treat as positional KeyValuePair
                            token.PositionalParameters.Add(new KeyValuePair<string, object>(key, value));
                        }
                    }
                    else
                    {
                        // Positional parameter
                        token.PositionalParameters.Add(ParseParameter(part));
                    }
                }
            }

            // Override positional parameters with named ones if present
            if (RecipeToken.OperatorParamMap.TryGetValue(op, out var map))
            {
                for (int i = 0; i < map.Length; i++)
                {
                    string key = map[i].key;
                    if (token.NamedParameters.ContainsKey(key))
                    {
                        // Override positional value with named value
                        if (i < token.PositionalParameters.Count)
                            token.PositionalParameters[i] = token.NamedParameters[key];
                        else
                        {
                            // Fill missing positional slots up to i
                            while (token.PositionalParameters.Count < i)
                                token.PositionalParameters.Add(RecipeToken.NamedDefaults[key]);
                            token.PositionalParameters.Add(token.NamedParameters[key]);
                        }
                    }
                }
                // Fill missing positional parameters with defaults
                for (int i = token.PositionalParameters.Count; i < map.Length; i++)
                {
                    string key = map[i].key;
                    token.PositionalParameters.Add(RecipeToken.NamedDefaults[key]);
                }
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
    /// Parses a single parameter: tries int, float, or string.
    /// </summary>
    private static object ParseParameter(string param)
    {
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