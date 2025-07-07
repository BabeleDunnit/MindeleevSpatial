using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Globalization;
using TMPro;


public static class PolyhedronRecipeEnumerator
{
    // Operator codes and their parameter spaces
    private static readonly char[] Operators = { 't', 'k', 'a', 'd', 'n', 'l' };
    private static readonly int[] FaceSidesFilter = { 0, 3, 4, 5 };
    private static readonly int[] FaceSignatureRounding = { 0, 1, 2, 3 };
    private static readonly float[] ParamValues = Enumerable.Range(0, 11).Select(i => -0.5f + 0.1f * i).ToArray();
    private static readonly char[] BasePolyhedra = { 'C', 'T', 'O', 'D', 'I' };

    // Token structure
    private struct Token
    {
        public int opIdx;
        public int faceSidesIdx;
        public int roundingIdx;
        public int param0Idx;
        public int param1Idx; // Only for n (InsetN)
    }

    // Maximum number of tokens in a recipe (adjust as needed)
    private const int MaxTokens = 5;

    // Encode a single token as an integer
    private static int EncodeToken(Token t)
    {
        // For each operator, encode only relevant params
        switch (Operators[t.opIdx])
        {
            case 'k': // Kis
                return t.opIdx
                    + Operators.Length * t.faceSidesIdx
                    + Operators.Length * FaceSidesFilter.Length * t.roundingIdx
                    + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * t.param0Idx;
            case 'n': // InsetN
                return t.opIdx
                    + Operators.Length * t.faceSidesIdx
                    + Operators.Length * FaceSidesFilter.Length * t.roundingIdx
                    + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * t.param0Idx
                    + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * ParamValues.Length * t.param1Idx;
            case 't': // Truncate (same as Kis)
                return t.opIdx
                    + Operators.Length * t.faceSidesIdx
                    + Operators.Length * FaceSidesFilter.Length * t.roundingIdx
                    + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * t.param0Idx;
            default: // a, d, l: only rounding
                return t.opIdx
                    + Operators.Length * t.roundingIdx;
        }
    }

    // Decode a single token from an integer (for demonstration, not used in main mapping)
    // ...

    // Get the number of possible tokens for each operator
    private static int TokenSpaceSize(int opIdx)
    {
        char op = Operators[opIdx];
        if (op == 'k' || op == 't')
            return FaceSidesFilter.Length * FaceSignatureRounding.Length * ParamValues.Length;
        if (op == 'n')
            return FaceSidesFilter.Length * FaceSignatureRounding.Length * ParamValues.Length * ParamValues.Length;
        // a, d, l
        return FaceSignatureRounding.Length;
    }

    // Get the total number of possible tokens
    private static int TotalTokenSpace()
    {
        int total = 0;
        for (int i = 0; i < Operators.Length; i++)
            total += TokenSpaceSize(i);
        return total;
    }

    // Map an integer to a recipe string
    public static string IntToRecipe(int n)
    {
        // 1. Choose base polyhedron
        int basePolyIdx = n % BasePolyhedra.Length;
        n /= BasePolyhedra.Length;

        // 2. Build operator tokens (from least to most significant)
        List<string> tokens = new List<string>();
        int tokenCount = 0;
        while (n > 0 && tokenCount < MaxTokens)
        {
            int opIdx = n % Operators.Length;
            n /= Operators.Length;

            char op = Operators[opIdx];
            string token = op.ToString();

            if (op == 'k' || op == 't')
            {
                int param0Idx = n % ParamValues.Length; n /= ParamValues.Length;
                int roundingIdx = n % FaceSignatureRounding.Length; n /= FaceSignatureRounding.Length;
                int faceSidesIdx = n % FaceSidesFilter.Length; n /= FaceSidesFilter.Length;
                token += $"({FaceSidesFilter[faceSidesIdx]},{FaceSignatureRounding[roundingIdx]},{ParamValues[param0Idx].ToString("0.0", CultureInfo.InvariantCulture)})";
            }
            else if (op == 'n')
            {
                int param1Idx = n % ParamValues.Length; n /= ParamValues.Length;
                int param0Idx = n % ParamValues.Length; n /= ParamValues.Length;
                int roundingIdx = n % FaceSignatureRounding.Length; n /= FaceSignatureRounding.Length;
                int faceSidesIdx = n % FaceSidesFilter.Length; n /= FaceSidesFilter.Length;
                token += $"({FaceSidesFilter[faceSidesIdx]},{FaceSignatureRounding[roundingIdx]},{ParamValues[param0Idx].ToString("0.0", CultureInfo.InvariantCulture)},{ParamValues[param1Idx].ToString("0.0", CultureInfo.InvariantCulture)})";
            }
            else // a, d, l
            {
                int roundingIdx = n % FaceSignatureRounding.Length; n /= FaceSignatureRounding.Length;
                token += $"({FaceSignatureRounding[roundingIdx]})";
            }

            tokens.Add(token);
            tokenCount++;
        }

        // 3. Compose recipe (reverse tokens for left-to-right application)
        tokens.Reverse();
        string recipe = string.Concat(tokens) + BasePolyhedra[basePolyIdx];
        return recipe;
    }

