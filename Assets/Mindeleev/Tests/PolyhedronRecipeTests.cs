using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

using System.IO;
using System.Text;
using System.Globalization;
using System;


public class PolyhedronRecipeTests
{


    [Test]
    public void Test_Parse_SimpleRecipe()
    {
        var recipe = "t(1,2,0.5)k(3,1,0.2)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual('C', parsed.BasePolyhedron);
        Assert.AreEqual(2, parsed.Tokens.Count);

        Assert.AreEqual("t", parsed.Tokens[0].Operator);
        Assert.AreEqual(3, parsed.Tokens[0].PositionalParameters.Count);
        Assert.AreEqual(1, parsed.Tokens[0].PositionalParameters[0]);
        Assert.AreEqual(2, parsed.Tokens[0].PositionalParameters[1]);
        Assert.AreEqual(0.5f, (float)parsed.Tokens[0].PositionalParameters[2], 1e-6);

        Assert.AreEqual("k", parsed.Tokens[1].Operator);
        Assert.AreEqual(3, parsed.Tokens[1].PositionalParameters.Count);
        Assert.AreEqual(3, parsed.Tokens[1].PositionalParameters[0]);
        Assert.AreEqual(1, parsed.Tokens[1].PositionalParameters[1]);
        Assert.AreEqual(0.2f, (float)parsed.Tokens[1].PositionalParameters[2], 1e-6);

        Assert.AreEqual(recipe, parsed.ToString());
    }

    [Test]
    public void Test_Parse_RecipeWithDefaults()
    {
        var recipe = "t(1,2)k(3)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual('C', parsed.BasePolyhedron);
        Assert.AreEqual(2, parsed.Tokens.Count);

        // t(1,2) should fill third param with default -0.3f
        Assert.AreEqual("t", parsed.Tokens[0].Operator);
        Assert.AreEqual(3, parsed.Tokens[0].PositionalParameters.Count);
        Assert.AreEqual(1, parsed.Tokens[0].PositionalParameters[0]);
        Assert.AreEqual(2, parsed.Tokens[0].PositionalParameters[1]);
        Assert.AreEqual(0.1f, (float)parsed.Tokens[0].PositionalParameters[2], 1e-6);

        // k(3) should fill second and third param with defaults 0, -0.5f
        Assert.AreEqual("k", parsed.Tokens[1].Operator);
        Assert.AreEqual(3, parsed.Tokens[1].PositionalParameters.Count);
        Assert.AreEqual(3, parsed.Tokens[1].PositionalParameters[0]);
        Assert.AreEqual(0, parsed.Tokens[1].PositionalParameters[1]);
        Assert.AreEqual(0.1f, (float)parsed.Tokens[1].PositionalParameters[2], 1e-6);

    }

    [Test]
    public void Test_Parse_RecipeWithKeyValueAndString()
    {
        var recipe = "t(1,foo:bar,hello)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual('C', parsed.BasePolyhedron);
        Assert.AreEqual(1, parsed.Tokens.Count);

        Assert.AreEqual("t", parsed.Tokens[0].Operator);
        Assert.AreEqual(3, parsed.Tokens[0].PositionalParameters.Count);

        Assert.AreEqual(1, parsed.Tokens[0].PositionalParameters[0]);
        var kv = parsed.Tokens[0].PositionalParameters[1] as KeyValuePair<string, object>?;
        Assert.IsNotNull(kv);
        Assert.AreEqual("foo", kv.Value.Key);
        Assert.AreEqual("bar", kv.Value.Value);
        Assert.AreEqual("hello", parsed.Tokens[0].PositionalParameters[2]);
    }

    [Test]
    public void Test_Parse_RecipeWithMissingParams01()
    {
        var recipe = "t()C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual('C', parsed.BasePolyhedron);
        Assert.AreEqual(1, parsed.Tokens.Count);

        Assert.AreEqual("t", parsed.Tokens[0].Operator);
        Assert.AreEqual(3, parsed.Tokens[0].PositionalParameters.Count);
        Assert.AreEqual(1, parsed.Tokens[0].PositionalParameters[0]);
        Assert.AreEqual(0, parsed.Tokens[0].PositionalParameters[1]);
        Assert.AreEqual(0.1f, (float)parsed.Tokens[0].PositionalParameters[2], 1e-6);

//         Assert.AreEqual(recipe, parsed.ToString());
    }

