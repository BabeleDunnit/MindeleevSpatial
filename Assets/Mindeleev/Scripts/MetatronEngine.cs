using System.Collections;
using System.Collections.Generic;
using SpatialSys.UnitySDK.Internal;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;
using System.Diagnostics.Tracing;

public class MetatronEngine : MonoBehaviour
{
    // public GameObject circlePrefab;
    int maxRings = 12;

    enum Behaviour { None, AttractPolytronsToSinks };
    Behaviour actualBehaviour = Behaviour.None;

    public class HexCellData
    {
        public int ring;
        public int idxInRing;
        public Vector3 worldCoords;
        public bool isOnMetatronPattern;
        public GameObject sink;
        public GameObject tile;
        // public GameObject polytron;

        public bool currentState = false;
        public bool nextState = false;
    }

    // these HexCoord lists and maps contains ALL the cells, prebuilt, rings [0,12]
    private Dictionary<HexCoord, HexCellData> gridCellsMap = new Dictionary<HexCoord, HexCellData>();
    // private List<HexCoord> gridCellsList = new List<HexCoord>();
    // subset of gridCellsList, only and all the hexes on the Metatron pattern (a six braces cross)
    private List<HexCoord> metatronCellsList = new List<HexCoord>();

    // the lists of objects of the actual configuration
    private List<GameObject> polytrons = new();
    private HashSet<GameObject> boundPolytrons = new();
    private List<GameObject> sinks = new();
    private List<GameObject> tiles = new();

    int actualRingsCount = 3;

    void Awake()
    {
        BuildHexGridDataStructure();
    }

    void Start()
    {
        // hide placeholder
        GetComponent<MeshRenderer>().enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            BuildLevel(0);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            actualBehaviour = Behaviour.AttractPolytronsToSinks;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Evolve();
        }

    }

    void FixedUpdate()
    {
        switch (actualBehaviour)
        {
            case Behaviour.AttractPolytronsToSinks:
                AttractPolytronsToSinks();
                break;

            default:
                break;
        }
    }


#if TENTATIVO2
    void BindPolytronsToSinks()
    {
        var unboundSink = sinks.Select(s => s.GetComponent<PolytronSink>())
                                      .FirstOrDefault(sinkComp => sinkComp.boundPolytron == null);
        if (unboundSink != null)
        {
            /*
            var unboundPolytronMatchingUnboundSink = polytrons.Select(p => p.GetComponent<Polytron>()).
                FirstOrDefault(p => boundPolytrons.Contains(p) && polytronComp => polytronComp.recipeString == unboundSink.GetComponent<PolytronSink>().attractedRecipe);
                */

            var unboundPolytronMatchingUnboundSink = polytrons
                .Select(p => p.GetComponent<Polytron>())
                .FirstOrDefault(polytronComp =>
                    !boundPolytrons.Contains(polytronComp.gameObject) &&
                    polytronComp.recipeString == unboundSink.attractedRecipe
                );

            if (unboundPolytronMatchingUnboundSink != null)
            {

            }

        }
    }

#endif

    void BindPolytronsToSinks()
    {
        foreach (var sink in sinks)
        {
            PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();
            if (sinkComponent.boundPolytron != null) continue;

            foreach (var polytron in polytrons)
            {
                Polytron polytronComponent = polytron.GetComponent<Polytron>();

                if (boundPolytrons.Contains(polytron)) continue;
                if (polytronComponent.recipeString != sinkComponent.attractedRecipe) continue;

                sinkComponent.boundPolytron = polytronComponent;
                sink.GetComponent<MeshRenderer>().material.color = Color.red;
                boundPolytrons.Add(polytron);

                RebuildTile(sinkComponent.hexCoord, "C");


                break;
            }
        }
    }

#if VECCHIO
    void RebuildTile(GameObject sink, string recipe)
    {
        PolytronSink sinkComponent = sink?.GetComponent<PolytronSink>();
        if (sinkComponent != null)
        {
            GameObject tile = gridCellsMap[sinkComponent.hexCoord].tile;
            if (tile != null)
            {
                tile.GetComponent<PolyhedronGenerator>().recipeString = recipe;
                tile.GetComponent<PolyhedronGenerator>().RebuildMesh();
            }
        }
    }
