using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using TMPro;

public class MutatronEngine : MonoBehaviour
{
    bool mustBuildFirstTime = true;
    bool isRebuildingLevel = false;

    int maxRings = 12;

    // all the 12 rings, prebuilt
    private Dictionary<HexCoord, HexCellData> gridCellsMap = new Dictionary<HexCoord, HexCellData>();

    // the 72 polytrons
    private List<Polytron> polytrons = new();

    // the polytrons homes
    private List<KeyValuePair<HexCoord, HexCellData>> polytronsHomes = new();

    private HexCellData mutatronCenter;

    // the Architron
    int architronIdx = 70;

    int evolveCount = 0;

    internal int currentlyHoveredPolytronSealNumber = -1;

    public class HexCellData
    {
        internal int ring;
        internal int idxInRing;
        internal Vector3 worldCoords;
        internal bool isOnMetatronPattern;
        internal PolytronSink sink;
        internal MutatronTile tile;
        internal GameObject circle;

        // the Polytronic Number also represents a quantified energy level in some way.
        // a polytron in this hexcell will get the appearance 
        // of RecipeKabbalah.IntToOperatorsSequence(polytronicNumber)
        internal int polytronicNumber;

        // the cellular automata accumulator for the next state of this cell
        internal int nextPolytronicNumberAccumulator;
        internal Range<int> fusionRange;

        // should this stay here? to be decided...
        internal string tileBasePolyhedron;
    }

    struct LevelConfig
    {

        // increased at every rebuild
        int levelCount;

        // we will start with 2
        public int actualRingsCount;
        internal int energyQuantumExchanged;
    }

    LevelConfig actualLevelConfig;

