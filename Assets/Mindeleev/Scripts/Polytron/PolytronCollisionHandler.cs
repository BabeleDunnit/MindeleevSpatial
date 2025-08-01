using UnityEngine;

public class PolytronCollisionHandler : MonoBehaviour
{
    private Polytron polytron;

    void Awake()
    {
        polytron = GetComponent<Polytron>();
    }

    void OnCollisionEnter(Collision collision)
    {

        Debug.Log("collision detected");

        var otherPolytron = collision.gameObject.GetComponent<Polytron>();
        if (polytron != null && otherPolytron != null)
        {
            // Example: sum recipes on collision
            string newRecipe = PolyhedronRecipeAlgebra.SumRecipes(
                new System.Collections.Generic.List<string> { polytron.recipeString, otherPolytron.recipeString }
            );
            polytron.recipeString = newRecipe;
            Debug.Log($"r1: {polytron.recipeString}, r2: {otherPolytron.recipeString}, new recipe: {newRecipe}");
            polytron.RebuildMesh();

            otherPolytron.recipeString = newRecipe;

            // You can replace the above with any algebra operation you want
            // For example: subtraction, split, etc.
        }
    }
}