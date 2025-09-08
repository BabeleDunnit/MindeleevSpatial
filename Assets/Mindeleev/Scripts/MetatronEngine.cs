using System.Collections;
using System.Collections.Generic;
using SpatialSys.UnitySDK.Internal;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;
using System.Diagnostics.Tracing;
using System;

/* how does it works

we have an hexagonal grid, built entirely at start time but shown partly during the game. This creates an
hexagonal big grid with rings [0,12], so the external ring has 72 places.

we have a Pattern Breeder Cellular Automata working under the hood, with two different rules applied to alternate rings.
the Pattern Breeder uses the count of neighbour cells modulo 2 to decide if a cell will live or die, and using it on alternate
rings forces the cells on different rings to preferentially live or die, when the initial CA pattern is perfectly simmetrical.

initially the cells set to alive are the ones on the "metatron pattern", i.e. the ones on the diagonals.


*/


public class MetatronEngine : MonoBehaviour
{
    // public GameObject circlePrefab;
    int maxRings = 12;

    enum Behaviour { None, AttractPolytronsToSinks };
    Behaviour actualBehaviour = Behaviour.None;

    public class HexCellData
    {
        internal int ring;
        internal int idxInRing;
        internal Vector3 worldCoords;
        internal bool isOnMetatronPattern;
        internal GameObject sink;
        internal GameObject tile;
        // public GameObject polytron;
        internal GameObject circle;

        internal bool actualState = false;
        internal bool nextState = false;

        // each round it is alive, add +1. Each round it is dead, add -1.
        internal int aliveDeadCounter = 0;

        // eliminare, non va        
        // public List<int> aliveDeadCounterHistory = new();
        internal bool fusionFissionThresholdReached;
        internal int paused;
        internal string nextTileRecipe;

        // initially set to LevelConfig same value, and then decremented at each fission
        internal int tileIntToOperatorsSequenceOffset;

    }

    // these HexCoord lists and maps contains ALL the cells, prebuilt, rings [0,12]
    private Dictionary<HexCoord, HexCellData> gridCellsMap = new Dictionary<HexCoord, HexCellData>();

    // subset of gridCellsList, only and all the hexes on the Metatron pattern (a six braces cross)
    private List<HexCoord> metatronCellsList = new List<HexCoord>();

    // the lists of objects of the actual configuration
    private List<GameObject> polytrons = new();
    private HashSet<GameObject> boundPolytrons = new();
    private List<GameObject> sinks = new();
    private List<GameObject> tiles = new();

    // int actualRingsCount = 3;

    struct LevelConfig
    {
        // we will start with 2
        public int actualRingsCount;

        // public int fusionFissionThreshold;

        public int tileIntToOperatorsSequenceOffset;

        public string tileBasePoly;
        internal int fusionThreshold;
        internal int fissionThreshold;
    }

    LevelConfig actualLevelConfig;

    void Awake()
    {
    }

