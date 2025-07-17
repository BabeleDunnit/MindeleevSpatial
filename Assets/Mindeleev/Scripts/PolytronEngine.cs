using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolytronEngine : MonoBehaviour
{

    private static List<Polytron> polytrons = new();

    public static void Register(Polytron instance)
    {
        instance.Id = polytrons.Count;
        polytrons.Add(instance);
        // Debug.Log("Polytron registered: " + instance.Id);
    }

    public static void Unregister(Polytron instance)
    {
        polytrons.Remove(instance);
        // Debug.Log("Prefab deregistered: " + instance.Id);
    }

    //     public static IEnumerable<GameObject> GetAll() => polytrons;
    public static IEnumerable<Polytron> GetAll() => polytrons;

    // Start is called before the first frame update
    void Start()
    {


        Debug.Log($"PolytronEngine::Start() entering");
        // all empty here
        foreach (Polytron p in GetAll())
        {
            Debug.Log($"polytron {p.Id} is of type {p.recipeString}");
            // p.Behaviour.ComputeForce();
        }

        Debug.Log($"PolytronEngine::Start() exiting");

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (Polytron p in GetAll())
        {
            // Debug.Log("Call ComputeForce for " + p.Id);
            PolytronBehaviour b = p.Behaviour;
            if (b != null)
            {
                b.ComputeForce();
            }
        }
    }

    public static Dictionary<string, int> CollectRecipes()
    {
        Dictionary<string, int> recipeCounts = new();
        foreach (Polytron p in GetAll())
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
