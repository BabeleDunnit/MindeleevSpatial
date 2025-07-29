using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

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
    }

    [Test]
    public void Test_NamedParameter_Dual()
    {
        var recipe = "d()C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual(1, parsed.Tokens[0].Parameter("faceSignatureRounding"));
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
    public void Test_CombinationsWithRepetition()
    {
        string chars = "abc";
        var perms = PolyhedronRecipeUtils.CombinationsWithRepetition(chars, 5);
        Debug.Log($"Combinations with repetitions: {string.Join(", ", perms)}");
         
    }

    [Test]
    public void Test_YetAnotherAufbau()
    {
        // operators are in "complexity" order - totally subjective
        string operators = "daktnl";

        // try to mimick the Pauli exclusion in some way
        for (int i = 1; i < 7; i++)
        {
            int recipeLen = i;
            for (int j = 2; j < i; j++)
            {
                int operatorsSubstringLen = j;
                List<string> combinationsWithRepetition = PolyhedronRecipeUtils.CombinationsWithRepetition(operators.Substring(0, operatorsSubstringLen), recipeLen);
                Debug.Log($"recipeLen: {recipeLen}, operatorsSubstringLen: {operatorsSubstringLen}");
                Debug.Log($"combinationsWithRepetition: {string.Join(", ", combinationsWithRepetition)}");
                List<PolyhedronRecipe> permutations = PolyhedronRecipeUtils.RecipePermutations(operators.Substring(0, operatorsSubstringLen)+"C");
                Debug.Log($"permutations: {string.Join(", ", permutations)}");

            }
        }        

        
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
}
