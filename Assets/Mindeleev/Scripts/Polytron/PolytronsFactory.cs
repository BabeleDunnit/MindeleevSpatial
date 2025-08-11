using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class PolytronsFactory : MonoBehaviour
{

    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector

    /*
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    */

    public static void Create(PolytronEngine engine, int count, string kind)
    {
        for (int i = 0; i < count; i++)
        {
            //            Polytron polytron = new Polytron();
            //            PolytronEngine.Register(polytron);
            //            polytron.Behaviour = new PolytronPhysics(polytron);
            //            polytron.Behaviour.Initialize();

            Vector3 polytronPosition = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f)
            );

            string recipe = "C";

            GameObject poly = Instantiate(engine.polytronPrefab, polytronPosition, Quaternion.identity, engine.transform);
            // poly.transform.localScale = engine.polytronPrefab.transform.localScale * 0.04f;
            var polytronComponent = poly.GetComponent<Polytron>();
            if (polytronComponent != null)
            {
                polytronComponent.Engine = engine;
                engine.Register(polytronComponent);
                polytronComponent.recipeString = recipe;
                polytronComponent.Behaviour = new PolytronSpring01Physics(polytronComponent);
                // Debug.Log($"polytron {polytronComponent.Id} is of type {polytronComponent.Behaviour}");

            }

            poly.name = $"Factory_{i}_{recipe}";
            CreateLabel(poly, recipe
                + " "
                + PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(recipe)),
                Vector3.zero);

        }
    }

    private static void CreateLabel(GameObject parent, string recipe, Vector3 position)
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


    public GameObject Create(string kind)
    {

        Vector3 polytronPosition = new Vector3(
            transform.position.x, transform.position.y, transform.position.z
        );

        string recipe = "taC";

        GameObject poly = Instantiate(polytronPrefab, polytronPosition, Quaternion.identity, transform);
        // poly.transform.localScale = transform.localScale * 0.4f;
        var polytronComponent = poly.GetComponent<Polytron>();
        if (polytronComponent != null)
        {
            // polytronComponent.Engine = engine;
            // engine.Register(polytronComponent);
            polytronComponent.recipeString = recipe;
            // polytronComponent.Behaviour = new PolytronSpring01Physics(polytronComponent);
            // Debug.Log($"polytron {polytronComponent.Id} is of type {polytronComponent.Behaviour}");
        }

        poly.name = $"Alone_{recipe}";

        CreateLabel(poly, recipe
            + " "
            + PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(recipe)),
            Vector3.zero);

        return poly;

    }
}
