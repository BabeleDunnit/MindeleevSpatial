using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EnumerationTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void TestAreRecipesEquivalent()
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

    [Test]
    public void TestComputeRecipesEquivalence()
    {
        var geometries = new List<(string recipe, (List<Vector3> meshVertices, List<int> triangles, List<Vector3> normals, List<int> colorIndices) geom)>();
        var equivalentPairs = new List<(int i, int j, string recipeI, string recipeJ)>();

        for (int i = 0; i <= 100; i++)
        {
            string recipeI = PolyhedronRecipeEnumerator.IntToRecipe(i);
            var polyDataI = Polyhedronisme.ParsePolyhedronRecipe(recipeI);
            var geomI = Polyhedronisme.ApplyFlatShade(polyDataI);

            // Compare with all previous
            for (int j = 0; j < geometries.Count; j++)
            {
                var (recipeJ, geomJ) = geometries[j];
                if (PolyhedronRecipeEnumerator.AreRecipesEquivalent(recipeI, recipeJ))
                {
                    equivalentPairs.Add((i, j, recipeI, recipeJ));
                    break; // Only report first equivalent found for i
                }
            }

            geometries.Add((recipeI, geomI));
        }

        if (equivalentPairs.Count == 0)
        {
            Debug.Log("No geometrically equivalent recipes found.");
        }
        else
        {
            Debug.Log("Equivalent polyhedra found (format: [i] <recipe_i> == [j] <recipe_j>):");
            foreach (var (i, j, recipeI, recipeJ) in equivalentPairs)
            {
                Debug.Log($"{i} {recipeI} == {j} {recipeJ}");
            }
        }
    }

    [Test]
    public void TestRecipeToIntAndBack()
    {
        for (int i = 0; i < 1000; i++)
        {
            string recipe = PolyhedronRecipeEnumerator.IntToRecipe(i);
            int recovered = PolyhedronRecipeEnumerator.RecipeToInt(recipe);
            string recipeFromRecovered = PolyhedronRecipeEnumerator.IntToRecipe(recovered);

            if (PolyhedronRecipeEnumerator.AreRecipesEquivalent(recipe, recipeFromRecovered))
            {
                if (i != recovered)
                {
                    Debug.Log($"int {i} maps to recipe {recipe}, which back-maps to int {recovered}, which maps to recipe {recipeFromRecovered}, but the recipes are equivalent");
                }
                else
                {
                    Debug.Log($"int {i} maps to recipe {recipe}, which back-maps to int {recovered}. OK.");
                }
            }
            else
            {
                Assert.Fail($"int {i} maps to recipe {recipe}, which back-maps to int {recovered}, which maps to recipe {recipeFromRecovered}, but the recipes are NOT equivalent. Test FAILED.");
            }
        }
        Debug.Log("RecipeToInt/IntToRecipe roundtrip test passed for 0..999");
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
