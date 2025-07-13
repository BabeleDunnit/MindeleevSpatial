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

    }

    // Update is called once per frame
    void Update()
    {

    }
    

}
