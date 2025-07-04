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

    /*

        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        var polyData = ParsePolyhedronRecipe(polyhedronRecipe);
        var polyFinalData = ApplyFlatShade(polyData, palette);
        filter.mesh = BuildMesh(polyFinalData);
        ApplyPolyhedronMaterial(renderer);

            if (showVertexIndices)
            {
                ShowVertexIndices(polyData.Item1); // Use logical vertices

                */

    /*
        bool AreRecipesEquivalent(string r1, string r2)
        {
            var polyData1 = ParsePolyhedronRecipe(r1);
            var polyFinalData1 = ApplyFlatShade(polyData1, palette);

            var polyData2 = ParsePolyhedronRecipe(r2);
            var polyFinalData2 = ApplyFlatShade(polyData2, palette);

        }
        */

}
