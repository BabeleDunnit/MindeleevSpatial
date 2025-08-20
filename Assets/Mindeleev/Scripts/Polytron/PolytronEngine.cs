using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolytronEngine : MonoBehaviour
{

    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public bool callFactory = false;

    // [SerializeReference]
    public PolytronEngineSpring01Physics enginePhysics;

    private Camera spatialCamera;

    private List<Polytron> polytrons = new List<Polytron>();

    public void Register(Polytron instance)
    {
        instance.Id = polytrons.Count;
        polytrons.Add(instance);
        Debug.Log("Polytron registered: " + instance.Id);
    }

    public void Unregister(Polytron instance)
    {
        polytrons.Remove(instance);
        Debug.Log("Prefab deregistered: " + instance.Id);
    }

    public List<Polytron> GetAllPolytrons() => polytrons;

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

        spatialCamera = CrossPlatformUtils.FindCamera();

        enginePhysics = gameObject.AddComponent<PolytronEngineSpring01Physics>();

    }

    // Update is called once per frame
    void FixedUpdate()
    {

        enginePhysics.Simulate(GetAllPolytrons());

        foreach (Polytron p in GetAllPolytrons())
        {
            // Debug.Log("Call ComputeForce for " + p.Id);
            /*
            PolytronPhysics b = p.Behaviour;
            if (b != null)
            {
                b.ComputeForce();
            }
            */
        }
    }

    /*
        private Camera FindSpatialCamera()
        {
            var cam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if (cam != null) return cam;
            return null;
        }
    */

/*
        private GameObject outlinedObject;
        void Update()
        {
            // if (Input.GetMouseButtonDown(0))
            Ray ray = spatialCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Debug.Log("[PolytronEngine.Update()]Hai cliccato su " + hit.collider.gameObject.name);
                PolytronOutline outline = hit.collider.gameObject.GetComponent<PolytronOutline>();
                if (outline != null)
                {
                    outline.EnableOutline();
                    outlinedObject = hit.collider.gameObject;
                }
            }

            if (hit.)
            {

            }

        }
    */


    //using UnityEngine;

    //public class RaycastHoverOutline : MonoBehaviour
    //{
    /// <summary>
    /// public float rayDistance = 10f;
    /// </summary>
    //public LayerMask interactableLayer;

    private GameObject lastHovered;

    /*
        void Update()
        {
            // Lancia il ray dal centro dello schermo
    //         Ray ray = spatialCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Ray ray = spatialCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

    //         if (Physics.Raycast(ray, out hit, rayDistance, interactableLayer))
            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObj = hit.collider.gameObject;

                // Se l'oggetto colpito è diverso dal precedente
                if (hitObj != lastHovered)
                {
                    // Disattiva outline del precedente
                    if (lastHovered != null)
                        lastHovered.GetComponent<PolytronOutline>()?.DisableOutline();

                    // Attiva outline sul nuovo
                    hitObj.GetComponent<PolytronOutline>()?.EnableOutline();

                    // Aggiorna riferimento
                    lastHovered = hitObj;
                }
            }
            else
            {
                // Nessun oggetto colpito → togli outline dal precedente
                if (lastHovered != null)
                {
                    lastHovered.GetComponent<PolytronOutline>()?.DisableOutline();
                    lastHovered = null;
                }
            }
        }
    //}
    */





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