    [Test]
    public void Test_Parse_RecipeWithMissingParams02()
    {
        var recipe = "tC";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual('C', parsed.BasePolyhedron);
        Assert.AreEqual(1, parsed.Tokens.Count);

        Assert.AreEqual("t", parsed.Tokens[0].Operator);
        Assert.AreEqual(3, parsed.Tokens[0].PositionalParameters.Count);
        Assert.AreEqual(1, parsed.Tokens[0].PositionalParameters[0]);
        Assert.AreEqual(0, parsed.Tokens[0].PositionalParameters[1]);
        Assert.AreEqual(0.1f, (float)parsed.Tokens[0].PositionalParameters[2], 1e-6);

        // Assert.AreEqual(recipe, parsed.ToString());
    }

    [Test]
    public void Test_NamedParameter_Overrides_Positional()
    {
        var recipe = "k(6, 7, 8, centerVertexHeight:-0.5, faceSignatureRounding:2, facesSidesFilter:3)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual(2, parsed.Tokens[0].Parameter("faceSignatureRounding"));
        Assert.AreEqual(3, parsed.Tokens[0].Parameter("facesSidesFilter"));
        Assert.AreEqual(-0.5f, (float)parsed.Tokens[0].Parameter("centerVertexHeight"), 1e-6);

        Assert.AreEqual(2, parsed.Tokens[0].PositionalParameters[0]);

        Assert.AreEqual("k(2,3,-0.5)C", parsed.ToString());
    }

    [Test]
    public void Test_NamedParameter_MixedOrder()
    {
        var recipe = "k(6, facesSidesFilter:3, faceSignatureRounding:2, 7, 8, centerVertexHeight:-0.5 )C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual(2, parsed.Tokens[0].Parameter("faceSignatureRounding"));
        Assert.AreEqual(3, parsed.Tokens[0].Parameter("facesSidesFilter"));
        Assert.AreEqual(-0.5f, (float)parsed.Tokens[0].Parameter("centerVertexHeight"), 1e-6);
    }

    [Test]
    public void Test_NamedParameter_Defaults()
    {
        var recipe = "k()C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual(1, parsed.Tokens[0].Parameter("faceSignatureRounding"));
        Assert.AreEqual(0, parsed.Tokens[0].Parameter("facesSidesFilter"));
        Assert.AreEqual(0.1f, (float)parsed.Tokens[0].Parameter("centerVertexHeight"), 1e-6);

        Assert.AreEqual("k(1,0,0.1)C", parsed.ToString());

    }

    [Test]
    public void Test_NamedParameter_N_Operator()
    {
        var recipe = "n(2, 4, 0.7, -0.2, extrudeHeight:-0.5, insetHeight:0.9)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual(2, parsed.Tokens[0].Parameter("faceSignatureRounding"));
        Assert.AreEqual(4, parsed.Tokens[0].Parameter("facesSidesFilter"));
        Assert.AreEqual(0.9f, (float)parsed.Tokens[0].Parameter("insetHeight"), 1e-6);
        Assert.AreEqual(-0.5f, (float)parsed.Tokens[0].Parameter("extrudeHeight"), 1e-6);
    }

    [Test]
    public void Test_NamedParameter_MissingAndDefault()
    {
        var recipe = "n()C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual(1, parsed.Tokens[0].Parameter("faceSignatureRounding"));
        Assert.AreEqual(0, parsed.Tokens[0].Parameter("facesSidesFilter"));
        Assert.AreEqual(0.6f, (float)parsed.Tokens[0].Parameter("insetHeight"), 1e-6);
        Assert.AreEqual(-0.3f, (float)parsed.Tokens[0].Parameter("extrudeHeight"), 1e-6);
    }

    [Test]
    public void Test_NamedParameter_Ambo()
    {
        var recipe = "a(faceSignatureRounding:5)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual(5, parsed.Tokens[0].Parameter("faceSignatureRounding"));
        Assert.AreEqual("a(5)C", parsed.ToString());
    }

    [Test]
    public void Test_NamedParameter_Ambo2()
    {
        var recipe = "a(5)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual(5, parsed.Tokens[0].Parameter("faceSignatureRounding"));
        Assert.AreEqual("a(5)C", parsed.ToString());
    }



    [Test]
    public void Test_NamedParameter_Dual()
    {
        var recipe = "d()C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual(1, parsed.Tokens[0].Parameter("faceSignatureRounding"));
        Assert.AreEqual("d(1)C", parsed.ToString());
    }

