using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EnumerationTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void TestsAreRecipesEquivalent()
    {
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("C", "C"));
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("O", "O"));
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("I", "I"));
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("D", "D"));
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("T", "T"));
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("kC", "kC"));
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("kO", "kO"));
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("kI", "kI"));
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("kD", "kD"));
        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("kT", "kT"));

        Assert.IsTrue(PolyhedronRecipeEnumerator.AreRecipesEquivalent("ltkdklI", "ltkdklI"));

        Assert.IsFalse(PolyhedronRecipeEnumerator.AreRecipesEquivalent("C", "O"));
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator EnumerationTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
