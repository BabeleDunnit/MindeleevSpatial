using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PolytronsFactory : MonoBehaviour
{
    public static PolytronsFactory Instance { get; private set; }

    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public GameObject sinkPrefab;

    public List<GameObject> createdPolytrons = new List<GameObject>();
    public List<GameObject> createdSinks = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PolytronsFactory instances detected. Destroying duplicate.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void Create(GameObject engineGameObject, int count, string kind)
    {
        PolytronEngine engine = engineGameObject.GetComponent<PolytronEngine>();

        for (int i = 0; i < count; i++)
        {

            Vector3 polytronPosition = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(-5f, 5f),
                Random.Range(-5f, 5f)
            );

            string recipe = "tdC";

            GameObject poly = Instantiate(polytronPrefab, engine.gameObject.transform.position + polytronPosition, Quaternion.identity, engine.transform);
            poly.transform.localScale = polytronPrefab.transform.localScale * 0.5f;
            var polytronComponent = poly.GetComponent<Polytron>();
            if (polytronComponent != null)
            {
                polytronComponent.Engine = engine;
                engine.Register(polytronComponent);
                polytronComponent.recipeString = recipe;
            }

            poly.name = $"Factory_{i}_{recipe}";
            CreateLabel(poly, recipe
                + " "
                + PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(recipe)),
                Vector3.zero);

            createdPolytrons.Add(poly);
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
        switch (kind)
        {
            case "polytron":
                {
                    string recipe = "taC";

                    GameObject poly = Instantiate(polytronPrefab, Vector3.zero, Quaternion.identity, transform);
                    var polytronComponent = poly.GetComponent<Polytron>();
                    if (polytronComponent != null)
                    {
                        polytronComponent.recipeString = recipe;
                    }

                    poly.name = $"{kind}_{recipe}";

                    CreateLabel(poly, recipe
                        + " "
                        + PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(recipe)),
                        Vector3.zero);

                    createdPolytrons.Add(poly);

                    return poly;

                }
                break;
            case "sink":
                {
                    GameObject sink = Instantiate(sinkPrefab, Vector3.zero, Quaternion.identity, transform);
                    createdSinks.Add(sink);
                    sink.name = "sink";

                    return sink;

                }
                break;

            default:
                break;
        }

        /*
                    Vector3 polytronPosition = new Vector3(
                        transform.position.x, transform.position.y, transform.position.z
                    );
        */

        return null;
    }
}
