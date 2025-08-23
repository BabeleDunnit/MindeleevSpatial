using System;
using System.Collections.Generic;
using UnityEngine;

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



    // Store created sinks for runtime manipulation
    // [NonSerialized]
    // "Type Mismatch" in the Inspector... why???
    public List<GameObject> sinks = new List<GameObject>();
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
        sinks.Clear();
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

                GameObject sink = PolytronsFactory.Instance.Create("sink");
                sink.transform.position = position;
                sink.transform.localScale = myEngine.transform.localScale * polytronScale;
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