#endif

    void RebuildTile(HexCoord coord, string recipe)
    {
            GameObject tile = gridCellsMap[coord].tile;
            if (tile != null)
            {
                tile.GetComponent<PolyhedronGenerator>().recipeString = recipe;
                tile.GetComponent<PolyhedronGenerator>().RebuildMesh();
            }
    }

    // bool polytronsToSinksBindingDone = false;

    void AttractPolytronsToSinks()
    {
        //if (!polytronsToSinksBindingDone)
        //{
        BindPolytronsToSinks();
        //polytronsToSinksBindingDone = true;
        //}

        foreach (var sink in sinks)
        {
            PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();
            if (sinkComponent.boundPolytron != null)
            {
                (Vector3 attractionForce, Vector3 from1To2Versor, float from1To2Distance) = CalcSpringForce(sinkComponent.boundPolytron.transform.position, sink.transform.position, sinkComponent.weight * 5f, 0.01f);
                sinkComponent.boundPolytron.GetComponent<Rigidbody>().AddForce(attractionForce);
            }
        }
    }

#if PRIMO_TENTATIVO
    void AttractPolytronsToSinks()
    {
        foreach (var polytron in polytrons)
        {
            if (boundPolytrons.Contains(polytron))
            {
                continue;
            }

            Rigidbody polytronRigidBodyComponent = polytron.GetComponent<Rigidbody>();
            Polytron polytronComponent = polytron.GetComponent<Polytron>();
            foreach (var sink in sinks)
            {
                PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();

                if (sinkComponent.boundPolytron != null)
                {
                    // Debug.Log("sink bound, skip");
                    continue;
                }

                if (polytronComponent.recipeString != sinkComponent.attractedRecipe)
                {
                    continue;
                }


                (Vector3 attractionForce, Vector3 from1To2Versor, float from1To2Distance) = CalcSpringForce(polytron.transform.position, sink.transform.position, sinkComponent.weight, 0.5f);

                if (from1To2Distance < 1f)
                {
                    // Debug.Log("sink bounded");
                    sinkComponent.boundPolytron = polytronComponent;
                    sink.GetComponent<MeshRenderer>().material.color = Color.red;
                    boundPolytrons.Add(polytron);
                    // polytronRigidBodyComponent.AddExplosionForce(100f, transform.position, 30f, 1f, ForceMode.Impulse);

                    /*
                                    foreach (var polytron2 in polytrons)
                                        {
                                            if (boundPolytrons.Contains(polytron2)) continue;
                                            //     (Vector3 attractionForce2, Vector3 from1To2Versor2, float from1To2Distance2) = CalcSpringForce(polytron.transform.position, sink.transform.position, 1f, 5f);
                                            if (Random.Range(0f, 1f) > 0.4f)
                                            {
                                                polytron2.GetComponent<Rigidbody>().AddExplosionForce(100f, transform.position, 30f, 1f, ForceMode.Impulse);
                                            }
                                        }
                    */


                }
                else
                {
                    polytronRigidBodyComponent.AddForce(attractionForce);
                }
            }
        }

        // int unboundSinks = 0;
        foreach (var sink in sinks)
        {
            PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();
            if (sinkComponent.boundPolytron != null)
            {
                (Vector3 attractionForce, Vector3 from1To2Versor, float from1To2Distance) = CalcSpringForce(sinkComponent.boundPolytron.transform.position, sink.transform.position, sinkComponent.weight * 5f, 0.01f);
                sinkComponent.boundPolytron.GetComponent<Rigidbody>().AddForce(attractionForce);

                /*
                foreach (var polytron in polytrons)
                {
                    if (boundPolytrons.Contains(polytron)) continue;
                    //     (Vector3 attractionForce2, Vector3 from1To2Versor2, float from1To2Distance2) = CalcSpringForce(polytron.transform.position, sink.transform.position, 1f, 5f);
                    if (Random.Range(0f, 1f) > 0.8f)
                    {
                        polytron.GetComponent<Rigidbody>().AddExplosionForce(100f, transform.position, 30f, 1f, ForceMode.Impulse);
                    }
                }
                */


            }
            else
            {
                // unboundSinks++;
            }
        }

        /*
                if (unboundSinks == 0)
                {
                    foreach (var polytron in polytrons)
                    {
                        if (!boundPolytrons.Contains(polytron))
                        {
                            polytron.GetComponent<Rigidbody>().AddForce(0, 1f, 0);
                        }
                    }
                }
        */

        foreach (var polytron in polytrons)
        {
            if (boundPolytrons.Contains(polytron)) continue;
            //     (Vector3 attractionForce2, Vector3 from1To2Versor2, float from1To2Distance2) = CalcSpringForce(polytron.transform.position, sink.transform.position, 1f, 5f);
            /*                    
                                if (Random.Range(0f, 1f) > 0.1f && (polytron.transform.position - transform.position).magnitude < 2f)
                        {
                            polytron.GetComponent<Rigidbody>().AddExplosionForce(100f, transform.position, 30f, 0f, ForceMode.Impulse);
                        }
                        */

            if (Random.Range(0f, 1f) > 0.999)
            {
                var unboundSink = sinks.Select(s => s.GetComponent<PolytronSink>())
                                      .FirstOrDefault(sinkComp => sinkComp.boundPolytron == null);

                if (unboundSink != null)
                {
                    unboundSink.GetComponent<PolytronSink>().boundPolytron = polytron.GetComponent<Polytron>();
                    unboundSink.GetComponent<MeshRenderer>().material.color = Color.red;
                    boundPolytrons.Add(polytron);
                }
            }
            
        }
    }