    public static int RecipeToInt(string recipe)
    {
        // 1. Find base polyhedron (last uppercase letter)
        int basePos = recipe.Length - 1;
        while (basePos >= 0 && !char.IsUpper(recipe[basePos]))
            basePos--;
        if (basePos < 0)
            throw new ArgumentException("No base polyhedron found in recipe.");

        char basePoly = recipe[basePos];
        int basePolyIdx = Array.IndexOf(BasePolyhedra, basePoly);
        if (basePolyIdx < 0)
            throw new ArgumentException("Unknown base polyhedron: " + basePoly);

        // 2. Parse tokens (left to right)
        List<int> tokenInts = new List<int>();
        int i = 0;
        while (i < basePos)
        {
            char op = recipe[i];
            int opIdx = Array.IndexOf(Operators, op);
            if (opIdx < 0)
                throw new ArgumentException($"Unknown operator: {op}");

            int facesSidesIdx = 0, roundingIdx = 0, param0Idx = 0, param1Idx = 0;
            if (i + 1 < basePos && recipe[i + 1] == '(')
            {
                int closePos = recipe.IndexOf(')', i + 2);
                if (closePos == -1)
                    throw new ArgumentException("Malformed recipe: missing ')'");

                string paramStr = recipe.Substring(i + 2, closePos - (i + 2));
                string[] parameters = paramStr.Split(',').Select(p => p.Trim()).ToArray();

                if (op == 'k' || op == 't')
                {
                    if (parameters.Length > 0)
                        facesSidesIdx = Array.IndexOf(FaceSidesFilter, int.Parse(parameters[0]));
                    if (parameters.Length > 1)
                        roundingIdx = Array.IndexOf(FaceSignatureRounding, int.Parse(parameters[1]));
                    if (parameters.Length > 2)
                        param0Idx = Array.IndexOf(ParamValues, float.Parse(parameters[2], CultureInfo.InvariantCulture));
                }
                else if (op == 'n')
                {
                    if (parameters.Length > 0)
                        facesSidesIdx = Array.IndexOf(FaceSidesFilter, int.Parse(parameters[0]));
                    if (parameters.Length > 1)
                        roundingIdx = Array.IndexOf(FaceSignatureRounding, int.Parse(parameters[1]));
                    if (parameters.Length > 2)
                        param0Idx = Array.IndexOf(ParamValues, float.Parse(parameters[2], CultureInfo.InvariantCulture));
                    if (parameters.Length > 3)
                        param1Idx = Array.IndexOf(ParamValues, float.Parse(parameters[3], CultureInfo.InvariantCulture));
                }
                else // a, d, l
                {
                    if (parameters.Length > 0)
                        roundingIdx = Array.IndexOf(FaceSignatureRounding, int.Parse(parameters[0]));
                }

                // Encode token
                int tokenInt = 0;
                if (op == 'k' || op == 't')
                {
                    tokenInt = opIdx
                        + Operators.Length * facesSidesIdx
                        + Operators.Length * FaceSidesFilter.Length * roundingIdx
                        + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * param0Idx;
                }
                else if (op == 'n')
                {
                    tokenInt = opIdx
                        + Operators.Length * facesSidesIdx
                        + Operators.Length * FaceSidesFilter.Length * roundingIdx
                        + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * param0Idx
                        + Operators.Length * FaceSidesFilter.Length * FaceSignatureRounding.Length * ParamValues.Length * param1Idx;
                }
                else // a, d, l
                {
                    tokenInt = opIdx
                        + Operators.Length * roundingIdx;
                }

                tokenInts.Add(tokenInt);
                i = closePos + 1;
            }
            else
            {
                // No params, just operator
                int tokenInt = opIdx;
                tokenInts.Add(tokenInt);
                i++;
            }
        }

        // 3. Combine tokens and base polyhedron into integer (reverse of IntToRecipe)
        int n = basePolyIdx;
        int radix = BasePolyhedra.Length;
        for (int t = tokenInts.Count - 1; t >= 0; t--)
        {
            n = n * Operators.Length + tokenInts[t];
            radix *= Operators.Length;
        }
        return n;
    }

    public static bool AreRecipesEquivalent(string r1, string r2)
    {
        var polyData1 = Polyhedronisme.ParsePolyhedronRecipe(r1);
        var polyFinalData1 = Polyhedronisme.ApplyFlatShade(polyData1);

        var polyData2 = Polyhedronisme.ParsePolyhedronRecipe(r2);
        var polyFinalData2 = Polyhedronisme.ApplyFlatShade(polyData2);

        // Compare meshVertices
        bool vertsEqual = polyFinalData1.meshVertices.SequenceEqual(polyFinalData2.meshVertices);
        // Compare triangles
        bool trisEqual = polyFinalData1.triangles.SequenceEqual(polyFinalData2.triangles);
        // Compare normals
        bool normalsEqual = polyFinalData1.normals.SequenceEqual(polyFinalData2.normals);
        // Compare color indices
        bool colorsEqual = polyFinalData1.colorIndices.SequenceEqual(polyFinalData2.colorIndices);

        return vertsEqual && trisEqual && normalsEqual && colorsEqual;
    }

}
