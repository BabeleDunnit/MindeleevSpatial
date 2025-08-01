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

            string originalRecipe = polytron.recipeString;
            string otherOriginalRecipe = otherPolytron.recipeString;

            // Example: sum recipes on collision
            string newRecipe = PolyhedronRecipeAlgebra.SumRecipes(
                new System.Collections.Generic.List<string> { originalRecipe, otherOriginalRecipe }
            );
            polytron.recipeString = newRecipe;
            Debug.Log($"r1: {originalRecipe}, r2: {otherOriginalRecipe}, new this recipe: {newRecipe}");
            polytron.RebuildMesh();

            // otherPolytron.recipeString = newRecipe;
            // Destroy(collision.gameObject);


            Rigidbody rb = GetComponent<Rigidbody>();
            // rb.velocity = Vector3.zero;
            // rb.angularVelocity = Vector3.zero;


            // You can replace the above with any algebra operation you want
            // For example: subtraction, split, etc.
        }
    }
}