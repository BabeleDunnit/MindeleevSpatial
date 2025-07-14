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
        Debug.Log("Polytron registered: " + instance.Id);
    }

    public static void Unregister(Polytron instance)
    {
        polytrons.Remove(instance);
        Debug.Log("Prefab deregistered: " + instance.Id);
    }

//     public static IEnumerable<GameObject> GetAll() => polytrons;
    public static IEnumerable<Polytron> GetAll() => polytrons;

    // Start is called before the first frame update
    void Start()
    {


        Debug.Log($"PolytronEngine::Start() entering");

        foreach (Polytron p in GetAll())
        {
            Debug.Log($"polytron {p.Id} is of type {p.recipeString}");
            // p.Behaviour.ComputeForce();
        }

        Debug.Log($"PolytronEngine::Start() exiting");

    }

    // Update is called once per frame
    void Update()
    {
        /*
        foreach (Polytron p in GetAll())
        {
            Debug.Log($"polytron {p.Id} is of type {p.recipeString}");
            // p.Behaviour.ComputeForce();
        }
        */

        foreach (Polytron p in GetAll())
        {
            PolytronBehaviour b = p.Behaviour;
            if (b != null)
            {
                b.ComputeForce();
            }

        }



        /*

                Debug.Log($"PolytronEngine::Update() entering");

        foreach (Polytron p in GetAll())
        {
            Debug.Log($"polytron {p.Id} is of type {p.recipeString}");
            Debug.Assert(p.Behaviour != null);
            // Debug.Log($"polytron {p.Id} is of type {p.Behaviour.Type}");
                    // p.Behaviour.ComputeForce();
        }

                Debug.Log($"PolytronEngine::Update() exiting");
*/



    }
    

}