    void InitializeCellsForCurrentLevel()
    {
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring <= actualLevelConfig.actualRingsCount)
            {
                // must change based on actualLevelConfig
                hckv.Value.polytronicNumber = hckv.Value.ring;
                hckv.Value.nextPolytronicNumberAccumulator = 0;
                hckv.Value.fusionRange = new Range<int>(0, 6);
                hckv.Value.tileBasePolyhedron = "C";
            }
        }

        //FindCellByRingAndIdx(2, 2).Value.Value.polytronicNumber = 17;
        //FindCellByRingAndIdx(2, 3).Value.Value.polytronicNumber = 17;

        PrintDebugStats("End of InitializeCellsForCurrentLevel");
    }

    void SendAllPolytronsHome()
    {
        foreach (var polytron in polytrons)
        {
            if (polytron.boundSink)
            {
                polytron.boundSink.boundPolytron = null;
                //                 polytron.boundSink = null;
            }
            polytron.boundSink = null;
        }

        SendUnboundPolytronsHome();
        Debug_CheckAllPolytronsAreAtHome();
    }

    void Debug_CheckAllPolytronsAreAtHome()
    {
        Debug.Assert(polytronsHomes.Count == 72);
        int i = 0;
        foreach (var ph in polytronsHomes)
        {
            HexCellData hcd = ph.Value;
            PolytronSink sink = hcd.sink;
            Debug.Assert(sink.boundPolytron == polytrons[i]);
            Debug.Assert(sink.boundPolytron.boundSink == sink);
            i++;
        }
    }


    void BuildLevel(int levelNumber)
    {

        Debug.Log($"Building level {levelNumber}");

        isRebuildingLevel = true;

        // the level number will determine the Metatron complexity
        // and set actualRingsCount, etc.


        actualLevelConfig.actualRingsCount = 4 - levelNumber; // max con 72 polytroni se riempi tutto: 4
        actualLevelConfig.energyQuantumExchanged = 1;

        ResetLevelGraphics();
        SendAllPolytronsHome();
        InitializeCellsForCurrentLevel();


        StartCoroutine(DrawMetatronGraphicsCoroutine());
        StartCoroutine(BuildTilesCoroutine());

        mustBuildFirstTime = false;
        evolveCount = 0;
    }

    void AfterTilesCreation()
    {
        UpdatePolytronsSinks();

        // PolytronInfoPanel pip = polytrons[architronIdx].GetComponent<PolytronInfoPanel>();
        // pip.button4Text = "Make Architron";


        isRebuildingLevel = false;
    }

    void Start()
    {
        // hide placeholder
        GetComponent<MeshRenderer>().enabled = false;

        CreateHexGridDataStructure();
        Create72PolytronsImmediate();
        StartCoroutine(Create72PolytronsAndHomesCoroutine());
    }

    /*
        // questo non dovrebbe farlo uno per uno, altrimenti può essere che ci sia un polytrone che matcha 
        // ma arriva il suo turno troppo tardi per essere scelto
        // dovrebbe fare un giro globale prima di updatare i sink
        int polyCount = 0;
        Polytron ChoosePolytronToAssignToSink(KeyValuePair<HexCoord, HexCellData> hckv)
        {

            PolytronSink sink = hckv.Value.sink;
            Debug.Assert(sink);

            string tileRecipe = hckv.Value.tile.recipe;

            // is there any unbound polytron already with the sinkRecipe?
            var matchingRecipePolytrons = polytrons.Where(p => p.recipe == tileRecipe && p.boundSink == null).ToList();
            if (matchingRecipePolytrons.Count > 0)
            {
                return matchingRecipePolytrons[0];
            }

            if (polyCount >= 72) polyCount = 0;
            return polytrons[polyCount++];
        }
    */

    Polytron FindPolytronToBind()
    {
        // first try with polytrons at home
        var toReturn = polytrons.Where(p => p.boundSink != null && gridCellsMap[p.boundSink.hexCoord].ring == 12);

        // if no polytrons at home
        if (toReturn.Count() == 0)
        {
            // try with unbound polytrons
            toReturn = polytrons.Where(p => p.boundSink == null);
        }

        return toReturn.FirstOrDefault();
    }

    bool IsMutatronCenter(HexCellData cd)
    {
        // Debug.Assert(cd == mutatronCenter);
        return cd.ring == 0 && cd.idxInRing == 0;
    }

    // this is called to update the attraction of polytrons to sinks after each evolution round
    // and at the beginning of a new level
    void UpdatePolytronsSinks()
    {
        // when we call this, we have unbind all the polytrons from their non-matching tiles.
        // So there can be many polytrons already on the mutatron, but they are not bound to any sink.
        // Other are bound to some tiles that did not change and were obviously left untouched.

        // I must detect if any polytron can be recycled. I must do this globally, not one by one,
        // because I could claim an unbound polytron which could be recycled to be used as new
        // depending from the scan of the sequence of cells.
        // so, first I must collect all the tile recipes actually present on the Mutatron
        var tilesRecipes = gridCellsMap
            .Where(c => c.Value.ring <= actualLevelConfig.actualRingsCount)
            .Select(c => c.Value.tile.recipe)
            .ToList();

        // now collect all the unbound polytrons which are already set on a tile recipe which did not change 
        // (but maybe changed place) and bind them
        var matchingRecipeUnboundPolytrons = polytrons.Where(p => tilesRecipes.Contains(p.recipe) && p.boundSink == null && p.isArchitron == false);
        Debug.Log($"[UpdatePolytronsSinks] unbound polytrons with matching recipe: {matchingRecipeUnboundPolytrons.Count()}");

        int rebuiltPolytrons = 0;
        int movedPolytrons = 0;
        // now for each unbound polytron with a recipe matching at least one tile try to bind the polytron
        foreach (var matchingRecipeUnboundPolytron in matchingRecipeUnboundPolytrons)
        {
            foreach (var hckv in gridCellsMap)
            {
                if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

                // special treatment for the center of the Mutatron, reserved for the architron
                if (/*hckv.Value.ring == 0 && hckv.Value.idxInRing == 0*/ IsMutatronCenter(hckv.Value))
                {
                    // BindPolytronToSink(polytrons[architronIdx], hckv);
                    continue;
                }

                if (hckv.Value.sink.boundPolytron == null && hckv.Value.tile.recipe == matchingRecipeUnboundPolytron.recipe)
                {
                    BindPolytronToSink(matchingRecipeUnboundPolytron, hckv);
                    movedPolytrons++;
                    break;
                }
            }
        }

        // maybe not all the matchingRecipeUnboundPolytrons have been bind, because maybe there were not
        // enough matching sinks. Let us send them home to relax at the END of the ring 12
        foreach (var pp in matchingRecipeUnboundPolytrons.Where(p => p.boundSink == null))
        {
            var unboundSinkOnExternalRing = gridCellsMap.Where(hckv => hckv.Value.ring == 12 && hckv.Value.sink.boundPolytron == null).Last();
            BindPolytronToSink(pp, unboundSinkOnExternalRing);
        }

        // now I can proceed to bind and rebuild the remaining polytrons and sinks
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

            // special treatment for the center of the Mutatron, reserved for the architron.
            // we always force bind the Architron to the (0,0) cell.
            if (/*hckv.Value.ring == 0 && hckv.Value.idxInRing == 0*/ IsMutatronCenter(hckv.Value))
            {
                Polytron architron = polytrons[architronIdx];
                Debug.Assert(hckv.Value.sink.boundPolytron == null);
                BindPolytronToSink(architron, hckv);
                Debug.Assert(hckv.Value.sink.boundPolytron == architron);
                Debug.Assert(architron.boundSink = hckv.Value.sink);
                Debug.Assert(architron.boundSink.boundPolytron = architron);
                continue;
            }

            if (hckv.Value.sink.boundPolytron == null)
            {
                Polytron p = FindPolytronToBind();
                // there could be no more polytrons...
                if (p)
                {
                    BindPolytronToSink(p, hckv);
                    string tileRecipe = hckv.Value.tile.recipe;
                    // RebuildPolytronMesh(p, tileRecipe);
                    rebuiltPolytrons++;
                }
            }
        }

        Debug.Log($"[UpdatePolytronsSinks] moved: {movedPolytrons}, rebuilt: {rebuiltPolytrons}");
    }

    void RebuildPolytronMesh(Polytron p, string recipe)
    {
        p.recipe = recipe;
        p.RebuildMesh();
        // p.name = $"Polytron_{recipe}";
    }

    /*
        void BindPolytronToHome(Polytron p, KeyValuePair<HexCoord, HexCellData> hckv)
        {
            // every polytron has a home which will not change
            hckv.Value.tile.sealHome = p.sealNumber;
        }
    */

    void BindPolytronToSink(Polytron p, KeyValuePair<HexCoord, HexCellData> hckv)
    {
        // if the polytron is alread bound to an old sink, reset the bound polytron of that sink
        // if (p.boundSink) p.boundSink.boundPolytron = null;

        // now bound the polytron to this sink
        // PolytronSink sinkOfThisCell = hckv.Value.sink;

        /*
        p.boundSink = sinkOfThisCell;
        sinkOfThisCell.boundPolytron = p;
        */

        BindPolytronToSink(p, hckv.Value.sink);
    }

    void BindPolytronToSink(Polytron p, PolytronSink ps)
    {

        // if the polytron is alread bound to an old sink, reset the bound polytron of that sink
        if (p.boundSink)
        {
            Debug.Assert(p.boundSink.boundPolytron == p);
            p.boundSink.boundPolytron = null;
        }

        // and now bind the polytron and the sink
        p.boundSink = ps;
        ps.boundPolytron = p;
    }



    /*
        // nearly all polytrons have sinks as their targets, but the architron has a different behaviour
        void AttractPolytronsToTargets()
        {
            foreach (var hckv in gridCellsMap)
            {
                // if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

                PolytronSink sink = hckv.Value.sink;
                if (sink && sink.boundPolytron)
                {
                    (Vector3 attractionForce, Vector3 from1To2Versor, float from1To2Distance) = CalcSpringForce(sink.boundPolytron.transform.position, sink.transform.position, sink.weight * 5f, 0.01f);
                    sink.boundPolytron.GetComponent<Rigidbody>().AddForce(attractionForce);
                }

            }
        }
        */

    bool IsMutatronReady()
    {
        return !mustBuildFirstTime && !isRebuildingLevel;
    }

    void AttractPolytronsToTargets()
    {
        foreach (Polytron p in polytrons)
        {

            // special behaviour for the architron
            if (p.isArchitron)
            {
                // the Architron has a special, dynamic behaviour. 
                // if it is outside the Mutatron it will follow the avatar

                Vector3 avatarPos = CrossPlatformUtils.GetAvatarPosition();
                Vector3 architronPos = p.transform.position;

                // float avatarToArchitronDistance = (architronPos - avatarPos).magnitude;
                float avatarToMutatronDistance = (new Vector3(transform.position.x, 1f, transform.position.z) - avatarPos).magnitude;

                int architronBehaviour = -1;

                if (IsMutatronReady())
                {
                    float approxMutatronRadius = (actualLevelConfig.actualRingsCount + 1) * 3f;
                    if (avatarToMutatronDistance > approxMutatronRadius)
                    {
                        // the avatar is not enough near to the center of the Mutatron.
                        // lets make the architron follow him
                        architronBehaviour = 1;
                    }
                    else
                    {
                        // the avatar is near/inside the Mutatron. the Architron must move to the center of the Mutatron
                        architronBehaviour = 0;
                    }

                }
                else
                {
                    // mutatron not ready, the architron follows the avatar
                    architronBehaviour = 1;
                }

                Debug.Assert(architronBehaviour != -1);

                if (architronBehaviour == 1)
                {
                    // Calculate intersection point on sphere of radius 2.5 at height 1
                    Vector3 dir = (architronPos - avatarPos).normalized;
                    Vector3 targetOnSphere = avatarPos + dir * 2.5f;
                    targetOnSphere.y = 1.0f;

                    Vector3 forceToCircle = (targetOnSphere - architronPos) * 0.5f;
                    p.GetComponent<Rigidbody>().AddForce(forceToCircle);
                    continue;
                }
            }


            Debug.Assert(p.boundSink == null || p.boundSink.boundPolytron == p,
                $" polytron: {p?.name} , polytron.boundSink: {p.boundSink?.name}, polytron.boundSink.boundPolytron: {(p.boundSink?.boundPolytron == null ? "null" : p.boundSink?.boundPolytron.name)},");
            if (p.boundSink)
            {
                (Vector3 attractionForce, Vector3 from1To2Versor, float from1To2Distance) = CalcSpringForce(p.transform.position, p.boundSink.transform.position, p.boundSink.weight * 5f, 0.01f);
                p.GetComponent<Rigidbody>().AddForce(attractionForce);
            }
        }
    }

    IEnumerator Create72PolytronsAndHomesCoroutine()
    {
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring == 12)
            {
                DrawCircle(hckv.Value, 1.73f, Color.gray, 0.05f);

                int polytronId = hckv.Value.idxInRing;

                GameObject polytronGameObject = polytrons[hckv.Value.idxInRing].gameObject;
                polytronGameObject.transform.position = hckv.Value.worldCoords * 0.1f + new Vector3(0, 10f, 0);
                float angleToCenter = hckv.Key.PolarAngle();
                Quaternion rotationToCenter = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);

                Polytron polytronComponent = polytronGameObject.GetComponent<Polytron>();
                if (polytronComponent.sealNumber == architronIdx)
                {
                    polytronComponent.isArchitron = true;
                }

                //PolytronInfoPanel pip = polytronGameObject.GetComponent<PolytronInfoPanel>();
                // pip.button4Text = "Make Architron";


                GameObject tile = PolytronsFactory.Instance.Create($"tile/{polytronComponent.recipe}", 1f);
                tile.transform.localScale = new Vector3(1f, 0.01f, 1f);
                tile.transform.position = hckv.Value.worldCoords + new Vector3(0, 0.1f, 0);
                tile.transform.localRotation = rotationToCenter;
                tile.name += $"_home_{polytronComponent.sealNumber}";
                hckv.Value.tile = tile.GetComponent<MutatronTile>();

                GameObject label = CreateTileLabel($"{polytronId + 1}\n" + polytronComponent.sealName, tile.transform.position + new Vector3(0, 3, 0));
                int r1 = polytronId / 12;
                label.transform.rotation = Quaternion.Euler(0, (-60f * (r1 + 2)) + 180, 0);
                label.transform.position = tile.transform.position + new Vector3(0, 3.5f, 0);

                // bind the polytron to his home. The home will not change.
                // This makes all the polytrons being attracted to their homes at the start
                // of the game; the Architron has a behaviour override in AttractPolytronsToTargrets.
                // BindPolytronToHome(polytronGameObject.GetComponent<Polytron>(), hckv);
                BindPolytronToSink(polytronGameObject.GetComponent<Polytron>(), hckv);

