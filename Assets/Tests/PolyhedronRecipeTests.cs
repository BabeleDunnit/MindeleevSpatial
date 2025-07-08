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
        Assert.AreEqual(3, parsed.Tokens[0].Parameters.Count);
        Assert.AreEqual(1, parsed.Tokens[0].Parameters[0]);
        Assert.AreEqual(2, parsed.Tokens[0].Parameters[1]);
        Assert.AreEqual(0.5f, (float)parsed.Tokens[0].Parameters[2], 1e-6);

        Assert.AreEqual("k", parsed.Tokens[1].Operator);
        Assert.AreEqual(3, parsed.Tokens[1].Parameters.Count);
        Assert.AreEqual(3, parsed.Tokens[1].Parameters[0]);
        Assert.AreEqual(1, parsed.Tokens[1].Parameters[1]);
        Assert.AreEqual(0.2f, (float)parsed.Tokens[1].Parameters[2], 1e-6);

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
        Assert.AreEqual(3, parsed.Tokens[0].Parameters.Count);
        Assert.AreEqual(1, parsed.Tokens[0].Parameters[0]);
        Assert.AreEqual(2, parsed.Tokens[0].Parameters[1]);
        Assert.AreEqual(0.1f, (float)parsed.Tokens[0].Parameters[2], 1e-6);

        // k(3) should fill second and third param with defaults 0, -0.5f
        Assert.AreEqual("k", parsed.Tokens[1].Operator);
        Assert.AreEqual(3, parsed.Tokens[1].Parameters.Count);
        Assert.AreEqual(3, parsed.Tokens[1].Parameters[0]);
        Assert.AreEqual(0, parsed.Tokens[1].Parameters[1]);
        Assert.AreEqual(0.1f, (float)parsed.Tokens[1].Parameters[2], 1e-6);

    }

    [Test]
    public void Test_Parse_RecipeWithKeyValueAndString()
    {
        var recipe = "t(1,foo:bar,hello)C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual('C', parsed.BasePolyhedron);
        Assert.AreEqual(1, parsed.Tokens.Count);

        Assert.AreEqual("t", parsed.Tokens[0].Operator);
        Assert.AreEqual(3, parsed.Tokens[0].Parameters.Count);

        Assert.AreEqual(1, parsed.Tokens[0].Parameters[0]);
        var kv = parsed.Tokens[0].Parameters[1] as KeyValuePair<string, object>?;
        Assert.IsNotNull(kv);
        Assert.AreEqual("foo", kv.Value.Key);
        Assert.AreEqual("bar", kv.Value.Value);
        Assert.AreEqual("hello", parsed.Tokens[0].Parameters[2]);
    }

    [Test]
    public void Test_Parse_RecipeWithMissingParams01()
    {
        var recipe = "t()C";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual('C', parsed.BasePolyhedron);
        Assert.AreEqual(1, parsed.Tokens.Count);

        Assert.AreEqual("t", parsed.Tokens[0].Operator);
        Assert.AreEqual(3, parsed.Tokens[0].Parameters.Count);
        Assert.AreEqual(1, parsed.Tokens[0].Parameters[0]);
        Assert.AreEqual(0, parsed.Tokens[0].Parameters[1]);
        Assert.AreEqual(0.1f, (float)parsed.Tokens[0].Parameters[2], 1e-6);
    }

    [Test]
    public void Test_Parse_RecipeWithMissingParams02()
    {
        var recipe = "tC";
        var parsed = PolyhedronRecipeParser.Parse(recipe);

        Assert.AreEqual('C', parsed.BasePolyhedron);
        Assert.AreEqual(1, parsed.Tokens.Count);

        Assert.AreEqual("t", parsed.Tokens[0].Operator);
        Assert.AreEqual(3, parsed.Tokens[0].Parameters.Count);
        Assert.AreEqual(1, parsed.Tokens[0].Parameters[0]);
        Assert.AreEqual(0, parsed.Tokens[0].Parameters[1]);
        Assert.AreEqual(0.1f, (float)parsed.Tokens[0].Parameters[2], 1e-6);
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
