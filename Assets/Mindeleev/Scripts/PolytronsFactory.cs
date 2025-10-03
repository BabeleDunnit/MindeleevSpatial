using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PolytronsFactory : MonoBehaviour
{
    public static PolytronsFactory Instance { get; private set; }

    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public GameObject sinkPrefab;
    public GameObject tilePrefab;

    public float polytronScale = 0.3f;

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

    /*
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

                string recipe = "ttC";

                GameObject poly = Instantiate(polytronPrefab, engine.gameObject.transform.position + polytronPosition, Quaternion.identity, engine.transform);
                poly.transform.localScale = polytronPrefab.transform.localScale * polytronScale;
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
                    Vector3.down * 1.5f);

                createdPolytrons.Add(poly);
            }
        }
    */

    public static void CreateLabel(GameObject parent, string s, Vector3 position)
    {
        GameObject label = new GameObject($"Label_{s}");
        label.transform.parent = parent.transform;
        // label.transform.localPosition = Vector3.down * 1.5f;
        label.transform.localPosition = position;

        TextMeshPro tmpText = label.AddComponent<TextMeshPro>();
        tmpText.text = s;
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

    public GameObject Create(string kind, float localUniformScale)
    {

        string recipe = "C";
        // string kind = null;

        // the "kind" string is composed from a kind of object and eventually some parameters
        // keep it simple: "kind/recipe"
        int idx = kind.IndexOf('/');
        if (idx > 0)
        {
            recipe = kind.Substring(idx + 1);
            kind = kind.Substring(0, idx);
        }

        switch (kind)
        {
            case "polytron":
                {
                    GameObject poly = Instantiate(polytronPrefab, Vector3.zero, Quaternion.identity, transform);

                    poly.transform.localScale = Vector3.one * localUniformScale;

                    var waveAnim = poly.GetComponent<WaveAnimation>();
                    if (waveAnim != null)
                    {
                        waveAnim.animationName = WaveAnimation.AnimationType.Breathe;
                        waveAnim.SetReferenceTransform(poly.transform.localScale);
                    }

                    var polytronComponent = poly.GetComponent<Polytron>();
                    if (polytronComponent != null)
                    {
                        polytronComponent.recipe = recipe;
                        polytronComponent.sealNumber = createdPolytrons.Count;
                        if (polytronComponent.sealNumber >= 72)
                        {
                            throw new System.Exception("Too many polytrons created, max is 72");
                        }

                        polytronComponent.sealName = PolytronName.GetName(polytronComponent.sealNumber + 1);
                    }

                    poly.name = $"{kind}_{recipe}";
                    // poly.transform. = new Vector3(0.003f, 0.003f, 0.003f);

                    /*
                                        CreateLabel(poly, recipe
                                            + " "
                                            + PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(recipe)),
                                            Vector3.zero);
                    */

                    createdPolytrons.Add(poly);

                    return poly;

                }
            // break;
            case "sink":
                {
                    GameObject sink = Instantiate(sinkPrefab, Vector3.zero, Quaternion.identity, transform);
                    sink.transform.localScale = Vector3.one * localUniformScale;
                    createdSinks.Add(sink);
                    sink.name = "sink";
                    sink.GetComponent<PolytronSink>().attractedRecipe = recipe;

                    return sink;

                }
            // break;

            case "tile":
                {
                    GameObject tile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, transform);
                    tile.transform.localScale = Vector3.one * localUniformScale;

                    var pg = tile.GetComponent<PolyhedronGenerator>();
                    pg.recipe = recipe;

                    return tile;
                }

            default:
                break;
        }

        return null;
    }
}
