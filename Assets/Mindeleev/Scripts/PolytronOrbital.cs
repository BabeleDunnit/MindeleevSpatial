using UnityEngine;
using System.Linq;

public class PolytronOrbital : MonoBehaviour
{
    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public float orbitalRadius = 2.0f;
    public float orbitalYOffset = 0.0f;

    void Start()
    {
        // Get the PolyhedronGenerator on this nucleus
        var generator = GetComponent<PolyhedronGenerator>();
        if (generator == null || polytronPrefab == null)
        {
            Debug.LogWarning("PolyhedronGenerator or PolytronPrefab not assigned.");
            return;
        }

        // Extract the base polyhedron (last uppercase letter in the recipe)
        string recipe = generator.polyhedronRecipe;
        char basePoly = recipe.LastOrDefault(c => char.IsUpper(c));
        if (basePoly == default)
        {
            Debug.LogWarning("No base polyhedron found in recipe: " + recipe);
            return;
        }

        // Prepare the base recipe string
        string baseRecipe = basePoly.ToString();

        // try to clone the Pauli/Aufbau sequence
        // use something analogous to quantic numbers


        string[] ops = { "k", "a", "n", "l" };

        for (int i = 2; i < 6; i++)
        {
            orbitalRadius = i * 2;
            for (int j = 0; j < i; j++)
            {
                if (i < 2) continue;
                float angle = j * Mathf.PI * 2f / (float)i;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * orbitalRadius,
                    orbitalYOffset,
                    Mathf.Sin(angle) * orbitalRadius
                );
                Vector3 position = transform.position + offset;

                GameObject orbital = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
                orbital.transform.localScale = transform.localScale * 0.3f; // Match nucleus scale

                // Set the base recipe
                var orbitalGen = orbital.GetComponent<PolyhedronGenerator>();
                if (orbitalGen != null)
                {
                    orbitalGen.polyhedronRecipe = string.Concat(Enumerable.Repeat(ops[i-2], j)) + baseRecipe;
                }

                orbital.name = $"Orbital_{baseRecipe}_{i + 1}";
            }
        }
    }
}