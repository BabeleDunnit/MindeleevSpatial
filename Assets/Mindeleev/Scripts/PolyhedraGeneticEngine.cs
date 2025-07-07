using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class PolyhedraGeneticEngine
{
    // Token structure for genetic operations
    private struct Token
    {
        public char op;
        public List<string> parameters; // Keep as strings for easy crossover and recipe reconstruction
    }

    /// <summary>
    /// Generate a population of N offspring recipes by genetic crossover of two parent recipes.
    /// Each offspring is a recombination of the token "DNA" of the parents.
    /// </summary>
    public static List<string> CrossoverRecipes(string recipeA, string recipeB, int populationSize)
    {
        var tokensA = TokenizeRecipe(recipeA);
        var tokensB = TokenizeRecipe(recipeB);

        int maxTokens = Math.Max(tokensA.Count, tokensB.Count);
        var population = new List<string>();

        var rand = new Random();

        for (int i = 0; i < populationSize; i++)
        {
            var offspringTokens = new List<Token>();

            // Crossover: for each token position, randomly pick from A or B (if available)
            for (int t = 0; t < maxTokens; t++)
            {
                Token? token = null;
                bool pickA = rand.NextDouble() < 0.5;

                if (pickA && t < tokensA.Count)
                    token = tokensA[t];
                else if (!pickA && t < tokensB.Count)
                    token = tokensB[t];
                else if (t < tokensA.Count)
                    token = tokensA[t];
                else if (t < tokensB.Count)
                    token = tokensB[t];

                if (token.HasValue)
                    offspringTokens.Add(token.Value);
            }

            // Always use the base polyhedron from one of the parents (randomly)
            char basePoly = rand.NextDouble() < 0.5 ? GetBasePolyhedron(recipeA) : GetBasePolyhedron(recipeB);

            string offspringRecipe = ReconstructRecipe(offspringTokens, basePoly);
            population.Add(offspringRecipe);
        }

        return population;
    }

    // Helper: Tokenize a recipe string into operator+params tokens
    private static List<Token> TokenizeRecipe(string recipe)
    {
        var tokens = new List<Token>();
        int basePos = recipe.Length - 1;
        while (basePos >= 0 && !char.IsUpper(recipe[basePos]))
            basePos--;

        int i = 0;
        while (i < basePos)
        {
            while (i < basePos && char.IsWhiteSpace(recipe[i]))
                i++;

            char op = recipe[i];
            i++;

            List<string> parameters = new List<string>();
            if (i < basePos && recipe[i] == '(')
            {
                int openPos = i;
                int closePos = recipe.IndexOf(')', openPos);
                string paramStr = recipe.Substring(openPos + 1, closePos - (openPos + 1));
                parameters = paramStr.Split(',').Select(p => p.Trim()).ToList();
                i = closePos + 1;
            }

            tokens.Add(new Token { op = op, parameters = parameters });
        }
        return tokens;
    }

    // Helper: Get the base polyhedron character from a recipe
    private static char GetBasePolyhedron(string recipe)
    {
        for (int i = recipe.Length - 1; i >= 0; i--)
        {
            if (char.IsUpper(recipe[i]))
                return recipe[i];
        }
        throw new ArgumentException("No base polyhedron found in recipe.");
    }

    // Helper: Reconstruct a recipe string from tokens and base polyhedron
    private static string ReconstructRecipe(List<Token> tokens, char basePoly)
    {
        var parts = new List<string>();
        foreach (var token in tokens)
        {
            if (token.parameters.Count > 0)
                parts.Add($"{token.op}({string.Join(",", token.parameters)})");
            else
                parts.Add(token.op.ToString());
        }
        parts.Add(basePoly.ToString());
        return string.Concat(parts);
    }
}