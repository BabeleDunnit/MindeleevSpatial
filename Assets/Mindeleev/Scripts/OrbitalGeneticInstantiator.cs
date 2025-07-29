using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class OrbitalGeneticInstantiator : MonoBehaviour
{
    [Header("Prefabs and Parameters")]
    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    // public int innerOrbitCount = 8;
    public float innerOrbitRadius = 4.0f;
    public int offspringCount = 5;
    public float offspringOrbitRadius = 1.5f;

    [Header("Nucleus Polyhedron")]
    [TextArea]
    public string nucleusRecipe = "C";

    private GameObject nucleusObject;
    private List<GameObject> innerPolyhedra = new List<GameObject>();

    void Start()
    {
        InstantiateNucleus();
        InstantiateInnerOrbit();
        var recipesMap = PolytronEngine.CollectRecipes();
        var recipeSet = new HashSet<string>(recipesMap.Keys);
        PolytronSpringRecipeBasedEquilibriumBehaviour.eqMap = PolytronSpringRecipeBasedEquilibriumBehaviour.CreateEquilibriumDistanceMap(recipeSet);
        // You can now use eqMap as needed
    }

    void InstantiateNucleus()
    {
        nucleusObject = Instantiate(polytronPrefab, transform.position, Quaternion.identity, transform);
        var gen = nucleusObject.GetComponent<PolyhedronGenerator>();
        if (gen != null)
            gen.recipeString = nucleusRecipe;
        nucleusObject.name = "Nucleus";
        CreateLabel(nucleusObject, nucleusRecipe
            + " "
            + PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(nucleusRecipe)),
            Vector3.zero);
    }

    void InstantiateInnerOrbit()
    {
        List<PolyhedronRecipe> permutations = PolyhedronRecipeUtils.RecipePermutations(nucleusRecipe);
        for (int i = 0; i < permutations.Count; i++)
        {
            float angle = i * Mathf.PI * 2f / permutations.Count;
            Vector3 offset = new Vector3(
                // Mathf.Cos(angle) * innerOrbitRadius * Random.Range(0,100),
                Mathf.Cos(angle) * innerOrbitRadius,
                0.0f,
                // Mathf.Cos(angle) * innerOrbitRadius,

                Mathf.Sin(angle) * innerOrbitRadius
            );
            Vector3 position = transform.position + offset;

            // Generate a random recipe using IntToRecipe
            // int randomInt = Random.Range(0, 2000);
            // string randomRecipe = PolyhedronRecipeEnumerator_obsolete.IntToRecipe(randomInt);
            string permutedRecipe = permutations[i].ToString();

            for (int j = 0; j < i+1; j++)
            {
                position.x += j*0.01f;
                GameObject poly = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
                poly.transform.localScale = transform.localScale * 0.4f;
                var polytronComponent = poly.GetComponent<Polytron>();
                if (polytronComponent != null)
                {
                    polytronComponent.recipeString = permutedRecipe;
                    polytronComponent.Behaviour = new PolytronSpringRecipeBasedEquilibriumBehaviour(polytronComponent);
                    // Debug.Log($"polytron {polytronComponent.Id} is of type {polytronComponent.Behaviour}");

                }

                poly.name = $"Inner_{i}_{permutedRecipe}";
                CreateLabel(poly, permutedRecipe
                    + " "
                    + PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(permutedRecipe)),
                    Vector3.zero);

                // Enable collision and add handler
                var collider = poly.GetComponent<Collider>();
                //if (collider == null)
                //    collider = poly.AddComponent<SphereCollider>();
                Debug.Assert(collider != null);

                
                collider.isTrigger = true;

                var handler = poly.AddComponent<InnerPolyhedronCollisionHandler>();
                handler.parent = this;
                handler.myRecipe = permutedRecipe;

                innerPolyhedra.Add(poly);
            }
        }
    }

    public List<GameObject> SpawnOffspring(GameObject collidedPoly, string collidedRecipe)
    {
        List<GameObject> offspringList = new List<GameObject>();
        string nucleus = nucleusRecipe;

        for (int m = 0; m < offspringCount; m++)
        {
            float angle = m * Mathf.PI * 2f / offspringCount;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * offspringOrbitRadius,
                0,
                Mathf.Sin(angle) * offspringOrbitRadius
            );
            Vector3 position = collidedPoly.transform.position + offset;

            var population = PolyhedraGeneticEngine.CrossoverRecipes(nucleus, collidedRecipe, 1);
            string offspringRecipe = population[0];

            GameObject offspring = Instantiate(polytronPrefab, position, Quaternion.identity, collidedPoly.transform);
            offspring.transform.localScale = collidedPoly.transform.localScale * 0.7f;
            var gen = offspring.GetComponent<PolyhedronGenerator>();
            if (gen != null)
                gen.recipeString = offspringRecipe;
            offspring.name = $"Offspring_{offspringRecipe}";
            CreateLabel(offspring, offspringRecipe
                + " "
                + PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(offspringRecipe)),
                Vector3.zero);

            offspringList.Add(offspring);
        }
        return offspringList;
    }

    private void CreateLabel(GameObject parent, string recipe, Vector3 position)
    {
        GameObject label = new GameObject($"Label_{recipe}");
        label.transform.parent = parent.transform;
        label.transform.localPosition = Vector3.down * 1.5f;

        TextMeshPro tmpText = label.AddComponent<TextMeshPro>();
        tmpText.text = recipe;
        tmpText.fontSize = 3;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.enableAutoSizing = true;
        tmpText.fontSizeMin = 1;
        tmpText.fontSizeMax = 2;
        tmpText.material = new Material(Shader.Find("TextMeshPro/Mobile/Distance Field"));
        label.transform.localRotation = Quaternion.identity;
        var rectTransform = tmpText.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(2, 0.5f);
    }
}

// Handles collision for inner orbit polyhedra
public class InnerPolyhedronCollisionHandler : MonoBehaviour
{
    [HideInInspector] public OrbitalGeneticInstantiator parent;
    [HideInInspector] public string myRecipe;

    // Keep track of spawned offspring
    private List<GameObject> spawnedOffspring = new List<GameObject>();

    private void OnCollisionEnter(Collision other)
    {
        if ((other.gameObject.CompareTag("Player") || other.gameObject.GetComponent<CharacterController>() != null) && spawnedOffspring.Count == 0)
        {
            Debug.Log("Collision Enter");
            // Spawn offspring and keep references
            spawnedOffspring = parent.SpawnOffspring(gameObject, myRecipe);
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.GetComponent<CharacterController>() != null)
        {
            // Destroy all spawned offspring
            foreach (var child in spawnedOffspring)
            {
                if (child != null)
                    Destroy(child);
            }   
            spawnedOffspring.Clear();
        }
    }

        private void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.CompareTag("Player") || other.gameObject.GetComponent<CharacterController>() != null) && spawnedOffspring.Count == 0)
        {
            Debug.Log("Trigger Enter");
            // Spawn offspring and keep references
            spawnedOffspring = parent.SpawnOffspring(gameObject, myRecipe);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.GetComponent<CharacterController>() != null)
        {
            // Destroy all spawned offspring
            foreach (var child in spawnedOffspring)
            {
                if (child != null)
                    Destroy(child);
            }
            spawnedOffspring.Clear();
        }
    }

}