//                 yield return new WaitForSeconds(0.15f);
                yield return new WaitForSeconds(0.01f);

                PolytronInfoPanel pip = polytronGameObject.GetComponent<PolytronInfoPanel>();
                if (polytronComponent.isArchitron == false)
                {
                    pip.button4Text = "Make Architron";
                }
            }
        }
    }

    void Create72PolytronsImmediate()
    {
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring == 12)
            {
                int polytronId = polytrons.Count;

                GameObject polytronGameObject = PolytronsFactory.Instance.Create($"polytron/T", 0.6f);
                polytronGameObject.transform.position = hckv.Value.worldCoords * 1f + new Vector3(0, -2f, 0);
                polytronGameObject.name += $"_{polytronId}";

                Polytron polytronComponent = polytronGameObject.GetComponent<Polytron>();
                polytronComponent.recipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(polytronId) + $"{(polytronId % 11):D2}" + "C";
                Debug.Log(polytronComponent.recipe);
                polytronComponent.RebuildMesh();
                Debug.Assert(polytronId == polytronComponent.sealNumber); // sealNumber is set by the factory

                polytrons.Add(polytronGameObject.GetComponent<Polytron>());

                polytronsHomes.Add(hckv);
            }
        }
    }



    /*
        static Color blueFloor
        {
            // [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Color(52, 56, 87, 255);
            }
        }
    */


    public static GameObject CreateTileLabel(string s, Vector3 position)
    {
        GameObject label = new GameObject($"Label_{s}");
        // label.transform.localPosition = Vector3.down * 1.5f;
        // label.transform.localPosition = position;

        TextMeshPro tmpText = label.AddComponent<TextMeshPro>();
        tmpText.text = s;
        tmpText.fontSize = 7;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = new Color(0.2f, 0.22f, 0.55f);
        // tmpText.enableAutoSizing = true;
        tmpText.fontSizeMin = 1;
        tmpText.fontSizeMax = 20;
        tmpText.material = new Material(Shader.Find("TextMeshPro/Mobile/Distance Field"));
        label.transform.localRotation = Quaternion.identity;
        var rectTransform = tmpText.GetComponent<RectTransform>();
        //         rectTransform.sizeDelta = new Vector2(2, 0.5f);
        rectTransform.sizeDelta = new Vector2(5, 1f);

        return label;
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

    void CreateHexGridDataStructure()
    {
        Vector2 center2D = new Vector2(transform.position.x, transform.position.z);
        for (int ring = 0; ring <= maxRings; ring++)
        {
            int hexesInRing = ring == 0 ? 1 : 6 * ring;
            for (int idxInRing = 0; idxInRing < hexesInRing; idxInRing++)
            {
                HexCoord hex = ring == 0 ? new HexCoord(0, 0) : HexCoord.AtPolar(ring, idxInRing);

                Vector2 hexPos2D = hex.Position() * 2f + center2D;
                Vector3 position = new Vector3(hexPos2D.x, transform.position.y, hexPos2D.y);

                var cellData = new HexCellData
                {
                    ring = ring,
                    idxInRing = idxInRing,
                    worldCoords = position,
                    circle = CreateCircle(ring, idxInRing)
                };

                // test                
                if (ring == 2 && idxInRing == 1)
                {
                    // cellData.actualState = true;

                    // AddPolytronDebug(cellData);
                }

                // cellData.sink = CreateSink()

                if (IsMetatronCoord(ring, idxInRing))
                {
                    // metatronCellsList.Add(hex);
                    cellData.isOnMetatronPattern = true;
                }

                gridCellsMap[hex] = cellData;

                if (ring == 0 && idxInRing == 0)
                {
                    mutatronCenter = cellData;
                }

            }
        }

        foreach (var hckv in gridCellsMap)
        {
            GameObject sink = CreateSink(hckv);
            sink.name += $"_{hckv.Value.ring}_{hckv.Value.idxInRing}";
        }

    }

    GameObject CreateSink(KeyValuePair<HexCoord, HexCellData> hckv)
    {
        string recipe = "tC";
        GameObject sink = PolytronsFactory.Instance.Create($"sink/{recipe}", 0.3f);

        // the central sink is a bit higher
        if (IsMutatronCenter(hckv.Value))
        {
            sink.transform.position = new Vector3(hckv.Value.worldCoords.x, 2f, hckv.Value.worldCoords.z);
        }
        else
        {
            sink.transform.position = new Vector3(hckv.Value.worldCoords.x, 1.0f, hckv.Value.worldCoords.z);
        }

        hckv.Value.sink = sink.GetComponent<PolytronSink>();
        hckv.Value.sink.weight = 0.2f;
        hckv.Value.sink.hexCoord = hckv.Key;
        hckv.Value.sink.GetComponent<MeshRenderer>().enabled = false;

        return sink;
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

    void DrawLine(Vector3 start, Vector3 end, Color color, float width = 0.05f)
    {
        var go = new GameObject("Line");
        //         go.tag = "Line";
        go.transform.SetParent(transform);
        var lr = go.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = lr.endWidth = width;
        lr.sharedMaterial = GetLineMat();
        lr.startColor = lr.endColor = color;
    }

    void ResetLevelGraphics()
    {
        foreach (HexCellData hcd in gridCellsMap.Values)
        {
            if (hcd.ring == 12) continue;

            if (hcd.circle)
            {
                GameObject.Destroy(hcd.circle);
            }

            hcd.circle = CreateCircle(hcd.ring, hcd.idxInRing);

            if (hcd.tile)
            {
                hcd.tile.GetComponent<MeshRenderer>().enabled = false;
                GameObject.Destroy(hcd.tile);
            }
        }

        GameObject[] all = GameObject.FindObjectsOfType<GameObject>();
        var lines = all.Where(go => go.name == "Line").ToArray();
        foreach (var line in lines)
        {
            GameObject.Destroy(line);
        }

    }

    IEnumerator DrawMetatronGraphicsCoroutine()
    {
        if (!mustBuildFirstTime)
        {
            yield return new WaitForSeconds(5.0f);
        }

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
                yield return new WaitForSeconds(0.05f);
                DrawLine(hc2.Value.Value.worldCoords, hc3.Value.Value.worldCoords, Color.white, 0.045f);
                yield return new WaitForSeconds(0.05f);
                DrawLine(hc3.Value.Value.worldCoords, hc1.Value.Value.worldCoords, Color.white, 0.045f);
                yield return new WaitForSeconds(0.05f);
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
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }
    }

    void PrintDebugStats(string header)
    {
        string msg = header + " Stats: \n";
        int totalQuantizedEnergy = 0;
        Dictionary<HexCellData, int> energyCellsMap = new();
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring <= actualLevelConfig.actualRingsCount)
            {
                totalQuantizedEnergy += hckv.Value.polytronicNumber;
                energyCellsMap.Add(hckv.Value, hckv.Value.polytronicNumber);
            }
        }

        var energyCellsList = new Dictionary<HexCellData, int>(energyCellsMap.OrderBy(kvp => kvp.Value).Reverse()).ToList();
        foreach (var kvp in energyCellsMap)
        {
            // msg += $"Cell (ring={kvp.Key.ring}, idxInRing={kvp.Key.idxInRing}) has polytronic number = {kvp.Value}\n";
        }

        msg += $"total quantized energy: {totalQuantizedEnergy}, most energy: {energyCellsList[0]}";

        Debug.Log(msg);

    }

    IEnumerator BuildTilesCoroutine()
    {

        if (!mustBuildFirstTime)
        {
            yield return new WaitForSeconds(5.5f);
        }

        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring <= actualLevelConfig.actualRingsCount)
            {
                float angleToCenter = hckv.Key.PolarAngle();
                Quaternion tileRotation = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);

                // string tileRecipe = hckv.Value.actualState ? "ttC" : "C";
                string tileRecipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(hckv.Value.polytronicNumber) + hckv.Value.tileBasePolyhedron;

                GameObject tile = PolytronsFactory.Instance.Create($"tile/{tileRecipe}", 1f);
                tile.name += $"_mutatron_{hckv.Value.ring}_{hckv.Value.idxInRing}";
                tile.transform.localScale = new Vector3(1f, 0.01f, 1f);
                tile.transform.position = hckv.Value.worldCoords + new Vector3(0, 0.1f, 0);
                tile.transform.localRotation = tileRotation;
                hckv.Value.tile = tile.GetComponent<MutatronTile>();

                yield return new WaitForSeconds(0.15f);
            }
        }

        AfterTilesCreation();
    }

    void UnbindNonMatchingPolytrons()
    {
        int polytronsThatWillNotMove = 0;
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

            PolytronSink sink = hckv.Value.sink;
            Polytron polytronBoundToSink = sink.boundPolytron;

            if (polytronBoundToSink && polytronBoundToSink.recipe != hckv.Value.tile.recipe)
            {
                polytronBoundToSink.boundSink = null;
                sink.boundPolytron = null;
                // polytronBoundToSink.NextPalette();
            }
            else
            {
                polytronsThatWillNotMove++;
            }
        }

        Debug.Log($"[UnbindNonMatchingPolytrons] polytronsThatWillNotMove: {polytronsThatWillNotMove}");
    }

    /*
        void SendUnboundPolytronsHome()
        {
            var unboundPolytrons = polytrons.Where(p => p.boundSink == null).ToList();
            var unboundSinksOnExternalRing = gridCellsMap.Where(hckv => hckv.Value.ring == 12 && hckv.Value.sink.boundPolytron == null).ToList();

            Debug.Assert(unboundSinksOnExternalRing.Count >= unboundPolytrons.Count);
            Debug.Log($"unboundPolytrons: {unboundPolytrons.Count}, unboundSinksOnExternalRing: {unboundSinksOnExternalRing.Count}");

            for (int i = 0; i < unboundPolytrons.Count; i++)
            {
                BindPolytronToSink(unboundPolytrons[i], unboundSinksOnExternalRing[i]);
            }
        }
        */

    void SendUnboundPolytronsHome()
    {
        var unboundPolytrons = polytrons.Where(p => p.boundSink == null).ToList();
        var unboundSinksOnExternalRing = gridCellsMap.Where(hckv => hckv.Value.ring == 12 && hckv.Value.sink.boundPolytron == null).ToList();

        Debug.Assert(unboundSinksOnExternalRing.Count >= unboundPolytrons.Count);
        Debug.Log($"unboundPolytrons: {unboundPolytrons.Count}, unboundSinksOnExternalRing: {unboundSinksOnExternalRing.Count}");

        for (int i = 0; i < unboundPolytrons.Count; i++)
        {
            BindPolytronToSink(unboundPolytrons[i], polytronsHomes[unboundPolytrons[i].sealNumber]);
        }
    }


    void Evolve()
    {

        if (!IsMutatronReady())
        {
            return;
        }

        // the idea: for each cell, count how many neighbors have a polytronicNumber higher than the one of the cell.
        // if the number of neighbors lies in the fusionRange, we have a fusion, and a quantum of energy moves from the polytronicNumber
        // of all the neighbors to the cell. 
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

            HexCellData cellData = hckv.Value;

            List<HexCellData> neighborsWithHigherPolytronicNumber = new();
            for (int n = 0; n < 6; n++)
            {
                HexCoord neighbor = hckv.Key.Neighbor(n);
                HexCellData neighborCellData = gridCellsMap[neighbor];
                if (neighborCellData.ring > actualLevelConfig.actualRingsCount) continue;

                if (neighborCellData.polytronicNumber > cellData.polytronicNumber)
                {
                    neighborsWithHigherPolytronicNumber.Add(neighborCellData);
                }
            }

            /*
                        if (cellData.fusionRange.Contains(neighborsWithHigherPolytronicNumber.Count))
                        {
                            // we have a fusion. The neighbors release one quantum of energy
                            cellData.nextPolytronicNumberAccumulator += (neighborsWithHigherPolytronicNumber.Count * actualLevelConfig.energyQuantumExchanged);
                            foreach (var neighborCellData in neighborsWithHigherPolytronicNumber) { neighborCellData.nextPolytronicNumberAccumulator -= actualLevelConfig.energyQuantumExchanged; }
                        }
                        else
                        {
                            cellData.nextPolytronicNumberAccumulator--;
                        }
                        */


            if (neighborsWithHigherPolytronicNumber.Count % 2 == 0)
            {
                // we have a fusion. The neighbors release one quantum of energy
                cellData.nextPolytronicNumberAccumulator += (neighborsWithHigherPolytronicNumber.Count * 2);
                foreach (var neighborCellData in neighborsWithHigherPolytronicNumber) { neighborCellData.nextPolytronicNumberAccumulator -= 1; }
            }
            else
            {
                cellData.nextPolytronicNumberAccumulator--;
            }

        }

        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

            HexCellData cellData = hckv.Value;
            cellData.polytronicNumber += cellData.nextPolytronicNumberAccumulator;
            // if (cellData.polytronicNumber < 0) cellData.polytronicNumber = 0;
            cellData.nextPolytronicNumberAccumulator = 0;
        }

        UpdateTiles();

        UnbindNonMatchingPolytrons();

        UpdatePolytronsSinks();

        SendUnboundPolytronsHome();

        evolveCount++;

        PrintDebugStats($"End of Evolve() call #{evolveCount}");

    }

    void RebuildTileMesh(HexCoord coord, string recipe)
    {
        PolyhedronGenerator tile = gridCellsMap[coord].tile;
        if (tile != null && tile.recipe != recipe)
        {
            tile.recipe = recipe;
            tile.RebuildMesh();

            // PolytronsFactory.CreateLabel(tile.gameObject, tile.recipeString, Vector3.up * 10.5f);
        }
    }

    void UpdateTiles()
    {
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

            HexCellData cellData = hckv.Value;

            string tileRecipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(hckv.Value.polytronicNumber) + hckv.Value.tileBasePolyhedron;

            RebuildTileMesh(hckv.Key, tileRecipe);

        }
    }


    void FixedUpdate()
    {
        AttractPolytronsToTargets();
    }

    int levelCount = 0;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            BuildLevel(levelCount++);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Evolve();
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

    bool IsHome(HexCellData hcd)
    {
        return hcd.ring == 12;
    }

    bool IsMutatronCell(HexCellData hcd)
    {
        return hcd.ring <= actualLevelConfig.actualRingsCount;
    }

    bool IsHome(HexCoord hc)
    {
        return gridCellsMap[hc].ring == 12;
    }

    bool IsMutatronCell(HexCoord hc)
    {
        return gridCellsMap[hc].ring <= actualLevelConfig.actualRingsCount;
    }

    internal void SetNewArchitron(int newArchitronIdx)
    {
        Debug.Log($"changing Architron, old: {architronIdx}, new: {newArchitronIdx}");

        Polytron actualArchitron = polytrons[architronIdx];
        Polytron newArchitron = polytrons[newArchitronIdx];

        Debug.Assert(actualArchitron.isArchitron == true);
        Debug.Assert(newArchitron.isArchitron == false);

        PolytronSink actualArchitronSink = actualArchitron.boundSink;
        PolytronSink newArchitronSink = newArchitron.boundSink;

        // detect if the newly selected architron is at home or on the Mutatron
        bool isNewArchitronAtHome = IsHome(newArchitron.boundSink.hexCoord);

        if (isNewArchitronAtHome)
        {

            // the actual architron must be sent back to his home
            BindPolytronToSink(actualArchitron, polytronsHomes[actualArchitron.sealNumber].Value.sink);
            // and the new architron must be bound to the metatron center
            BindPolytronToSink(newArchitron, mutatronCenter.sink);
        }
        else
        {
            actualArchitronSink.boundPolytron = newArchitron;
            actualArchitron.boundSink = newArchitronSink;

            newArchitronSink.boundPolytron = actualArchitron;
            newArchitron.boundSink = actualArchitronSink;
        }


        actualArchitron.isArchitron = false;
        newArchitron.isArchitron = true;

        architronIdx = newArchitronIdx;

        newArchitron.GetComponent<PolytronInfoPanel>().button4Text = "";
        actualArchitron.GetComponent<PolytronInfoPanel>().button4Text = "Make Architron";

        Debug.Assert(actualArchitron.isArchitron == false);
        Debug.Assert(newArchitron.isArchitron == true);

        Debug.Assert(gridCellsMap[newArchitron.boundSink.hexCoord].ring == 0);
        Debug.Assert(gridCellsMap[newArchitron.boundSink.hexCoord].idxInRing == 0);

        Debug.Assert(actualArchitron.boundSink.boundPolytron == actualArchitron);
        Debug.Assert(newArchitron.boundSink.boundPolytron == newArchitron);

    }

    string oldArchitronRecipe;

    internal void OnPolytronPointerEnter(int sealNum)
    {
        currentlyHoveredPolytronSealNumber = sealNum;

        // if I am hovering on a polytron which is not the Architron, get its recipe
        Polytron hoveredPolytron = polytrons[sealNum];
        if (hoveredPolytron.isArchitron == false)
        {
            string newArchitronRecipe = hoveredPolytron._recipe.PaletteIdx.ToString("D2") + hoveredPolytron._recipe.BasePolyhedron;
            oldArchitronRecipe = polytrons[architronIdx].recipe;
            polytrons[architronIdx].recipe = newArchitronRecipe;
            polytrons[architronIdx].RebuildMesh();
        }
    }

    internal void OnPolytronPointerExit(int sealNum)
    {
        currentlyHoveredPolytronSealNumber = -1;

        Polytron hoveredPolytron = polytrons[sealNum];
        if (hoveredPolytron.isArchitron == false)
        {
            polytrons[architronIdx].recipe = oldArchitronRecipe;
            polytrons[architronIdx].RebuildMesh();
        }

    }

    // Selection slots
    private Polytron paletteSelector = null;    // state 1: fix palette index + base polyhedron
    private Polytron operatorsSelector = null;  // state 2: fix operator sequence

    // Architron backup recipes for restore semantics
    private string architronSavedRecipeForSelection = null;   // saved when first selection is made (for permanent restore)
    private string architronHoverSavedRecipe = null;         // saved when hover preview starts
    private bool architronHoverOverrideActive = false;

    // Helper to clear palette selection (reset outline on previous)
    void ClearPaletteSelection()
    {
        if (paletteSelector != null)
        {
            var oldOutline = paletteSelector.GetComponent<PointerOutlineStateController>();
            oldOutline?.SetState(0);
            paletteSelector = null;
        }
    }

    // Helper to clear operators selection (reset outline on previous)
    void ClearOperatorsSelection()
    {
        if (operatorsSelector != null)
        {
            var oldOutline = operatorsSelector.GetComponent<PointerOutlineStateController>();
            oldOutline?.SetState(0);
            operatorsSelector = null;
        }
    }

    // Called by Polytron on click
    internal void OnPolytronClicked(Polytron p)
    {
        if (p == null || p.isArchitron) return;

        var pOutline = p.GetComponent<PointerOutlineStateController>();

        // New selection rules:
        // - At most one paletteSelector and one operatorsSelector.
        // - If only one slot is occupied and you click the same polytron, cycle 0 -> 1 -> 2 -> 0.
        // - If both slots are occupied and you click a selected polytron, deselect that slot.
        // - Clicking an unselected polytron assigns it to the first free slot (palette preferred).
        // - If both occupied and you click an unselected polytron, replace the operators slot.

        if (paletteSelector == p)
        {
            // Clicked the polytron that is currently the palette selection.
            if (operatorsSelector == null)
            {
                // Only palette selected -> cycle it to operators.
                ClearPaletteSelection();
                operatorsSelector = p;
                pOutline?.SetState(2); // operators (green)
            }
            else
            {
                // Both selected -> clicking the palette-selected polytron should deselect it.
                ClearPaletteSelection();
            }
        }
        else if (operatorsSelector == p)
        {
            // Clicked the polytron that is currently the operators selection.
            // Regardless of the other slot, clicking an operators-selected polytron should deselect it.
            ClearOperatorsSelection();
        }
        else
        {
            // Clicked a polytron that is not currently selected
            if (paletteSelector == null)
            {
                paletteSelector = p;
                pOutline?.SetState(1);
            }
            else if (operatorsSelector == null)
            {
                operatorsSelector = p;
                pOutline?.SetState(2);
            }
            else
            {
                // both slots occupied: replace the operators slot with the clicked polytron
                ClearOperatorsSelection();
                operatorsSelector = p;
                pOutline?.SetState(2);
            }
        }

        // Save architron original recipe once when first selection appears
        var arch = polytrons[architronIdx];
        if ((paletteSelector != null || operatorsSelector != null) && architronSavedRecipeForSelection == null)
            architronSavedRecipeForSelection = arch.recipe;

        // If both slots present -> set architron permanently to their union
        if (paletteSelector != null && operatorsSelector != null)
        {
            string combined = CombineUsingSelectors(paletteSelector, operatorsSelector);
            ApplyRecipeToArchitron(combined);
        }
        else
        {
            // If both cleared -> restore saved pre-selection recipe
            if (paletteSelector == null && operatorsSelector == null && architronSavedRecipeForSelection != null)
            {
                ApplyRecipeToArchitron(architronSavedRecipeForSelection);
                architronSavedRecipeForSelection = null;
            }
            // Single-slot behavior remains transient and handled on hover
        }

        Debug.Log($"[MutatronEngine.OnPolytronClicked] palette: {(paletteSelector == null ? "null" : paletteSelector.name)}, operators: {(operatorsSelector == null ? "null" : operatorsSelector.name)}");
    }

    // Build combined recipe string from two selected Polytrons:
    // paletteSelector provides PaletteIdx and BasePolyhedron,
    // operatorsSelector provides OperatorsSequence().
    private string CombineUsingSelectors(Polytron paletteP, Polytron opsP)
    {
        string ops = opsP._recipe?.OperatorsSequence() ?? "";
        string paletteIdxStr = paletteP._recipe?.PaletteIdx.ToString("D2") ?? "00";
        char baseChar = paletteP._recipe?.BasePolyhedron ?? 'C';
        return ops + paletteIdxStr + baseChar;
    }

    // Apply string recipe to the architron (set recipe and rebuild)
    private void ApplyRecipeToArchitron(string recipe)
    {
        var arch = polytrons[architronIdx];
        if (arch == null) return;
        if (arch.recipe == recipe) return;
        arch.recipe = recipe;
        arch.RebuildMesh();
        arch.name = $"Architron_{recipe}";
    }

    // Called by Polytron on pointer enter
    internal void OnPolytronPointerEnter(Polytron hovered)
    {
        if (hovered == null || hovered.isArchitron) return;

        var arch = polytrons[architronIdx];

        // If hovering a selected polytron -> temporarily show its radix (palette + base)
        if (hovered == paletteSelector || hovered == operatorsSelector)
        {
            // save current arch recipe once for restore
            if (!architronHoverOverrideActive)
            {
                architronHoverSavedRecipe = arch.recipe;
                architronHoverOverrideActive = true;
            }

            string radix = GetRadixRecipe(hovered);
            ApplyRecipeToArchitron(radix);
            return;
        }

        // If no selection slots active, do nothing (info panel / outline handled by Polytron)
        if (paletteSelector == null && operatorsSelector == null) return;

        // Save pre-hover arch recipe once
        if (!architronHoverOverrideActive)
        {
            architronHoverSavedRecipe = arch.recipe;
            architronHoverOverrideActive = true;
        }

        string newRecipe = null;

        if (paletteSelector != null && operatorsSelector != null)
        {
            // both fixed -> permanent combination (already applied on click) but ensure consistency
            newRecipe = CombineUsingSelectors(paletteSelector, operatorsSelector);
        }
        else if (paletteSelector != null)
        {
            // palette fixed: hovered provides operators
            string hoveredOps = hovered._recipe?.OperatorsSequence() ?? "";
            string paletteIdxStr = paletteSelector._recipe?.PaletteIdx.ToString("D2") ?? "00";
            char baseChar = paletteSelector._recipe?.BasePolyhedron ?? 'C';
            newRecipe = hoveredOps + paletteIdxStr + baseChar;
        }
        else // operatorsSelector != null
        {
            // operators fixed: hovered provides palette/base
            string ops = operatorsSelector._recipe?.OperatorsSequence() ?? "";
            string paletteIdxStr = hovered._recipe?.PaletteIdx.ToString("D2") ?? "00";
            char baseChar = hovered._recipe?.BasePolyhedron ?? 'C';
            newRecipe = ops + paletteIdxStr + baseChar;
        }

        if (newRecipe != null)
            ApplyRecipeToArchitron(newRecipe);
    }

    // Called by Polytron on pointer exit
    internal void OnPolytronPointerExit(Polytron p)
    {
        var arch = polytrons[architronIdx];

        if (!architronHoverOverrideActive) return;

        // restore architron to either selection-based permanent recipe (if both slots set)
        // or to the saved pre-hover recipe (if only hover was active)
        if (paletteSelector != null && operatorsSelector != null)
        {
            string combined = CombineUsingSelectors(paletteSelector, operatorsSelector);
            ApplyRecipeToArchitron(combined);
        }
        else
        {
            // if a selection-based saved recipe exists (user had selected something before)
            if (architronSavedRecipeForSelection != null)
            {
                ApplyRecipeToArchitron(architronSavedRecipeForSelection);
            }
            else
            {
                // restore the recipe saved before hover
                if (architronHoverSavedRecipe != null)
                {
                    ApplyRecipeToArchitron(architronHoverSavedRecipe);
                }
            }
        }

        architronHoverOverrideActive = false;
        architronHoverSavedRecipe = null;
    }

    // Return a recipe string representing only the radix (palette index + base polyhedron)
    private string GetRadixRecipe(Polytron p)
    {
        if (p == null || p._recipe == null) return "";
        string paletteIdxStr = p._recipe.PaletteIdx.ToString("D2");
        char baseChar = p._recipe.BasePolyhedron;
        return paletteIdxStr + baseChar;
    }
}
