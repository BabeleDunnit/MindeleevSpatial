using UnityEngine;
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
        // Special case for color remap operator
        if (Operator == "c" && NamedParameters.TryGetValue("colorRemap", out var remapObj) && remapObj is Dictionary<int, int> remapDict)
        {
            var pairs = remapDict.Select(kv => $"{kv.Key}:{kv.Value}");
            return $"{Operator}({string.Join(",", pairs)})";
        }

        var paramList = new List<string>();
        // Add positional parameters
        paramList.AddRange(PositionalParameters.Select(p => FormatParam(p)));
        // Add named parameters (not already present as positional)
        foreach (var kv in NamedParameters)
        {
            // Skip colorRemap for c operator, already handled above
            if (Operator == "c" && kv.Key == "colorRemap") continue;
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
        if (p is Dictionary<int, int> dict)
            return string.Join(",", dict.Select(kv => $"{kv.Key}:{kv.Value}"));
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
    public int PaletteIdx { get; set; } = 0; // NEW: Palette index, default 0

    public override string ToString()
    {
        string ops = string.Concat(Tokens.Select(t => t.ToString()));
        string paletteStr = PaletteIdx.ToString("D2");
        return $"{ops}{paletteStr}{BasePolyhedron}";
    }
}

/// <summary>
/// Utility for parsing and tokenizing polyhedron recipes.
/// </summary>
public static class PolyhedronRecipeParser
{
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
        string opsAndPalette = recipe.Substring(0, basePos);

        // NEW: Palette index parsing (last two digits before basePolyhedron)
        int paletteIdx = 0;
        string opsPart = opsAndPalette;
        if (opsAndPalette.Length >= 2 &&
            char.IsDigit(opsAndPalette[opsAndPalette.Length - 2]) &&
            char.IsDigit(opsAndPalette[opsAndPalette.Length - 1]))
        {
            paletteIdx = int.Parse(opsAndPalette.Substring(opsAndPalette.Length - 2, 2));
            opsPart = opsAndPalette.Substring(0, opsAndPalette.Length - 2);
        }

        var tokens = new List<RecipeToken>();
        foreach (Match match in TokenRegex.Matches(opsPart))
        {
            string op = match.Groups[1].Value;
            string paramStr = match.Groups[2].Success ? match.Groups[2].Value : null;

            var token = new RecipeToken(op);

            // Special handling for color remap operator
            if (op == "c" && !string.IsNullOrEmpty(paramStr))
            {
                var paramParts = SplitParams(paramStr);
                var colorRemap = new Dictionary<int, int>();
                foreach (var part in paramParts)
                {
                    var kv = KeyValueRegex.Match(part);
                    if (kv.Success &&
                        int.TryParse(kv.Groups[1].Value, out int from) &&
                        int.TryParse(kv.Groups[2].Value, out int to))
                    {
                        colorRemap[from] = to;
                    }
                }
                token.NamedParameters["colorRemap"] = colorRemap;
            }
            else if (!string.IsNullOrEmpty(paramStr))
            {
                // Parse parameters (mixed positional and named)
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
            BasePolyhedron = basePoly,
            PaletteIdx = paletteIdx
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

/// <summary>
/// Builds a polyhedron mesh tuple from a PolyhedronRecipe, mimicking Polyhedronisme.ParsePolyhedronRecipe logic.
/// </summary>
public static class PolyhedronRecipeBuilder
{
    public static (Vector3[], int[][], int[]) Build(PolyhedronRecipe recipe, int paletteColorsCount = 6)
    {
        // Get base polyhedron
        (Vector3[], int[][], int[]) current = recipe.BasePolyhedron switch
        {
            'C' => Polyhedronisme.Cube,
            'T' => Polyhedronisme.Tetrahedron,
            'O' => Polyhedronisme.Octahedron,
            'D' => Polyhedronisme.Dodecahedron,
            'I' => Polyhedronisme.Icosahedron,
            _ => Polyhedronisme.Cube
        };

        // Apply operators from right to left (last token is applied first)
        for (int t = recipe.Tokens.Count - 1; t >= 0; t--)
        {
            var token = recipe.Tokens[t];
            string op = token.Operator;

            // Use named parameter access for clarity
            int faceSignatureRounding = Convert.ToInt32(token.Parameter("faceSignatureRounding"));
            int facesSidesFilter = Convert.ToInt32(token.Parameter("facesSidesFilter"));

            // introduce an upper bound complexity control
            if (current.Item1.Length > 500)
            {
                Debug.LogWarning($"Polyhedron complexity upper bound hit, stopping generation - recipe: {recipe.ToString()}, vertices: {current.Item1.Length}");
                break;
            }

            switch (op)
            {
                case "k":
                    current = Polyhedronisme.ApplyKis(
                        current,
                        facesSidesFilter,
                        faceSignatureRounding,
                        Convert.ToSingle(token.Parameter("centerVertexHeight"))
                    );
                    break;
                case "t":
                    // Truncate: d -> k -> d
                    current = Polyhedronisme.ApplyDual(current, faceSignatureRounding);
                    current = Polyhedronisme.ApplyKis(
                        current,
                        facesSidesFilter,
                        faceSignatureRounding,
                        Convert.ToSingle(token.Parameter("centerVertexHeight"))
                    );
                    current = Polyhedronisme.ApplyDual(current, faceSignatureRounding);
                    break;
                case "a":
                    current = Polyhedronisme.ApplyAmbo(current, faceSignatureRounding);
                    break;
                case "d":
                    current = Polyhedronisme.ApplyDual(current, faceSignatureRounding);
                    break;
                case "n":
                    current = Polyhedronisme.ApplyInsetN(
                        current,
                        facesSidesFilter,
                        faceSignatureRounding,
                        Convert.ToSingle(token.Parameter("insetHeight")),
                        Convert.ToSingle(token.Parameter("extrudeHeight"))
                    );
                    break;
                case "l":
                    current = Polyhedronisme.ApplyStellation(current, faceSignatureRounding);
                    break;
                case "f":
                    current = Polyhedronisme.ApplyFuckedStellation(current, faceSignatureRounding);
                    break;
                case "c":
                    if (token.NamedParameters.TryGetValue("colorRemap", out var remapObj) && remapObj is Dictionary<int, int> remapDict)
                        current = Polyhedronisme.ApplyColorRemap(current,
                            remapDict,
                            paletteColorsCount
                        );
                    break;
                default:
                    // Unknown operator: skip
                    break;
            }
        }

        return current;
    }
}