    [Test]
    public void Test_Builder_Matches_ParsePolyhedronRecipe_Kis()
    {
        // Kis changed parameters order
        var expected = Polyhedronisme.ParsePolyhedronRecipe_obsolete("k(2,4,0.2)C");

        var parsed = PolyhedronRecipeParser.Parse("k(4,2,0.2)C");
        var built = PolyhedronRecipeBuilder.Build(parsed);

        AssertPolyhedronTuplesEqual(expected, built);
    }

    [Test]
    public void Test_Builder_Matches_ParsePolyhedronRecipe_Truncate()
    {
        // Truncate changed parameters order because uses Kis
        var expected = Polyhedronisme.ParsePolyhedronRecipe_obsolete("t(2,4,0.3)O");

        var parsed = PolyhedronRecipeParser.Parse("t(4,2,0.3)O");
        var built = PolyhedronRecipeBuilder.Build(parsed);

        AssertPolyhedronTuplesEqual(expected, built);
    }

    [Test]
    public void Test_Builder_Matches_ParsePolyhedronRecipe_InsetN()
    {
        string recipeStr = "n(5,1,0.7,-0.2)I";
        var expected = Polyhedronisme.ParsePolyhedronRecipe_obsolete(recipeStr);

        var parsed = PolyhedronRecipeParser.Parse(recipeStr);
        var built = PolyhedronRecipeBuilder.Build(parsed);

        AssertPolyhedronTuplesEqual(expected, built);
    }

    [Test]
    public void Test_Parse_ColorRemapOperator()
    {
        var recipe = "c(0:1,2:3)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual('C', parsed.BasePolyhedron);
        Assert.AreEqual(1, parsed.Tokens.Count);
        Assert.AreEqual("c", parsed.Tokens[0].Operator);

        var remap = parsed.Tokens[0].NamedParameters["colorRemap"] as Dictionary<int, int>;
        Assert.IsNotNull(remap);
        Assert.AreEqual(2, remap.Count);
        Assert.AreEqual(1, remap[0]);
        Assert.AreEqual(3, remap[2]);
    }

    [Test]
    public void Test_Builder_ApplyColorRemap()
    {
        // Start from a simple cube with known color indices
        var baseTuple = Polyhedronisme.Cube;
        // All faces start with color index 0
        Assert.IsTrue(baseTuple.Item3.All(ci => ci == 0));

        // Build a recipe that remaps color 0 to 5
        var recipe = "c(0:5)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);
        var built = PolyhedronRecipeBuilder.Build(parsed);

        // All color indices should now be 5
        Assert.IsTrue(built.Item3.All(ci => ci == 5));
    }

    [Test]
    public void Test_Builder_ApplyColorRemap2()
    {
        var recipe = "c(0:1)lakk(2, 3, 0.1)C";
        // var recipe = "lakk(2, 3, 0.1)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);
        var built = PolyhedronRecipeBuilder.Build(parsed);

        Assert.IsTrue(built.Item3.All(ci => ci != 0));
    }

    [Test]
    public void Test_Builder_ApplyPartialColorRemap()
    {
        // Create a tuple with mixed color indices
        var baseTuple = Polyhedronisme.Cube;
        var mixedColors = new int[] { 0, 1, 2, 0, 1, 2 };
        var tuple = (baseTuple.Item1, baseTuple.Item2, mixedColors);

        // Apply remap: 0->9, 2->7
        var remap = new Dictionary<int, int> { { 0, 9 }, { 2, 7 } };
        var remapped = Polyhedronisme.ApplyColorRemap(tuple, remap, 6);

        Assert.AreEqual(9, remapped.Item3[0]);
        Assert.AreEqual(1, remapped.Item3[1]);
        Assert.AreEqual(7, remapped.Item3[2]);
        Assert.AreEqual(9, remapped.Item3[3]);
        Assert.AreEqual(1, remapped.Item3[4]);
        Assert.AreEqual(7, remapped.Item3[5]);
    }

