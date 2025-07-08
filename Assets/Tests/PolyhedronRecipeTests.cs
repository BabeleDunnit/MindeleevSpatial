using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
