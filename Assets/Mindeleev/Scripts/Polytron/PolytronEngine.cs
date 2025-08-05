using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolytronEngine : MonoBehaviour
{

    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public bool callFactory = false;

    private List<Polytron> polytrons = new List<Polytron>();

    public void Register(Polytron instance)
    {
        instance.Id = polytrons.Count;
        polytrons.Add(instance);
        // Debug.Log("Polytron registered: " + instance.Id);
    }

    public void Unregister(Polytron instance)
    {
        polytrons.Remove(instance);
        // Debug.Log("Prefab deregistered: " + instance.Id);
    }

    public IEnumerable<Polytron> GetAllPolytrons() => polytrons;

    // Start is called before the first frame update
    void Start()
    {

        if (callFactory)
        {
            PolytronsFactory.Create(this, 10, "pippo");
        }

        Debug.Log($"PolytronEngine::Start() entering");
        // all empty here
        foreach (Polytron p in GetAllPolytrons())
        {
            Debug.Log($"polytron {p.Id} is of type {p.recipeString}");
            // p.Behaviour.ComputeForce();
        }

        Debug.Log($"PolytronEngine::Start() exiting");

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (Polytron p in GetAllPolytrons())
        {
            // Debug.Log("Call ComputeForce for " + p.Id);
            PolytronPhysics b = p.Behaviour;
            if (b != null)
            {
                b.ComputeForce();
            }
        }
    }

    public Dictionary<string, int> CollectRecipes()
    {
        Dictionary<string, int> recipeCounts = new();
        foreach (Polytron p in GetAllPolytrons())
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

        Debug.Log($"We have {polytrons.Count} polytrons with {recipeCounts.Count} different recipes:");
        foreach (var kv in recipeCounts)
        {
            Debug.Log($"  {kv.Key} : {kv.Value}");
        }

        return recipeCounts;
    }


}