    private void AssertPolyhedronTuplesEqual((Vector3[], int[][], int[]) a, (Vector3[], int[][], int[]) b)
    {
        Assert.AreEqual(a.Item1.Length, b.Item1.Length, "Vertex count mismatch");
        Assert.AreEqual(a.Item2.Length, b.Item2.Length, "Face count mismatch");
        Assert.AreEqual(a.Item3.Length, b.Item3.Length, "Color index count mismatch");

        for (int i = 0; i < a.Item1.Length; i++)
            Assert.That(a.Item1[i], Is.EqualTo(b.Item1[i]).Using(Vector3ComparerWithTolerance(1e-5f)), $"Vertex {i} mismatch");

        for (int i = 0; i < a.Item2.Length; i++)
            CollectionAssert.AreEqual(a.Item2[i], b.Item2[i], $"Face {i} mismatch");

        for (int i = 0; i < a.Item3.Length; i++)
            Assert.AreEqual(a.Item3[i], b.Item3[i], $"Color index {i} mismatch");
    }

    private static IEqualityComparer<Vector3> Vector3ComparerWithTolerance(float tolerance)
    {
        return new Vector3EqualityComparer(tolerance);
    }

    private class Vector3EqualityComparer : IEqualityComparer<Vector3>
    {
        private readonly float _tolerance;
        public Vector3EqualityComparer(float tolerance) => _tolerance = tolerance;
        public bool Equals(Vector3 a, Vector3 b) =>
            Mathf.Abs(a.x - b.x) < _tolerance &&
            Mathf.Abs(a.y - b.y) < _tolerance &&
            Mathf.Abs(a.z - b.z) < _tolerance;
        public int GetHashCode(Vector3 obj) => obj.GetHashCode();
    }

    [Test]
    public void Test_AllPermutationsWithRepetitions()
    {
        var chars = new HashSet<char> { 'a', 'b', 'c' };
        var perms = PolyhedronRecipeUtils.AllPermutationsWithRepetition(chars, 5);
        Debug.Log($"Permutations with repetitions: {string.Join(", ", perms)}");

    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator PolyhedronRecipeTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }

    [Test]
    public void SumRecipes_PadsShorterRecipesCorrectly()
    {
        var recipes = new List<string> { "daC", "kI" }; // "daC" (2 ops), "kI" (1 op)
        var result = PolyhedronRecipeAlgebra.SumRecipes(recipes);
        Assert.AreEqual("dtC", result);
    }

    [Test]
    public void SumRecipes_DifferentLengthRecipes()
    {
        var recipes = new List<string> { "tkO", "dC", "aaaI" };
        var result = PolyhedronRecipeAlgebra.SumRecipes(recipes);
        Assert.AreEqual("annC", result);
    }

    [Test]
    public void SplitRecipe_BasePolyhedron_T()
    {
        var result = PolyhedronRecipeAlgebra.SplitRecipe("dT");
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("dT", result[0]);
        Assert.AreEqual("dT", result[1]);
        Assert.AreEqual("dT", result[2]);
    }

    [Test]
    public void SplitRecipe_OperatorsAndBase()
    {
        var result = PolyhedronRecipeAlgebra.SplitRecipe("tdT");
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("kdT", result[0]);
        Assert.AreEqual("kdT", result[1]);
        Assert.AreEqual("dT", result[2]);
    }

    [Test]
    public void SplitRecipe_MultipleOperators()
    {
        var result = PolyhedronRecipeAlgebra.SplitRecipe("ntdT");
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("tkdT", result[0]);
        Assert.AreEqual("tkdT", result[1]);
        Assert.AreEqual("dT", result[2]);
    }

    [Test]
    public void SplitRecipe_MoreOperatorsThanSplits()
    {
        var result = PolyhedronRecipeAlgebra.SplitRecipe("dtdT");
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("dkdT", result[0]);
        Assert.AreEqual("dkdT", result[1]);
        Assert.AreEqual("ddT", result[2]);
    }

    [Test]
    public void SplitRecipe_BasePolyhedron_C()
    {
        var result = PolyhedronRecipeAlgebra.SplitRecipe("aC");
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("dT", result[0]);
        Assert.AreEqual("dT", result[1]);
    }

    [Test]
    public void SplitRecipe_BasePolyhedron_D()
    {
        var result = PolyhedronRecipeAlgebra.SplitRecipe("lD");
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("nO", result[0]);
        Assert.AreEqual("nO", result[1]);
    }

