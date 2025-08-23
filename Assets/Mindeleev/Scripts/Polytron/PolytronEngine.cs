using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolytronEngine : MonoBehaviour
{

    //  public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector

    [SerializeReference]
    public PolytronEngineBehaviour engineBehaviour_;
    
    private PolytronEngineBehaviour engineBehaviour;

    private Camera spatialCamera;

    public List<Polytron> registeredPolytrons = new List<Polytron>();


    void Awake()
    {
        engineBehaviour = ScriptableObject.Instantiate(engineBehaviour_);
    }

    public void Register(Polytron instance)
    {
        instance.Id = registeredPolytrons.Count;
        registeredPolytrons.Add(instance);
        Debug.Log("Polytron registered: " + instance.Id);
    }

    public void Unregister(Polytron instance)
    {
        registeredPolytrons.Remove(instance);
        Debug.Log("Prefab deregistered: " + instance.Id);
    }

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<MeshRenderer>().enabled = false;

        engineBehaviour.Setup(gameObject);

        Debug.Log($"PolytronEngine::Start() entering");
        // all empty here
        foreach (Polytron p in registeredPolytrons)
        {
            Debug.Log($"polytron {p.Id} is of type {p.recipeString}");
            // p.Behaviour.ComputeForce();
        }

        Debug.Log($"PolytronEngine::Start() exiting");

        spatialCamera = CrossPlatformUtils.FindCamera();
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        engineBehaviour.Loop(registeredPolytrons, gameObject);
        // engineBehaviour.Loop(PolytronsFactory.Instance.createdPolytrons.ConvertAll(go => go.GetComponent<Polytron>()), gameObject);

        foreach (Polytron p in registeredPolytrons)
        {
        }
    }

    public Dictionary<string, int> CollectRecipes()
    {
        Dictionary<string, int> recipeCounts = new();
        foreach (Polytron p in registeredPolytrons)
        {
            // Debug.Log("recipe: " + p.recipeString);
            if (!string.IsNullOrEmpty(p.recipeString))
            {
                if (recipeCounts.ContainsKey(p.recipeString))
                    recipeCounts[p.recipeString]++;
                else
                    recipeCounts[p.recipeString] = 1;
            }
        }

        Debug.Log($"We have {registeredPolytrons.Count} polytrons with {recipeCounts.Count} different recipes:");
        foreach (var kv in recipeCounts)
        {
            Debug.Log($"  {kv.Key} : {kv.Value}");
        }

        return recipeCounts;
    }


}
