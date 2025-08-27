using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "PolytronEngineHexCellularAutomataBehaviour", menuName = "Polytron Engine/Hex Cellular Automata")]
public class PolytronEngineHexCellularAutomataBehaviour : PolytronEnginePhysics
{
    [Header("Hex Grid Settings")]
    public int maxRing = 3;
    public float hexDistance = 2.0f;
    public float polytronScale = 0.5f;
    public GameObject polytronSinkPrefab; // Assign in inspector
    public GameObject polytronPrefab; // Assign in inspector
    public GameObject polytronTilePrefab; // Assign in inspector

    private HashSet<Polytron> boundPolytrons = new();



    // Store created sinks for runtime manipulation
    // [NonSerialized]
    // "Type Mismatch" in the Inspector... why???
    // also, this is not cleared. Maybe because this script is an asset and not a component?
    public List<GameObject> sinks = new();
    // public List<PolytronSink> sinks2 = new List<PolytronSink>();

    /*
        private void BuildGridWithPolytrons(GameObject myEngine)
        {
            Vector2 center2D = new Vector2(myEngine.transform.position.x, myEngine.transform.position.z);
            // int maxRing = 3;
            int polytronNumber = 0;

            for (int ring = 0; ring <= maxRing; ring++)
            {
                int hexesInRing = ring == 0 ? 1 : 6 * ring;
                for (int i = 0; i < hexesInRing; i++)
                {
                    HexCoord hex;
                    if (ring == 0)
                    {
                        hex = new HexCoord(0, 0); // Center
                    }
                    else
                    {
                        // Use AtPolar to get the hex at (ring, i) in polar coordinates
                        hex = HexCoord.AtPolar(ring, i);
                    }

                    // Get world position for hex center
                    Vector2 hexPos2D = hex.Position() + center2D;
                    Vector3 position = new Vector3(hexPos2D.x, myEngine.transform.position.y, hexPos2D.y);


                    GameObject poly = PolytronsFactory.Instance.Create("polytron");
                    poly.transform.position = position;

                    // GameObject poly = Instantiate(polytronPrefab, position, Quaternion.identity, transform);

                    poly.transform.localScale = myEngine.transform.localScale * polytronScale;

                    var polyGen = poly.GetComponent<PolyhedronGenerator>();
                    if (polyGen != null)
                    {
                        string recipe = ring switch
                        {
                            0 => "C",
                            1 => "tC",
                            2 => "ttC",
                            3 => "ltC",
                            _ => "C"
                        };
                        polyGen.recipeString = recipe;
                    }

                    poly.name = $"HexP_{polytronNumber}_R{ring}_I{i}";
                    // CreateLabel(poly, poly.name, position);

                    polytronNumber++;

                    // add a "pavement"
                    float angleToCenter = hex.PolarAngle();
                    Quaternion pavementRotation = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);
                    Vector3 pavementPosition = new Vector3(hexPos2D.x, myEngine.transform.position.y - 1f, hexPos2D.y);
                    GameObject tile = Instantiate(polytronTilePrefab, pavementPosition, pavementRotation, myEngine.transform);
                    tile.transform.localScale = new Vector3(0.6f, 0.01f, 0.8f);

                    polyGen = tile.GetComponent<PolyhedronGenerator>();
                    if (polyGen != null)
                    {
                        string recipe = ring switch
                        {
                            0 => "O",
                            1 => "tO",
                            2 => "ttO",
                            3 => "ltO",
                            _ => "O"
                        };
                        polyGen.recipeString = recipe;
                    }
                }
            }
        }
        */

