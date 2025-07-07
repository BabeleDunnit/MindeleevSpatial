using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class OrbitalGeneticInstantiator : MonoBehaviour
{
    [Header("Prefabs and Parameters")]
    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public int innerOrbitCount = 8;
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
    }

    void InstantiateNucleus()
    {
        nucleusObject = Instantiate(polytronPrefab, transform.position, Quaternion.identity, transform);
        var gen = nucleusObject.GetComponent<PolyhedronGenerator>();
        if (gen != null)
            gen.polyhedronRecipe = nucleusRecipe;
        nucleusObject.name = "Nucleus";
        CreateLabel(nucleusObject, nucleusRecipe, Vector3.zero);
    }

    void InstantiateInnerOrbit()
    {
        for (int i = 0; i < innerOrbitCount; i++)
        {
            float angle = i * Mathf.PI * 2f / innerOrbitCount;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * innerOrbitRadius,
                0.7f,
                Mathf.Sin(angle) * innerOrbitRadius
            );
            Vector3 position = transform.position + offset;

            // Generate a random recipe using IntToRecipe
            int randomInt = Random.Range(0, 2000);
            string randomRecipe = PolyhedronRecipeEnumerator.IntToRecipe(randomInt);

            GameObject poly = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
            poly.transform.localScale = transform.localScale * 0.3f;
            var gen = poly.GetComponent<PolyhedronGenerator>();
            if (gen != null)
                gen.polyhedronRecipe = randomRecipe;
            poly.name = $"Inner_{i}_{randomRecipe}";
            CreateLabel(poly, randomRecipe, Vector3.zero);

            // Enable collision and add handler
            var collider = poly.GetComponent<Collider>();
            if (collider == null)
                collider = poly.AddComponent<SphereCollider>();
            collider.isTrigger = true;

            var handler = poly.AddComponent<InnerPolyhedronCollisionHandler>();
            handler.parent = this;
            handler.myRecipe = randomRecipe;

            innerPolyhedra.Add(poly);
        }
    }

    // Called by InnerPolyhedronCollisionHandler
    public void OnInnerPolyhedronCollision(GameObject collidedPoly, string collidedRecipe)
    {
        // Get nucleus recipe
        string nucleus = nucleusRecipe;

        // Place offspring in a small orbit around the collided polyhedron
        for (int m = 0; m < offspringCount; m++)
        {
            float angle = m * Mathf.PI * 2f / offspringCount;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * offspringOrbitRadius,
                0,
                Mathf.Sin(angle) * offspringOrbitRadius
            );
            Vector3 position = collidedPoly.transform.position + offset;

            // Generate a genetic recipe from nucleus and collided polyhedron
            var population = PolyhedraGeneticEngine.CrossoverRecipes(nucleus, collidedRecipe, 1);
            string offspringRecipe = population[0];

            GameObject offspring = Instantiate(polytronPrefab, position, Quaternion.identity, collidedPoly.transform);
            offspring.transform.localScale = collidedPoly.transform.localScale * 0.9f;
            var gen = offspring.GetComponent<PolyhedronGenerator>();
            if (gen != null)
                gen.polyhedronRecipe = offspringRecipe;
            offspring.name = $"Offspring_{offspringRecipe}";
            CreateLabel(offspring, offspringRecipe, Vector3.zero);
        }
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            parent.OnInnerPolyhedronCollision(gameObject, myRecipe);
            // Optionally, disable further collisions or destroy this handler
            Destroy(this);
        }
    }
}