    [Test]
    public void SplitRecipe_EmptyRecipe()
    {
        var result = PolyhedronRecipeAlgebra.SplitRecipe("");
        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public void MastermindSubtraction_NonExact_Basic()
    {
        var r1 = "aakaI";
        var r2 = "aaO";
        var result = PolyhedronRecipeAlgebra.MastermindSubtraction(r1, r2, false);
        // I - O = C, aaka - aa = ak
        Assert.AreEqual("akC", result);
    }

    [Test]
    public void MastermindSubtraction_Exact_Basic()
    {
        var r1 = "aakaI";
        var r2 = "aaO";
        var result = PolyhedronRecipeAlgebra.MastermindSubtraction(r1, r2, true);
        // I - O = C, only rightmost a matches, so aak
        Assert.AreEqual("aakC", result);
    }

    [Test]
    public void MastermindSubtraction_NonExact_DifferentLengths()
    {
        var r1 = "tkaO";
        var r2 = "kaC";
        var result = PolyhedronRecipeAlgebra.MastermindSubtraction(r1, r2, false);
        // O - C = T, tka - k = ta, ta - a = t
        Assert.AreEqual("tT", result);
    }

    [Test]
    public void MastermindSubtraction_Exact_DifferentLengths()
    {
        var r1 = "tkaO";
        var r2 = "kaC";
        var result = PolyhedronRecipeAlgebra.MastermindSubtraction(r1, r2, true);
        // O - C = T, ka matches
        Assert.AreEqual("tT", result);
    }

    [Test]
    public void MastermindSubtraction_NonExact_AllMatch()
    {
        var r1 = "aaaI";
        var r2 = "aaaO";
        var result = PolyhedronRecipeAlgebra.MastermindSubtraction(r1, r2, false);
        // I - O = C, aaa - a = aa, aa - a = a, a - a = ""
        Assert.AreEqual("C", result);
    }

    [Test]
    public void MastermindSubtraction_Exact_AllMatch()
    {
        var r1 = "aaaI";
        var r2 = "aaaO";
        var result = PolyhedronRecipeAlgebra.MastermindSubtraction(r1, r2, true);
        // I - O = C, all match by position, so ""
        Assert.AreEqual("C", result);
    }

    [Test]
    public void MastermindSubtraction_BasePolyhedron_Subtraction()
    {
        var r1 = "dI";
        var r2 = "aO";
        var result = PolyhedronRecipeAlgebra.MastermindSubtraction(r1, r2, false);
        // I - O = C, a does not match
        Assert.AreEqual("dC", result);
    }

    [Test]
    public void MastermindSubtraction_EmptyR2()
    {
        var r1 = "tkaO";
        var r2 = "";
        var result = PolyhedronRecipeAlgebra.MastermindSubtraction(r1, r2, false);
        Assert.AreEqual("tkaO", result);
    }

    [Test]
    public void MastermindSubtraction_EmptyR1()
    {
        var r1 = "";
        var r2 = "kaC";
        var result = PolyhedronRecipeAlgebra.MastermindSubtraction(r1, r2, false);
        Assert.AreEqual("", result);
    }

    [Test]
    public void KabbalahFusion_BasePoly_Mean()
    {
        Assert.AreEqual("T", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "T", "T" }));
        Assert.AreEqual("T", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "T", "C" }));
        Assert.AreEqual("C", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "T", "O" }));
        Assert.AreEqual("D", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "I", "D" }));
        Assert.AreEqual("I", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "I", "I" }));
    }

    [Test]
    public void KabbalahFusion_Operators_NoCarry()
    {
        Assert.AreEqual("kdT", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "adT", "adT" }));
        Assert.AreEqual("tdT", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "atT", "atT" }));
    }

    [Test]
    public void KabbalahFusion_Operators_WithCarry()
    {
        Assert.AreEqual("alnT", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "llT", "llT" }));
        Assert.AreEqual("alnT", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "llT", "llC" }));
    }

    [Test]
    public void KabbalahFusion_MixedLength()
    {
        Assert.AreEqual("adT", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "adT", "dT" }));
        Assert.AreEqual("kdT", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "atT", "tT" }));
    }

    [Test]
    public void KabbalahFusion_DegenerateRecipes()
    {
        Assert.AreEqual("T", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "T" }));
        Assert.AreEqual("T", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "T", "T" }));
        Assert.AreEqual("C", PolyhedronRecipeKabbalah.RecipeFusion(new List<string> { "C" }));
    }

    [Test]
    public void Kabbalah_IntToRecipe_RecipeToInt_Roundtrip()
    {

        Assert.AreEqual("d", PolyhedronRecipeKabbalah.IntToOperatorsSequence(0));
        Assert.AreEqual("a", PolyhedronRecipeKabbalah.IntToOperatorsSequence(1));

        Assert.AreEqual(0, PolyhedronRecipeKabbalah.RecipeToInt("dC", false));
        Assert.AreEqual(0, PolyhedronRecipeKabbalah.RecipeToInt("d", false));

        Assert.AreEqual(2, PolyhedronRecipeKabbalah.RecipeToInt("dC", true));

        Assert.AreEqual(-1, PolyhedronRecipeKabbalah.RecipeToInt("", false));
        Assert.AreEqual(-1, PolyhedronRecipeKabbalah.RecipeToInt(null, false));
        Assert.AreEqual(-1, PolyhedronRecipeKabbalah.RecipeToInt("", true));
        Assert.AreEqual(-1, PolyhedronRecipeKabbalah.RecipeToInt(null, true));


        for (int i = 0; i < 10000; i++)
        {
            string recipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(i);
            int back = PolyhedronRecipeKabbalah.RecipeToInt(recipe, false);
            Assert.AreEqual(i, back, $"Failed at i={i}, recipe={recipe}, back={back}");
        }
    }

    [Test]
    public void Kabbalah_ComputeComplexity()
    {
        // store the complexity of the same recipe applied to different base polyhedra
        var complexities = new Dictionary<char, Dictionary<int, float>>();
        for (int i = 0; i < 100; i++)
        {
            string recipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(i);
            foreach (char c in new[] { 'T', 'C', 'O', 'D', 'I' })
            {
                if (!complexities.ContainsKey(c))
                    complexities[c] = new Dictionary<int, float>();

                string r = recipe + c;
                float complexity = PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(r));
                complexities[c][i] = complexity;
            }
        }

        foreach (char c in new[] { 'T', 'C', 'O', 'D', 'I' })
        {
            var sorted = complexities[c]
                .OrderBy(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();
            // Debug.Log($"Base {c}, {string.Join(", ", sorted)}");
        }


        // After filling 'complexities'...

        // Build a list of sorted index arrays for each base polyhedron
        var sortedIndexes = new Dictionary<char, List<int>>();
        foreach (char c in new[] { 'T', 'C', 'O', 'D', 'I' })
        {
            sortedIndexes[c] = complexities[c]
                .OrderBy(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();
        }

        // For each position, check if all base polys have the same index at that position
        int count = sortedIndexes['T'].Count;
        for (int pos = 0; pos < count; pos++)
        {
            int idxT = sortedIndexes['T'][pos];
            bool allMatch = true;
            foreach (char c in new[] { 'C', 'O', 'D', 'I' })
            {
                if (sortedIndexes[c][pos] != idxT)
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch)
            {
                string recipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(idxT);
                Debug.Log($"Index {idxT} appears at position {pos} for all bases: recipe = {recipe}");
            }
        }

    }

    [Test]
    public void Kabbalah_RecipeFission_Basic()
    {
        // "adk" = a=1, d=0, k=2 => 1*36 + 0*6 + 2 = 38
        var result = PolyhedronRecipeKabbalah.RecipeFission("adkC", 3);
        // 38/3 = 12, remainder 2, so [13, 13, 12]
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(PolyhedronRecipeKabbalah.IntToOperatorsSequence(13) + "C", result[0]);
        Assert.AreEqual(PolyhedronRecipeKabbalah.IntToOperatorsSequence(13) + "C", result[1]);
        Assert.AreEqual(PolyhedronRecipeKabbalah.IntToOperatorsSequence(12) + "C", result[2]);
    }

    [Test]
    public void Kabbalah_RecipeFission_Single()
    {
        var result = PolyhedronRecipeKabbalah.RecipeFission("adkI", 1);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("adkI", result[0]);
    }

    [Test]
    public void Kabbalah_RecipeFission_MorePartsThanValue()
    {
        var result = PolyhedronRecipeKabbalah.RecipeFission("aC", 5); // "a" = 1
        // Should be: [1,0,0,0,0] => ["a","d","d","d","d"]
        Assert.AreEqual(5, result.Count);
        Assert.AreEqual("aC", result[0]);
        Assert.AreEqual("dC", result[1]);
        Assert.AreEqual("dC", result[2]);
        Assert.AreEqual("dC", result[3]);
        Assert.AreEqual("dC", result[4]);
    }

    [Test]
    public void Kabbalah_RecipeFission_Zero()
    {
        var result = PolyhedronRecipeKabbalah.RecipeFission("", 3);
        Assert.AreEqual(0, result.Count);

        result = PolyhedronRecipeKabbalah.RecipeFission("d", 0);
        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public void Kabbalah_IntToRecipe_Complexity_CSV()
    {

        CsvTable csv = new();
        csv.AddRow("idx", "OpSeq", "T", "C", "O", "D", "I");

        List<Dictionary<int, float>> energies = new List<Dictionary<int,float>>();
        List<string> opSeqs = new();
        List<string> polys = new List<string>
        {
            "T", "C", "O", "D", "I"
        };

        for (int p = 0; p < 5; p++)
        {
            Dictionary<int, float> energiesList = new();

            for (int i = 0; i < 72; i++)
            {
                if (p == 0)
                {
                    opSeqs.Add(PolyhedronRecipeKabbalah.IntToOperatorsSequence(i));
                }

                string opSeq = opSeqs[i];

                energiesList.Add(i, PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(opSeq + polys[p])));
            }
            energies.Add(energiesList);
        }

        for (int i = 0; i < 72; i++)
        {
            string opSeq = opSeqs[i];
            csv.AddRow(i, opSeq,
                energies[0][i],
                energies[1][i],
                energies[2][i],
                energies[3][i],
                energies[4][i]
                );
        }


        // now sort by energy

        CsvTable csv2 = new();
        csv2.AddRow("T", "C", "O", "D", "I", "opSeq");
        List<List<int>> idxs = new List<List<int>>();

        for (int p = 0; p < 5; p++)
        {
            energies[p] = energies[p].OrderBy(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
            List<int> newidxs = energies[p].OrderBy(kv => kv.Value).Select(kv => kv.Key).ToList();
            idxs.Add(newidxs);
        }

        for (int i = 0; i < 72; i++)
        {
            
            bool equals = true;
            for (int p = 1; p < 5; p++)
            {
                if (idxs[0][i] != idxs[p][i]) equals = false;
            }

            string recipe = equals ? PolyhedronRecipeKabbalah.IntToOperatorsSequence(i) : "";

            csv2.AddRow(
                idxs[0][i],
                idxs[1][i],
                idxs[2][i],
                idxs[3][i],
                idxs[4][i],
                recipe
            );
        }

        csv2.Save("SortIdxByEnergy.csv");

    }


    public class CsvTable
    {
        private readonly List<List<string>> rows = new List<List<string>>();

        public void AddRow(params object[] values)
        {
            var row = new List<string>();
            foreach (var v in values)
                row.Add(v?.ToString() ?? "");
            rows.Add(row);
        }

        public void Save(string fileName)
        {
            // string dir = Application.persistentDataPath;
            string dir = ".";
            string path = Path.Combine(dir, fileName);
            Directory.CreateDirectory(dir);

            using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                foreach (var row in rows)
                {
                    for (int i = 0; i < row.Count; i++)
                        row[i] = EscapeCsv(row[i]);
                    sw.WriteLine(string.Join(",", row));
                }
            }

            Debug.Log($"CSV salvato in: {path}");
        }

        private static string EscapeCsv(string s)
        {
            bool needQuotes = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
            if (s.Contains("\"")) s = s.Replace("\"", "\"\"");
            return needQuotes ? $"\"{s}\"" : s;
        }
    }


    /*
        public static class CsvSaver
        {
            public static string SaveCsv(string fileName, string[][] rows)
            {
                // costruisci il percorso in persistentDataPath
                // string dir = Application.persistentDataPath;
                string dir = ".";
                string path = Path.Combine(dir, fileName);

                // assicura che la cartella esista
                Directory.CreateDirectory(dir);

                // scrivi il CSV in UTF-8
                using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    foreach (var cols in rows)
                    {
                        // attento a virgole/virgolette
                        for (int i = 0; i < cols.Length; i++)
                            cols[i] = EscapeCsv(cols[i] ?? "");

                        sw.WriteLine(string.Join(",", cols));
                    }
                }

                Debug.Log($"CSV salvato in: {path}");
                return path;
            }

            // Regola base CSV: se contiene virgola, " o newline → racchiudi tra doppi apici e raddoppia gli apici interni
            static string EscapeCsv(string s)
            {
                bool needQuotes = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
                if (s.Contains("\"")) s = s.Replace("\"", "\"\"");
                return needQuotes ? $"\"{s}\"" : s;
            }
        }
        */



}