    private void BuildGridWithSinks(GameObject myEngine)
    {
        // sinks.Clear();
        Vector2 center2D = new Vector2(myEngine.transform.position.x, myEngine.transform.position.z);
        // int maxRing = 3;

        for (int ring = 0; ring <= maxRing; ring++)
        {
            int hexesInRing = ring == 0 ? 1 : 6 * ring;
            for (int i = 0; i < hexesInRing; i++)
            {
                HexCoord hex;
                if (ring == 0)
                {
                    hex = new HexCoord(0, 0); // Center
                }
                else
                {
                    // Use AtPolar to get the hex at (ring, i) in polar coordinates
                    hex = HexCoord.AtPolar(ring, i);
                }

                // Get world position for hex center
                Vector2 hexPos2D = hex.Position() + center2D;
                Vector3 position = new Vector3(hexPos2D.x, myEngine.transform.position.y, hexPos2D.y);

                GameObject sink = PolytronsFactory.Instance.Create("sink", polytronScale);
                sink.transform.position = position;
//                 sink.transform.localScale = myEngine.transform.localScale * polytronScale;
                PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();
                string sinkRecipe = ring switch
                {
                    0 => "C",
                    1 => "tC",
                    2 => "ttC",
                    3 => "ltC",
                    _ => "C"
                };
                sinkComponent.attractedRecipe = sinkRecipe;
                // sinkComponent.weight *= ((ring + 0.1f) * (float)Math.Exp(ring));
                // sinkComponent.weight += ((float)(ring * 0.1f)) * 2f; 
                sinkComponent.weight += Mathf.Pow((float)(ring * 0.1f), 2f);
                sink.name = $"sink_{sinkRecipe}";
                sink.GetComponent<Renderer>().material.color = new Color(1f, 0f, 0f, 0.5f);
                sinks.Add(sink);
                // sinks2.Add(sinkComponent);

                // add a "pavement"
                float angleToCenter = hex.PolarAngle();
                Quaternion pavementRotation = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);
                Vector3 pavementPosition = new Vector3(hexPos2D.x, myEngine.transform.position.y - 1f, hexPos2D.y);
                GameObject tile = Instantiate(polytronTilePrefab, pavementPosition, pavementRotation, myEngine.transform);
                tile.transform.localScale = new Vector3(0.6f, 0.01f, 0.8f);

                PolyhedronGenerator polyGen = tile.GetComponent<PolyhedronGenerator>();
                if (polyGen != null)
                {
                    string recipe = ring switch
                    {
                        0 => "O",
                        1 => "tO",
                        2 => "ttO",
                        3 => "ltO",
                        _ => "O"
                    };
                    polyGen.recipeString = recipe;
                }
            }
        }
    }


    public override void Setup(GameObject myEngine)
    {
        BuildGridWithSinks(myEngine);
    }

    public override void Loop(List<Polytron> registeredPolytrons, GameObject myEngine)
    {
        // first: check for new, unbound polytrons. We could have a new polytron, a new sink etc.
        /*
        foreach (var polytron in registeredPolytrons)
        {
            if (boundPolytrons.Contains(polytron))
            {
                continue;
            }

            // find all the eligible sinks for the bound with this polytron
            Dictionary<GameObject, float> eligibleSinks = new();
            foreach (var sink in sinks)
            {
                PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();
                if (polytron.recipeString != sinkComponent.attractedRecipe)
                {
                    continue;
                }

                if (sinkComponent.boundPolytron != null)
                {
                    continue;
                }

                float distanceFromPolytronToSink = (sink.transform.position - polytron.transform.position).magnitude;
                eligibleSinks.Add(sink, distanceFromPolytronToSink);
            }

            // we now have all the eligible, free sinks that can accomodate the polytron. We can select the nearest and bind it.
            if (eligibleSinks.Count > 0)
            {
                var nearestSink = eligibleSinks.OrderBy(pair => pair.Value).Last().Key;
                PolytronSink nearestSinkComponent = nearestSink.GetComponent<PolytronSink>();
                nearestSinkComponent.boundPolytron = polytron;
                boundPolytrons.Add(polytron);
            }
        }
        */

        foreach (var sink in sinks)
        {
            PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();
            if (sinkComponent.boundPolytron != null)
            {
                continue;
            }

            Dictionary<Polytron, float> eligiblePolytrons = new();
            foreach (var polytron in registeredPolytrons)
            {
                if (boundPolytrons.Contains(polytron))
                {
                    continue;
                }

                if (polytron.recipeString != sinkComponent.attractedRecipe)
                {
                    continue;
                }

                float distanceFromPolytronToSink = (sink.transform.position - polytron.transform.position).magnitude;
                eligiblePolytrons.Add(polytron, distanceFromPolytronToSink);
            }

            // we now have all the eligible, unbound polytrons that can be accomodated in the sink. 
            // We can select the nearest and bind it.
            if (eligiblePolytrons.Count > 0)
            {
                var nearestPolytron = eligiblePolytrons.OrderBy(pair => pair.Value).Last().Key;
                sinkComponent.boundPolytron = nearestPolytron;
                boundPolytrons.Add(nearestPolytron);
            }

        }


        // now we can attract all the bound polytrons to their sinks
        foreach (var sink in sinks)
        {
            PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();
            if (sinkComponent.boundPolytron == null)
            {
                continue;
            }

            (Vector3 attractionForce, Vector3 from1To2Versor, float from1To2Distance) = CalcSpringForce(sinkComponent.boundPolytron.transform.position, sink.transform.position, sinkComponent.weight, 0.01f);

            Rigidbody polytronRigidBody = sinkComponent.boundPolytron.GetComponent<Rigidbody>();
            polytronRigidBody.AddForce(attractionForce);

        }

    }

    public void LoopUnused(List<Polytron> registeredPolytrons, GameObject myEngine)
    {
        /*
        // Example: Manipulate sinks at runtime
        // Here you could implement cellular automata rules, attract polytrons, etc.
        foreach (var sink in sinks)
        {
            // Example: Toggle color if empty
            if (sink.IsEmpty)
            {
                var renderer = sink.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.gray;
                }
            }
            else
            {
                var renderer = sink.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.green;
                }
            }
        }
        */


        foreach (var sink in sinks)
        {
            PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();
            if (sinkComponent == null) continue;

            foreach (var polytron in registeredPolytrons)
            {
                // PolyhedronGenerator polyGen = polytron.GetComponent<PolyhedronGenerator>();
                //if (polyGen == null) continue;

                if (polytron.recipeString == sinkComponent.attractedRecipe)
                {
                    if (sinkComponent.boundPolytron == null)
                    {
                        // Attraction logic from PolytronEngineSpring01Physics
                        /*
                        Vector3 direction = sink.transform.position - polytron.transform.position;
                        float distance = direction.magnitude;
                        if (distance > 0.01f)
                        {
                            Vector3 force = direction.normalized * Mathf.Clamp(1.0f / distance, 0, 10f);
                            Rigidbody polytronRigidBody = polytron.GetComponent<Rigidbody>();
                            polytronRigidBody.AddForce(polytron, force);
                        }
                        */

                        (Vector3 attractionForce, Vector3 from1To2Versor, float from1To2Distance) =
        CalcSpringForce(polytron.transform.position, sink.transform.position, 1, 0.1f);

                        if (from1To2Distance < 0.5f)
                        {
                            sinkComponent.boundPolytron = polytron;
                        }
                        else
                        {

                            Rigidbody polytronRigidBody = polytron.GetComponent<Rigidbody>();
                            polytronRigidBody.AddForce(attractionForce);
                        }

                    }


                }
            }
        }
    }
}