    void Create72Polytrons()
    {
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring == 12)
            {
                DrawCircle(hckv.Value, 1.73f, Color.gray, 0.01f);

                int polytronId = polytrons.Count;

                GameObject polytron = PolytronsFactory.Instance.Create($"polytron/T", 0.4f);
                polytron.transform.position = hckv.Value.worldCoords + new Vector3(0, 1f, 0);
                float angleToCenter = hckv.Key.PolarAngle();
                Quaternion polytronRotation = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);
                // polytron.transform.localRotation = polytronRotation;
                polytrons.Add(polytron);
            }
        }
    }

    void Start()
    {
        // hide placeholder
        GetComponent<MeshRenderer>().enabled = false;

        CreateHexGridDataStructure();
        Create72Polytrons();

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

                RebuildTileMesh(sinkComponent.hexCoord, "C");

                break;
            }
        }
    }

    void RebuildTileMesh(HexCoord coord, string recipe)
    {
        GameObject tile = gridCellsMap[coord].tile;
        if (tile != null && tile.GetComponent<PolyhedronGenerator>().recipeString != recipe)
        {
            tile.GetComponent<PolyhedronGenerator>().recipeString = recipe;
            tile.GetComponent<PolyhedronGenerator>().RebuildMesh();
        }
    }

    void RebuildPolytronMesh(GameObject p, string recipe)
    {
        p.GetComponent<PolyhedronGenerator>().recipeString = recipe;
        p.GetComponent<PolyhedronGenerator>().RebuildMesh();
    }

    void AttractPolytronsToSinks()
    {
        BindPolytronsToSinks();
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

        actualLevelConfig.actualRingsCount = 2;
        actualLevelConfig.fusionThreshold = 3;
        actualLevelConfig.fissionThreshold = 3;
        actualLevelConfig.tileBasePoly = "C";
        actualLevelConfig.tileIntToOperatorsSequenceOffset = 5;


        StartCoroutine(BuildMetatronCoroutine());
        StartCoroutine(BuildSinksCoroutine());
        StartCoroutine(BuildTilesCoroutine());
        StartCoroutine(ResetPolytronsCoroutine());

    }

    IEnumerator BuildSinksCoroutine()
    {
        yield return new WaitForSeconds(1f);
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring <= actualLevelConfig.actualRingsCount)
            {
                string recipe = "tC";
                if (hckv.Value.isOnMetatronPattern)
                {
                    //recipe = "ttC";
                    //if (hckv.Value.ring == 0 && hckv.Value.idxInRing == 0) recipe = "lI";
                    GameObject sink = PolytronsFactory.Instance.Create($"sink/{recipe}", 0.3f);
                    sink.transform.position = new Vector3(hckv.Value.worldCoords.x, 1.0f, hckv.Value.worldCoords.z);
                    hckv.Value.sink = sink;
                    sinks.Add(sink);
                    sink.GetComponent<PolytronSink>().weight = 0.2f;
                    sink.GetComponent<PolytronSink>().hexCoord = hckv.Key;
                    hckv.Value.actualState = true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
    }

    IEnumerator BuildTilesCoroutine()
    {
        yield return new WaitForSeconds(1.2f);
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring <= actualLevelConfig.actualRingsCount)
            {
                float angleToCenter = hckv.Key.PolarAngle();
                Quaternion tileRotation = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);

                // string tileRecipe = hckv.Value.actualState ? "ttC" : "C";
                string tileRecipe = hckv.Value.actualState ?
                    PolyhedronRecipeKabbalah.IntToOperatorsSequence(actualLevelConfig.tileIntToOperatorsSequenceOffset) + actualLevelConfig.tileBasePoly
                    :
                    PolyhedronRecipeKabbalah.IntToOperatorsSequence(actualLevelConfig.tileIntToOperatorsSequenceOffset - 1) + actualLevelConfig.tileBasePoly;

                GameObject tile = PolytronsFactory.Instance.Create($"tile/{tileRecipe}", 1f);
                tile.transform.localScale = new Vector3(1f, 0.01f, 1f);
                tile.transform.position = hckv.Value.worldCoords + new Vector3(0, 0.1f, 0);
                tile.transform.localRotation = tileRotation;
                tiles.Add(tile);
                hckv.Value.tile = tile;
                hckv.Value.tileIntToOperatorsSequenceOffset = actualLevelConfig.tileIntToOperatorsSequenceOffset;

                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    IEnumerator ResetPolytronsCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        int polytronIdx = 0;
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring == 12)
            {
                string recipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(polytronIdx) + "C";

                GameObject polytron = polytrons[polytronIdx];
                polytron.transform.position = hckv.Value.worldCoords + new Vector3(0, 1f, 0);
                float angleToCenter = hckv.Key.PolarAngle();
                Quaternion polytronRotation = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);
                polytron.transform.localRotation = polytronRotation;

                RebuildPolytronMesh(polytron, recipe);

                yield return new WaitForSeconds(0.1f);
                polytronIdx++;
            }
        }
    }

    IEnumerator BuildMetatronCoroutine()
    {
        // Draw circles
        foreach (HexCellData hcd in gridCellsMap.Values)
        {
            if (hcd.ring <= actualLevelConfig.actualRingsCount)
            {
                if (hcd.isOnMetatronPattern)
                {
                    DrawCircle(hcd, 1.73f, Color.white, 0.025f);
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    DrawCircle(hcd, 1.73f, Color.gray, 0.01f);
                    yield return null;
                }
            }
        }

        yield return new WaitForSeconds(0.2f);

        // Draw hexagons
        for (int r = 1; r <= actualLevelConfig.actualRingsCount; r++)
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
            int idxInRing1 = (i * actualLevelConfig.actualRingsCount);
            int idxInRing2 = ((i + 3) * actualLevelConfig.actualRingsCount);
            KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(actualLevelConfig.actualRingsCount, idxInRing1);
            KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(actualLevelConfig.actualRingsCount, idxInRing2);
            DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.gray, 0.02f);
        }

        yield return new WaitForSeconds(0.2f);

        // Draw opposite equilateral triangles
        {
            // simplify.... :)
            // for (int r = ringsToBuild; r <= ringsToBuild; r++)
            //{
            int r = actualLevelConfig.actualRingsCount;
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
        for (int r = 2; r <= actualLevelConfig.actualRingsCount; r++)
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
                DrawCircle(hcd, 1.73f, Color.blue);
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

    // inefficent, but only used in metatron build
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

    void CreateHexGridDataStructure()
    {
        Vector2 center2D = new Vector2(transform.position.x, transform.position.z);
        for (int ring = 0; ring <= maxRings; ring++)
        {
            int hexesInRing = ring == 0 ? 1 : 6 * ring;
            for (int i = 0; i < hexesInRing; i++)
            {
                HexCoord hex = ring == 0 ? new HexCoord(0, 0) : HexCoord.AtPolar(ring, i);

                Vector2 hexPos2D = hex.Position() * 2f + center2D;
                Vector3 position = new Vector3(hexPos2D.x, transform.position.y, hexPos2D.y);

                var cellData = new HexCellData
                {
                    ring = ring,
                    idxInRing = i,
                    worldCoords = position,
                    circle = CreateCircle(ring, i)
                };

                // test                
                if (ring == 2 && i == 1)
                {
                    cellData.actualState = true;
                }


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

    static Material sLineMat;
    static Material GetLineMat()
    {
        if (sLineMat == null) sLineMat = new Material(Shader.Find("Sprites/Default"));
        return sLineMat;
    }

    GameObject CreateCircle(int ring, int idxInRing)
    {
        var go = new GameObject($"circle_{ring}_{idxInRing}");
        go.transform.SetParent(transform);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.sharedMaterial = GetLineMat();
        lr.positionCount = 0;

        return go;
    }

    void DrawCircle(HexCellData hcd, float radius, Color color, float lineWidth = 0.05f, int segments = 20)
    {
        GameObject go = hcd.circle;
        LineRenderer lr = go.GetComponent<LineRenderer>();
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = lr.endColor = color;
        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, hcd.worldCoords + new Vector3(x, 0.1f, y));
        }
    }

    void DrawLine(Vector3 start, Vector3 end, Color color, float width = 0.05f)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(transform);
        var lr = go.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = lr.endWidth = width;
        lr.sharedMaterial = GetLineMat();
        lr.startColor = lr.endColor = color;
    }

    public void Evolve()
    {
        // Compute next state
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;
            if (hckv.Value.paused > 0)
            {
                hckv.Value.paused--;
                continue;
            }

            int aliveNeighbors = 0;
            for (int n = 0; n < 6; n++)
            {
                HexCoord neighbor = hckv.Key.Neighbor(n);

                if (gridCellsMap.ContainsKey(neighbor) && gridCellsMap[neighbor].actualState)
                    aliveNeighbors++;
            }

            HexCellData cellData = hckv.Value;
            bool alive = cellData.actualState;
            bool nextAlive = false;

            // rule for pattern breeder
            if (hckv.Value.ring % 2 == 0)
            {
                // this goes to zero
                nextAlive = ((aliveNeighbors % 2) == 1);
            }
            else
            {
                nextAlive = ((aliveNeighbors % 2) == 1);
            }

            cellData.nextState = nextAlive;
            cellData.aliveDeadCounter += cellData.nextState ? 1 : -1;
            // cellData.aliveDeadCounterHistory.Add(cellData.aliveDeadCounter);
        }

        // UpdateSinks();
        // in this point I can understand if the cell is born, dead, or remained alive or dead

        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;
            hckv.Value.actualState = hckv.Value.nextState;
        }

        DetectFusionFission();
        UpdateSinks();
        ResetFusionFission();

        UpdateTiles();

    }


    void DetectFusionFission()
    {
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

            DrawRubedoNigredoCircle(hckv);

            if (hckv.Value.fusionFissionThresholdReached)
            {
                if (hckv.Value.aliveDeadCounter > 0)
                {
                    // we were incrementing the energy, so now we must execute a fission

                    // count alive neighbors
                    int aliveNeighbors = 0;
                    for (int n = 0; n < 6; n++)
                    {
                        HexCoord neighbor = hckv.Key.Neighbor(n);
                        if (gridCellsMap.ContainsKey(neighbor) && gridCellsMap[neighbor].ring <= actualLevelConfig.actualRingsCount)
                        {
                            //HexCellData neighborCellData = gridCellsMap[neighbor];
                            //if (neighborCellData.actualState == true)
                            //{
                            aliveNeighbors++;
                            //}
                        }
                    }

                    string cellRecipe = hckv.Value.tile.GetComponent<PolyhedronGenerator>().recipeString;

                    List<string> fissionRecipes = PolyhedronRecipeKabbalah.RecipeFission(cellRecipe, aliveNeighbors + 1);
                    Debug.Log($"fission of cell at ring {hckv.Value.ring}, idxInRing {hckv.Value.idxInRing}, cell recipe: {cellRecipe}, aliveNeighbors: {aliveNeighbors}, recipes: {string.Join(", ", fissionRecipes)}");

                    hckv.Value.nextTileRecipe = fissionRecipes[0];
                    // hckv.Value.nextTileRecipe = "D";

                    if (hckv.Value.tileIntToOperatorsSequenceOffset > 2)
                    {
                        hckv.Value.tileIntToOperatorsSequenceOffset--;
                    }

                    // I cannot set the others cells recipes, i can set only mine...
                    /*
                                        int aliveNeighborIdx = 1;
                                        for (int n = 0; n < 6; n++)
                                        {
                                            HexCoord neighbor = hckv.Key.Neighbor(n);
                                            if (gridCellsMap.ContainsKey(neighbor) && gridCellsMap[neighbor].ring <= actualLevelConfig.actualRingsCount)
                                            {
                                                HexCellData neighborCellData = gridCellsMap[neighbor];
                                                //if (neighborCellData.actualState == true)
                                                //{                                
                                                // neighborCellData.tile.GetComponent<PolyhedronGenerator>().recipeString = fissionRecipes[aliveNeighborIdx++] + "C";
                    //                             neighborCellData.nextTileRecipe = fissionRecipes[aliveNeighborIdx++];
                                                neighborCellData.nextTileRecipe = "C";

                                                // neighborCellData.tile.GetComponent<PolyhedronGenerator>().recipeString = "C";
                                                //}
                                            }
                                        }
                                        */


                }
                else
                {
                    // we were decrementing the energy, so now we must execute a fusion

                    Debug.Log("starting fusion");
                    // sink is "born", make a fusion
                    List<string> recipesForFusion = new();
                    for (int n = 0; n < 6; n++)
                    {
                        HexCoord neighbor = hckv.Key.Neighbor(n);

                        if (gridCellsMap.ContainsKey(neighbor) && gridCellsMap[neighbor].ring <= actualLevelConfig.actualRingsCount)
                        {
                            HexCellData neighborCellData = gridCellsMap[neighbor];
                            recipesForFusion.Add(neighborCellData.tile.GetComponent<PolyhedronGenerator>().recipeString);
                        }
                    }
                    Debug.Log($"fusion recipes: {string.Join(", ", recipesForFusion)}");

                    string fusionRecipe = PolyhedronRecipeKabbalah.RecipeFusion(recipesForFusion);
                    Debug.Log($"fusion result: {fusionRecipe}");
                    hckv.Value.nextTileRecipe = fusionRecipe;


                }

            }
        }

        /*
                foreach (var hckv in gridCellsMap)
                {
                    if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;
                    if (hckv.Value.nextTileRecipe != null)
                    {
                        // hckv.Value.tile.GetComponent<PolyhedronGenerator>().recipeString = hckv.Value.nextTileRecipe;
                        // hckv.Value.nextTileRecipe = null;
                    }
               }
               */

    }


    void UpdateTiles()
    {
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;
            // if (hckv.Value.paused > 0) continue;

            // test
            if (hckv.Value.ring == 1)
            {
                Debug.Log($"ring: {hckv.Value.ring}, idxInRing: {hckv.Value.idxInRing}, nextState: {hckv.Value.nextState}, aliveDeadCounter: {hckv.Value.aliveDeadCounter}");
            }

            // RebuildTileMesh(hckv.Key, PolyhedronRecipeKabbalah.IntToOperatorsSequence(actualLevelConfig.tileIntToOperatorsSequenceOffset + hckv.Value.aliveDeadCounter) + actualLevelConfig.tileBasePoly);
            // RebuildTileMesh(hckv.Key, hckv.Value.tile.GetComponent<PolyhedronGenerator>().recipeString);

            if (hckv.Value.nextTileRecipe != null)
            {
                RebuildTileMesh(hckv.Key, hckv.Value.nextTileRecipe);
                hckv.Value.nextTileRecipe = null;
            }
            else
            {
                RebuildTileMesh(hckv.Key, PolyhedronRecipeKabbalah.IntToOperatorsSequence(hckv.Value.tileIntToOperatorsSequenceOffset + hckv.Value.aliveDeadCounter) + actualLevelConfig.tileBasePoly);
            }
        }
    }


    void ResetFusionFission()
    {
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;
            if (hckv.Value.fusionFissionThresholdReached)
            {
                hckv.Value.aliveDeadCounter = 0;
                hckv.Value.fusionFissionThresholdReached = false;
                hckv.Value.actualState = false;
                hckv.Value.paused = (actualLevelConfig.actualRingsCount - hckv.Value.ring + 2) * 3;
            }
        }
    }

    void DrawRubedoNigredoCircle(KeyValuePair<HexCoord, HexCellData> hckv)
    {

        float colorValue = hckv.Value.aliveDeadCounter;

        float normalizedColorValue = colorValue > 0 ?
            (float)colorValue / (float)actualLevelConfig.fusionThreshold
            :
            (float)colorValue / (float)actualLevelConfig.fissionThreshold;

        if (Math.Abs(normalizedColorValue) > 1f)
        {
            hckv.Value.fusionFissionThresholdReached = true;
        }

        normalizedColorValue = Mathf.Clamp(normalizedColorValue, -1f, 1f);
        Color color = Color.white;
        if (normalizedColorValue < 0f)
        {
            // da nero a bianco
            // t = -1 → 0; t = 0 → 1
            float u = normalizedColorValue + 1f; // mappa [-1,0] → [0,1]
            color = Color.Lerp(Color.black, Color.white, u);
        }
        else
        {
            // da bianco a rosso
            // t = 0 → 0; t = 1 → 1
            color = Color.Lerp(Color.white, Color.red, normalizedColorValue);
        }

        float lineWidth = hckv.Value.fusionFissionThresholdReached ? 0.2f : 0.04f;

        if (hckv.Value.isOnMetatronPattern)
        {
            // DrawCircle(hckv.Value, 1.73f - i * 0.5f, color, lineWidth);
            DrawCircle(hckv.Value, 1.73f, color, lineWidth);
        }
        else
        {
            DrawCircle(hckv.Value, 1.73f, color, lineWidth - 0.015f);
        }
    }


    void UpdateSinks()
    {
        foreach (var sink in sinks)
        {
            PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();

            HexCellData sinkCellData = gridCellsMap[sinkComponent.hexCoord];
            Debug.Assert(sinkCellData.sink == sink);
        }
    }




