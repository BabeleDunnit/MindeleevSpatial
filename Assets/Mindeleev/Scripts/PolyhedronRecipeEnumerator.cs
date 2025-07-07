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
        switch (Operators[t.opIdx])
        {
            case 'k': // Kis
            case 't': // Truncate (same as Kis)
                return t.param0Idx
                    + ParamValues.Length * t.roundingIdx
                    + ParamValues.Length * FaceSignatureRounding.Length * t.faceSidesIdx
                    + ParamValues.Length * FaceSignatureRounding.Length * FaceSidesFilter.Length * t.opIdx;
            case 'n': // InsetN
                return t.param1Idx
                    + ParamValues.Length * t.param0Idx
                    + ParamValues.Length * ParamValues.Length * t.roundingIdx
                    + ParamValues.Length * ParamValues.Length * FaceSignatureRounding.Length * t.faceSidesIdx
                    + ParamValues.Length * ParamValues.Length * FaceSignatureRounding.Length * FaceSidesFilter.Length * t.opIdx;
            default: // a, d, l: only rounding
                return t.roundingIdx
                    + FaceSignatureRounding.Length * t.opIdx;
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
        int basePolyIdx = n % BasePolyhedra.Length;
        n /= BasePolyhedra.Length;

        List<string> tokens = new List<string>();
        int tokenCount = 0;
        while (n > 0 && tokenCount < MaxTokens)
        {
            // Find which operator this token is
            int opIdx = 0;
            int tokenInt = 0;
            // Try all operators to find which fits
            int running = n;
            for (int testOpIdx = 0; testOpIdx < Operators.Length; testOpIdx++)
            {
                int tokenSpace = TokenSpaceSize(testOpIdx);
                if (running < tokenSpace)
                {
                    opIdx = testOpIdx;
                    tokenInt = running;
                    break;
                }
                running -= tokenSpace;
            }
            n = (n - tokenInt) / TokenSpaceSize(opIdx);

            char op = Operators[opIdx];
            string token = op.ToString();

            if (op == 'k' || op == 't')
            {
                int param0Idx = tokenInt % ParamValues.Length; tokenInt /= ParamValues.Length;
                int roundingIdx = tokenInt % FaceSignatureRounding.Length; tokenInt /= FaceSignatureRounding.Length;
                int faceSidesIdx = tokenInt % FaceSidesFilter.Length; tokenInt /= FaceSidesFilter.Length;
                token += $"({FaceSidesFilter[faceSidesIdx]},{FaceSignatureRounding[roundingIdx]},{ParamValues[param0Idx].ToString("0.0", CultureInfo.InvariantCulture)})";
            }
            else if (op == 'n')
            {
                int param1Idx = tokenInt % ParamValues.Length; tokenInt /= ParamValues.Length;
                int param0Idx = tokenInt % ParamValues.Length; tokenInt /= ParamValues.Length;
                int roundingIdx = tokenInt % FaceSignatureRounding.Length; tokenInt /= FaceSignatureRounding.Length;
                int faceSidesIdx = tokenInt % FaceSidesFilter.Length; tokenInt /= FaceSidesFilter.Length;
                token += $"({FaceSidesFilter[faceSidesIdx]},{FaceSignatureRounding[roundingIdx]},{ParamValues[param0Idx].ToString("0.0", CultureInfo.InvariantCulture)},{ParamValues[param1Idx].ToString("0.0", CultureInfo.InvariantCulture)})";
            }
            else // a, d, l
            {
                int roundingIdx = tokenInt % FaceSignatureRounding.Length; tokenInt /= FaceSignatureRounding.Length;
                token += $"({FaceSignatureRounding[roundingIdx]})";
            }

            tokens.Add(token);
            tokenCount++;
        }

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
        List<int> tokenRadixes = new List<int>();
        int i = 0;
        while (i < basePos)
        {
            // Skip any whitespace (optional, if your recipes can have spaces)
            while (i < basePos && char.IsWhiteSpace(recipe[i]))
                i++;

            // Expect an operator
            char op = recipe[i];
            int opIdx = Array.IndexOf(Operators, op);
            if (opIdx < 0)
                throw new ArgumentException($"Unknown operator: {op}");

            int faceSidesIdx = 0, roundingIdx = 0, param0Idx = 0, param1Idx = 0;
            i++; // Move past operator

            if (i < basePos && recipe[i] == '(')
            {
                int openPos = i;
                int closePos = recipe.IndexOf(')', openPos);
                if (closePos == -1)
                    throw new ArgumentException("Malformed recipe: missing ')'");

                string paramStr = recipe.Substring(openPos + 1, closePos - (openPos + 1));
                string[] parameters = paramStr.Split(',').Select(p => p.Trim()).ToArray();

                if (op == 'k' || op == 't')
                {
                    faceSidesIdx = Array.IndexOf(FaceSidesFilter, int.Parse(parameters[0]));
                    roundingIdx = Array.IndexOf(FaceSignatureRounding, int.Parse(parameters[1]));
                    param0Idx = FindClosestParamIndex(float.Parse(parameters[2], CultureInfo.InvariantCulture));
                }
                else if (op == 'n')
                {
                    faceSidesIdx = Array.IndexOf(FaceSidesFilter, int.Parse(parameters[0]));
                    roundingIdx = Array.IndexOf(FaceSignatureRounding, int.Parse(parameters[1]));
                    param0Idx = FindClosestParamIndex(float.Parse(parameters[2], CultureInfo.InvariantCulture));
                    param1Idx = FindClosestParamIndex(float.Parse(parameters[3], CultureInfo.InvariantCulture));
                }
                else // a, d, l
                {
                    roundingIdx = Array.IndexOf(FaceSignatureRounding, int.Parse(parameters[0]));
                }

                // Encode token using your EncodeToken logic
                var token = new Token
                {
                    opIdx = opIdx,
                    faceSidesIdx = faceSidesIdx,
                    roundingIdx = roundingIdx,
                    param0Idx = param0Idx,
                    param1Idx = param1Idx
                };
                int tokenInt = EncodeToken(token);
                int tokenRadix = TokenSpaceSize(opIdx);

                tokenInts.Add(tokenInt);
                tokenRadixes.Add(tokenRadix);

                i = closePos + 1; // Move past ')'
            }
            else
            {
                // No params, just operator
                var token = new Token { opIdx = opIdx };
                int tokenInt = EncodeToken(token);
                int tokenRadix = TokenSpaceSize(opIdx);

                tokenInts.Add(tokenInt);
                tokenRadixes.Add(tokenRadix);

                // i already points to next operator
            }
        }

        // 3. Combine token ints into integer (reverse of IntToRecipe)
        tokenInts.Reverse();
        tokenRadixes.Reverse();

        int n = 0;
        for (int t = 0; t < tokenInts.Count; t++)
        {
            n = n * tokenRadixes[t] + tokenInts[t];
        }
        n = n * BasePolyhedra.Length + basePolyIdx;
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

    private static int FindClosestParamIndex(float value)
    {
        float epsilon = 1e-4f;
        for (int i = 0; i < ParamValues.Length; i++)
        {
            if (Mathf.Abs(ParamValues[i] - value) < epsilon)
                return i;
        }
        throw new ArgumentException($"Value {value} not found in ParamValues.");
    }

}