#endif



    public (Vector3 attractionForce, Vector3 from1To2Versor, float from1To2Distance)
        CalcSpringForce(Vector3 obj1pos, Vector3 obj2pos, float attractionMultiplier, float equilibriumDistance)
    {
        Vector3 from1to2Vector = obj2pos - obj1pos;
        float from1To2Distance = from1to2Vector.magnitude;

        float distanceFromEquilibrium = from1To2Distance - equilibriumDistance;

        Vector3 from1To2Versor = from1to2Vector.normalized;
        Vector3 attractionForce = from1To2Versor * distanceFromEquilibrium * attractionMultiplier;

        return (attractionForce, from1To2Versor, from1To2Distance);
    }


    void BuildLevel(int levelNumber)
    {

        // the level number will determine the Metatron complexity
        // and set actualRingsCount, etc.

        StartCoroutine(BuildMetatronCoroutine(actualRingsCount));
        StartCoroutine(BuildSinksAndTilesCoroutine(actualRingsCount));
        StartCoroutine(BuildPolytronsCoroutine());

    }

    /*
        IEnumerator BuildPavementCoroutine()
        {
            yield return BuildMetatronCoroutine(3);
            // it works
            // yield return new WaitForSeconds(4f);
            // yield return BuildMetatronCoroutine(4);
        }
    */

    IEnumerator BuildSinksAndTilesCoroutine(int ringsToBuild)
    {
        yield return new WaitForSeconds(1f);
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring <= ringsToBuild)
            {
                string recipe = "tC";
                if (hckv.Value.isOnMetatronPattern)
                {
                    recipe = "ttC";
                    if (hckv.Value.ring == 0 && hckv.Value.idxInRing == 0) recipe = "lI";
                    GameObject sink = PolytronsFactory.Instance.Create($"sink/{recipe}", 0.3f);
                    sink.transform.position = new Vector3(hckv.Value.worldCoords.x, 1.0f, hckv.Value.worldCoords.z);
                    hckv.Value.sink = sink;
                    sinks.Add(sink);
                    sink.GetComponent<PolytronSink>().weight = 0.2f;
                    sink.GetComponent<PolytronSink>().hexCoord = hckv.Key;
                    hckv.Value.currentState = true;
                    // yield return new WaitForSeconds(0.1f);
                }

                float angleToCenter = hckv.Key.PolarAngle();
                Quaternion tileRotation = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);

                GameObject tile = PolytronsFactory.Instance.Create($"tile/{recipe}", 1f);
                tile.transform.localScale = new Vector3(1f, 0.01f, 1f);
                tile.transform.position = hckv.Value.worldCoords + new Vector3(0, 0.01f, 0);
                tile.transform.localRotation = tileRotation;
                tiles.Add(tile);
                hckv.Value.tile = tile;

                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    IEnumerator BuildPolytronsCoroutine()
    {
        yield return new WaitForSeconds(1f);
        foreach (var hc in gridCellsMap)
        {
            if (hc.Value.ring == 12)
            {

                DrawCircle(hc.Value.worldCoords, 1.73f, Color.gray, 0.01f);
                yield return new WaitForSeconds(0.1f);

                float angleToCenter = hc.Key.PolarAngle();
                Quaternion polytronRotation = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);

                string recipe = "ttC";
                if (hc.Value.idxInRing % 12 == 0) recipe = "lT";

                GameObject polytron = PolytronsFactory.Instance.Create($"polytron/{recipe}", 0.4f);
                polytron.transform.position = hc.Value.worldCoords + new Vector3(0, 1f, 0);
                polytron.transform.localRotation = polytronRotation;
                polytrons.Add(polytron);

                yield return new WaitForSeconds(0.1f);

            }
        }

    }


    IEnumerator BuildMetatronCoroutine(int ringsToBuild)
    {
        // Draw circles
        foreach (HexCellData hcd in gridCellsMap.Values)
        {
            if (hcd.ring <= ringsToBuild)
            {
                if (hcd.isOnMetatronPattern)
                {
                    DrawCircle(hcd.worldCoords, 1.73f, Color.white, 0.025f);
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    DrawCircle(hcd.worldCoords, 1.73f, Color.gray, 0.01f);
                    yield return null;
                }

            }
        }

        yield return new WaitForSeconds(0.2f);

        // Draw hexagons
        for (int r = 1; r <= ringsToBuild; r++)
        {
            for (int i = 0; i < 6; i++)
            {
                int idxInRing = r * i;
                KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(r, idxInRing);
                KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(r, (idxInRing + r) % (r * 6));
                DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.gray, 0.02f);
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitForSeconds(0.2f);

        // Draw central cross
        for (int i = 0; i < 3; i++)
        {
            int idxInRing1 = (i * ringsToBuild);
            int idxInRing2 = ((i + 3) * ringsToBuild);
            KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(ringsToBuild, idxInRing1);
            KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(ringsToBuild, idxInRing2);
            DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.gray, 0.02f);
        }

        yield return new WaitForSeconds(0.2f);

        // Draw opposite equilateral triangles
        {
            // simplify.... :)
            // for (int r = ringsToBuild; r <= ringsToBuild; r++)
            //{
            int r = ringsToBuild;
            for (int i = 0; i < 2; i++)
            {
                int idxInRing1 = (i * r);
                int idxInRing2 = ((i + 2) * r);
                int idxInRing3 = ((i + 4) * r);
                KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(r, idxInRing1);
                KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(r, idxInRing2);
                KeyValuePair<HexCoord, HexCellData>? hc3 = FindCellByRingAndIdx(r, idxInRing3);
                DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.white, 0.045f);
                yield return new WaitForSeconds(0.1f);
                DrawLine(hc2.Value.Value.worldCoords, hc3.Value.Value.worldCoords, Color.white, 0.045f);
                yield return new WaitForSeconds(0.1f);
                DrawLine(hc3.Value.Value.worldCoords, hc1.Value.Value.worldCoords, Color.white, 0.045f);
                yield return new WaitForSeconds(0.1f);
            }
        }
        //}

        // Draw isosceles triangles
        for (int r = 2; r <= ringsToBuild; r++)
        {
            for (int i = 0; i < 6; i++)
            {
                for (int q = r - 1; q >= 1; q--)
                {
                    int idxInRing1 = (i * r);
                    int idxInRing2 = ((i + 2) * q) % (q * 6);
                    int idxInRing3 = ((i + 4) * q) % (q * 6);
                    KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(r, idxInRing1);
                    KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(q, idxInRing2);
                    KeyValuePair<HexCoord, HexCellData>? hc3 = FindCellByRingAndIdx(q, idxInRing3);
                    DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.gray, 0.01f);
                    DrawLine(hc2.Value.Value.worldCoords, hc3.Value.Value.worldCoords, Color.gray, 0.01f);
                    DrawLine(hc3.Value.Value.worldCoords, hc1.Value.Value.worldCoords, Color.gray, 0.01f);
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
    }

    void BuildMetatronImmediate(int ringsToBuild)
    {
        Dictionary<string, int> skippedLines = new();
        foreach (HexCellData hcd in gridCellsMap.Values)
        {
            if (hcd.isOnMetatronPattern && hcd.ring <= ringsToBuild)
            {
                DrawCircle(hcd.worldCoords, 1.73f, Color.blue);
            }
        }

        // create the hexagons. For each ring, the "corner" hexagons have coord (ring, ring * i) for i (0, 5)
        for (int r = 1; r <= ringsToBuild; r++)
        {
            for (int i = 0; i < 6; i++)
            {
                int idxInRing = r * i;
                KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(r, idxInRing);
                KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(r, (idxInRing + r) % (r * 6));
                DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.yellow, 0.05f);
            }
        }

        // create the central cross.
        // we must join the outer opposite corners two by two
        for (int i = 0; i < 3; i++)
        {
            int idxInRing1 = (i * ringsToBuild);
            int idxInRing2 = ((i + 3) * ringsToBuild);
            KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(ringsToBuild, idxInRing1);
            KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(ringsToBuild, idxInRing2);
            DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.blue, 0.05f);
        }


        // create the two opposite equilateral triangles on each ring
        for (int r = 1; r <= ringsToBuild; r++)
        {
            for (int i = 0; i < 2; i++)
            {
                int idxInRing1 = (i * r);
                int idxInRing2 = ((i + 2) * r);
                int idxInRing3 = ((i + 4) * r);
                KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(r, idxInRing1);
                KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(r, idxInRing2);
                KeyValuePair<HexCoord, HexCellData>? hc3 = FindCellByRingAndIdx(r, idxInRing3);
                DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.red, 0.05f);
                DrawLine(hc2.Value.Value.worldCoords, hc3.Value.Value.worldCoords, Color.red, 0.05f);
                DrawLine(hc3.Value.Value.worldCoords, hc1.Value.Value.worldCoords, Color.red, 0.05f);
            }
        }

        // create the isosceles triangles.
        for (int r = 2; r <= ringsToBuild; r++)
        {
            for (int i = 0; i < 6; i++)
            {
                // do it for all the internal cells
                for (int q = r - 1; q >= 1; q--)
                {

                    int idxInRing1 = (i * r);
                    int idxInRing2 = ((i + 2) * q) % (q * 6);
                    int idxInRing3 = ((i + 4) * q) % (q * 6);
                    KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(r, idxInRing1);
                    KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(q, idxInRing2);
                    KeyValuePair<HexCoord, HexCellData>? hc3 = FindCellByRingAndIdx(q, idxInRing3);
                    // CircleRenderer cr = new CircleRenderer();
                    DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.cyan, 0.05f);
                    DrawLine(hc2.Value.Value.worldCoords, hc3.Value.Value.worldCoords, Color.cyan, 0.05f);
                    DrawLine(hc3.Value.Value.worldCoords, hc1.Value.Value.worldCoords, Color.cyan, 0.05f);

                }
            }
        }
    }

    KeyValuePair<HexCoord, HexCellData>? FindCellByRingAndIdx(int ring, int idxInRing)
    {
        foreach (var kvp in gridCellsMap)
        {
            if (kvp.Value.ring == ring && kvp.Value.idxInRing == idxInRing)
            {
                return kvp;
            }
        }
        return null;
    }

    void BuildHexGridDataStructure()
    {
        Vector2 center2D = new Vector2(transform.position.x, transform.position.z);
        for (int ring = 0; ring <= maxRings; ring++)
        {
            int hexesInRing = ring == 0 ? 1 : 6 * ring;
            for (int i = 0; i < hexesInRing; i++)
            {
                HexCoord hex = ring == 0 ? new HexCoord(0, 0) : HexCoord.AtPolar(ring, i);
                // gridCellsList.Add(hex);

                Vector2 hexPos2D = hex.Position() * 2f + center2D;
                Vector3 position = new Vector3(hexPos2D.x, transform.position.y, hexPos2D.y);

                var cellData = new HexCellData
                {
                    ring = ring,
                    idxInRing = i,
                    worldCoords = position
                };

                if (IsMetatronCoord(ring, i))
                {
                    metatronCellsList.Add(hex);
                    cellData.isOnMetatronPattern = true;
                }

                gridCellsMap[hex] = cellData;
            }
        }
    }

    bool IsMetatronCoord(int ring, int idxInRing)
    {
        if (ring == 0 || ring == 1) return true;
        for (int i = 2; i < 10; i++)
        {
            if (ring == i && (idxInRing % i == 0)) return true;
        }
        return false;
    }

    void DrawCircle(Vector3 pos, float radius, Color color, float lineWidth = 0.05f, int segments = 20)
    {
        var go = new GameObject("circle");
        go.transform.SetParent(transform);

        //         var go = Instantiate(circlePrefab, pos, Quaternion.identity);
        //         lr = GetComponent<LineRenderer>();

        LineRenderer lr = go.AddComponent<LineRenderer>();
        // lr.useWorldSpace = false;   // così resta relativo all'oggetto
        lr.loop = true;             // chiude il cerchio
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        // Puoi cambiare materiale in Inspector (default = unlit/white)
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = color;


        /*
                var cd = go.GetComponent<CircleRenderer>();
                cd.radius = radius;
                cd.DrawCircle();
                */

        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, pos + new Vector3(x, 0f, y));
        }

    }

    void DrawLine(Vector3 start, Vector3 end, Color color, float width = 0.05f)
    {
        var go = new GameObject("Line");
        // GameObject go = Instantiate(GameObject, transform);
        go.transform.SetParent(transform);
        var lr = go.AddComponent<LineRenderer>();

        // lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
    }


    public void Evolve()
    {
        // Compute next state
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualRingsCount) continue;

            int aliveNeighbors = 0;
            for (int n = 0; n < 6; n++)
            {

                HexCoord neighbor = hckv.Key.Neighbor(n);

                if (gridCellsMap.ContainsKey(neighbor) && gridCellsMap[neighbor].currentState)
                    aliveNeighbors++;

            }

            HexCellData cellData = hckv.Value;
            bool alive = cellData.currentState;
            bool nextAlive = false;

            /*

                        // Game of Life rules for hex grid
                        if (alive)
                        {
                            // this is simmetrical with the Metatron initial scheme
                            // nextAlive = (aliveNeighbors == 3 || aliveNeighbors == 4);
                            nextAlive = (aliveNeighbors == 3 || aliveNeighbors == 6);
                        }
                        else
                        {
                            nextAlive = (aliveNeighbors == 2);
                        }
            */

            // rule for pattern breeder
            nextAlive = ((aliveNeighbors % 2) == 1) /* || cellData.ring == 0 */;

            cellData.nextState = nextAlive;
        }

        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualRingsCount) continue;
            hckv.Value.currentState = hckv.Value.nextState;
        }

        UpdateSinks();


    }

    void UpdateSinks()
    {

        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualRingsCount) continue;

            bool state = hckv.Value.currentState;
            if (state)
            {
                // sink.GetComponent<MeshRenderer>().material.color = Color.white;
                RebuildTile(hckv.Key, "ttC");
            }
            else
            {
                // sink.GetComponent<MeshRenderer>().material.color = Color.black;
                RebuildTile(hckv.Key, "C");
            }

        }

        /*
        foreach (var sink in sinks)
        {
            PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();

            bool state = gridCellsMap[sinkComponent.hexCoord].currentState;

            if (state)
            {
                sink.GetComponent<MeshRenderer>().material.color = Color.white;
                RebuildTile(sink, "ttC");
            }
            else
            {
                sink.GetComponent<MeshRenderer>().material.color = Color.black;
                RebuildTile(sink, "C");
            }

            // boundPolytrons.Add(polytron);

            //                 RebuildTile(sink, "C");
        }        
        */

    }
}