#if DACAPIRE
    void UpdateSinks()
    {
        foreach (var sink in sinks)
        {
            PolytronSink sinkComponent = sink.GetComponent<PolytronSink>();

            HexCellData sinkCellData = gridCellsMap[sinkComponent.hexCoord];
            Debug.Assert(sinkCellData.sink == sink);

            if (sinkCellData.actualState == false && sinkCellData.nextState == true)
            {

                // sinkCellData.aliveDeadCounter += 1;

                Debug.Log("starting fusion");
                // sink is "born", make a fusion
                List<string> recipesForFusion = new();
                for (int n = 0; n < 6; n++)
                {
                    HexCoord neighbor = sinkComponent.hexCoord.Neighbor(n);

                    if (gridCellsMap.ContainsKey(neighbor))
                    {
                        HexCellData neighborCellData = gridCellsMap[neighbor];
                        if (neighborCellData.sink != null)
                        {
                            recipesForFusion.Add(neighborCellData.sink.GetComponent<PolytronSink>().attractedRecipe);
                        }
                    }
                }
                Debug.Log($"fusion recipes: {string.Join(", ", recipesForFusion)}");

                string fusionRecipe = PolyhedronRecipeKabbalah.RecipeFusion(recipesForFusion);
                Debug.Log($"fusion result: {fusionRecipe}");

                // sinkComponent.attractedRecipe = fusionRecipe;

            }

            if (sinkCellData.actualState == true && sinkCellData.nextState == false)
            {
                // sink is "dead", make a fission
                // sinkCellData.aliveDeadCounter -= 1;
            }

            /*
                        if (sinkCellData.nextState == true)
                        {
                            sinkCellData.aliveDeadCounter += 1;
                        }
                        else
                        {
                            sinkCellData.aliveDeadCounter -= 1;
                        }
            */


            /*
                        if (state)
                        {
                            sink.GetComponent<MeshRenderer>().material.color = Color.white;
                        }
                        else
                        {
                            sink.GetComponent<MeshRenderer>().material.color = Color.black;
                        }
            */

        }
    }
#endif


}