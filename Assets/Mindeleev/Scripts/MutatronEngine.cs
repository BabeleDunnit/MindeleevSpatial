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

        PrintDebugStats("End of InitializeCellsForCurrentLevel");
    }

    void SendAllPolytronsHome()
    {
        foreach (var polytron in polytrons)
        {
            if (polytron.boundSink)
            {
                polytron.boundSink.boundPolytron = null;
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
        DeselectAllPolytrons();
        UpdatePolytronsSinks();

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
            if (IsMutatronCenter(hckv.Value))
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
                    RebuildPolytronFromRecipe(p, tileRecipe);
                    rebuiltPolytrons++;
                }
            }
        }

        Debug.Log($"[UpdatePolytronsSinks] moved: {movedPolytrons}, rebuilt: {rebuiltPolytrons}");
    }

    void BindPolytronToSink(Polytron p, KeyValuePair<HexCoord, HexCellData> hckv)
    {
        BindPolytronToSink(p, hckv.Value.sink);
    }

    void BindPolytronToSink(Polytron p, PolytronSink ps)
    {
        if (p.boundSink)
        {
            Debug.Assert(p.boundSink.boundPolytron == p);
            p.boundSink.boundPolytron = null;
        }

        p.boundSink = ps;
        ps.boundPolytron = p;
    }

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

                BindPolytronToSink(polytronGameObject.GetComponent<Polytron>(), hckv);

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
                polytronComponent.RebuildMesh();

                Debug.Assert(polytronId == polytronComponent.sealNumber); // sealNumber is set by the factory

                polytrons.Add(polytronGameObject.GetComponent<Polytron>());

                polytronsHomes.Add(hckv);
            }
        }
    }

    void RebuildPolytronFromRecipe(Polytron p, string r)
    {
        p.recipe = r;
        p.RebuildMesh();
        p.GetComponent<PolytronInfoPanel>().bodyText = r;
    }

    public static GameObject CreateTileLabel(string s, Vector3 position)
    {
        GameObject label = new GameObject($"Label_{s}");

        TextMeshPro tmpText = label.AddComponent<TextMeshPro>();
        tmpText.text = s;
        tmpText.fontSize = 7;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = new Color(0.2f, 0.22f, 0.55f);
        tmpText.fontSizeMin = 1;
        tmpText.fontSizeMax = 20;
        tmpText.material = new Material(Shader.Find("TextMeshPro/Mobile/Distance Field"));
        label.transform.localRotation = Quaternion.identity;
        var rectTransform = tmpText.GetComponent<RectTransform>();
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

        msg += $"total quantized energy: {totalQuantizedEnergy}, most energy: {(energyCellsList.Count>0?energyCellsList[0].ToString():"none")}";
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
            }
            else
            {
                polytronsThatWillNotMove++;
            }
        }

        Debug.Log($"[UnbindNonMatchingPolytrons] polytronsThatWillNotMove: {polytronsThatWillNotMove}");
    }

    void SendUnboundPolytronsHome()
    {
        var unboundPolytrons = polytrons.Where(p => p.boundSink == null).ToList();
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

        DeselectAllPolytrons();

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
        AttractGeneticFriends(); // apply attraction to genetic friends if any
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

    // Selection slots
    private Polytron paletteSelector = null;    // state 1: fix palette index + base polyhedron (yellow)
    private Polytron operatorsSelector = null;  // state 2: fix operator sequence (green)

    // Architron backup recipes for restore semantics
    private string architronSavedRecipeForSelection = null;   // saved when first selection is made (for permanent restore)
    private string architronHoverSavedRecipe = null;         // saved when hover preview starts
    private bool architronHoverOverrideActive = false;

    // --- Genetic / friends state ---
    private struct PolytronStateBackup
    {
        public string recipe;
        public PolytronSink boundSink;
        public Vector3 position;
    }

    private bool geneticModeActive = false;
    private List<Polytron> geneticFriends = new List<Polytron>();
    private Dictionary<Polytron, PolytronStateBackup> geneticBackups = new Dictionary<Polytron, PolytronStateBackup>();
    // Targets previously were absolute; now we keep relative offsets (around architron) for active genetic attraction
    private Dictionary<Polytron, Vector3> geneticRelativeOffsets = new Dictionary<Polytron, Vector3>();
    // When friends are being returned to their original positions (on dismissal), we use absolute return targets
    private Dictionary<Polytron, Vector3> geneticReturnTargets = new Dictionary<Polytron, Vector3>();
    private HashSet<Polytron> geneticReturning = new HashSet<Polytron>();

    private float geneticFriendsRadius = 4f;
    private float geneticFriendsHeight = 2f;
    private float geneticAttractionStrength = 5f;

    // Helper to clear palette selection (reset outline on previous)
    void ClearPaletteSelection()
    {
        ClearGeneticFriends();

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
        ClearGeneticFriends();

        if (operatorsSelector != null)
        {
            var oldOutline = operatorsSelector.GetComponent<PointerOutlineStateController>();
            oldOutline?.SetState(0);
            operatorsSelector = null;
        }
    }

    // Deselect all polytrons: remove outlines, reset selection state and hover state,
    // and restore the Architron to the "no selection" recipe (saved before selection) and rebuild it.
    internal void DeselectAllPolytrons()
    {
        Debug.Log("[MutatronEngine] DeselectAllPolytrons()");

        ClearGeneticFriends();

        // remove outlines on every polytron to ensure no visual selection remains
        foreach (var p in polytrons)
        {
            if (p == null) continue;
            var outline = p.GetComponent<PointerOutlineStateController>();
            outline?.SetState(0);
        }

        // reset selection slots
        paletteSelector = null;
        operatorsSelector = null;

        // reset hover bookkeeping
        architronHoverOverrideActive = false;
        architronHoverSavedRecipe = null;
        currentlyHoveredPolytronSealNumber = -1;

        // restore Architron recipe: prefer the saved pre-selection recipe if present,
        // otherwise restore the pre-hover recipe if available.
        var arch = polytrons[architronIdx];
        if (arch == null) return;

        if (!string.IsNullOrEmpty(architronSavedRecipeForSelection))
        {
            ApplyRecipeToArchitron(architronSavedRecipeForSelection);
            architronSavedRecipeForSelection = null;
        }
        else if (!string.IsNullOrEmpty(architronHoverSavedRecipe))
        {
            ApplyRecipeToArchitron(architronHoverSavedRecipe);
            architronHoverSavedRecipe = null;
        }
        else
        {
            ApplyRecipeToArchitron(arch.recipe);
        }
    }

    // Called by Polytron on click
    internal void OnPolytronClicked(Polytron p)
    {
        if (p == null || p.isArchitron) return;

        var pOutline = p.GetComponent<PointerOutlineStateController>();

        // Ensure any previous genetic friends are cleared before changing selection.
        ClearGeneticFriends();

        // New selection rules:
        // - At most one paletteSelector and one operatorsSelector.
        // - If only one slot is occupied and you click the same polytron, cycle 0 -> 1 -> 2 -> 0.
        // - If both slots are occupied and you click a selected polytron, deselect that slot.
        // - Clicking an unselected polytron assigns it to the first free slot (palette preferred).
        // - If both occupied and you click an unselected polytron, replace the operators slot.

        if (paletteSelector == p)
        {
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
            // Clicking the operators-selected polytron toggles it off.
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

        // If both slots present -> set architron permanently to their union or activate genetic mode
        if (paletteSelector != null && operatorsSelector != null)
        {
            if (paletteSelector.recipe == operatorsSelector.recipe)
            {
                string combined = CombineUsingSelectors(paletteSelector, operatorsSelector);
                ApplyRecipeToArchitron(combined);
            }
            else
            {
                var crossovers = ComputeCrossoverRecipes(paletteSelector, operatorsSelector);
                if (crossovers.Count == 1)
                {
                    ApplyRecipeToArchitron(crossovers[0]);
                }
                else
                {
                    SetupGeneticFriends(crossovers);
                }
            }
        }
        else
        {
            if (paletteSelector == null && operatorsSelector == null && architronSavedRecipeForSelection != null)
            {
                ApplyRecipeToArchitron(architronSavedRecipeForSelection);
                architronSavedRecipeForSelection = null;
            }
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

        if (!architronHoverOverrideActive)
        {
            architronHoverSavedRecipe = arch.recipe;
            architronHoverOverrideActive = true;
        }

        string newRecipe = null;

        if (paletteSelector != null && operatorsSelector != null)
        {
            newRecipe = CombineUsingSelectors(paletteSelector, operatorsSelector);
        }
        else if (paletteSelector != null)
        {
            string hoveredOps = hovered._recipe?.OperatorsSequence() ?? "";
            string paletteIdxStr = paletteSelector._recipe?.PaletteIdx.ToString("D2") ?? "00";
            char baseChar = paletteSelector._recipe?.BasePolyhedron ?? 'C';
            newRecipe = hoveredOps + paletteIdxStr + baseChar;
        }
        else // operatorsSelector != null
        {
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

        if (paletteSelector != null && operatorsSelector != null)
        {
            string combined = CombineUsingSelectors(paletteSelector, operatorsSelector);
            ApplyRecipeToArchitron(combined);
        }
        else
        {
            if (architronSavedRecipeForSelection != null)
            {
                ApplyRecipeToArchitron(architronSavedRecipeForSelection);
            }
            else
            {
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

    // --- Genetic feature helpers ---

    // Compute cartesian product of ops / palette / base between two polytrons.
    // Returns unique recipe strings (ops + paletteIdx D2 + baseChar).
    private List<string> ComputeCrossoverRecipes(Polytron a, Polytron b)
    {
        var result = new HashSet<string>();

        var opsSet = new HashSet<string>();
        var paletteSet = new HashSet<int>();
        var baseSet = new HashSet<char>();

        if (a?._recipe != null)
        {
            opsSet.Add(a._recipe.OperatorsSequence());
            paletteSet.Add(a._recipe.PaletteIdx);
            baseSet.Add(a._recipe.BasePolyhedron);
        }
        if (b?._recipe != null)
        {
            opsSet.Add(b._recipe.OperatorsSequence());
            paletteSet.Add(b._recipe.PaletteIdx);
            baseSet.Add(b._recipe.BasePolyhedron);
        }

        foreach (var ops in opsSet)
        {
            foreach (var pal in paletteSet)
            {
                foreach (var bas in baseSet)
                {
                    string recipe = (ops ?? "") + pal.ToString("D2") + bas;
                    result.Add(recipe);
                }
            }
        }

        return result.ToList();
    }

    // Setup genetic friends around the architron and assign them the crossover recipes.
    private void SetupGeneticFriends(List<string> crossoverRecipes)
    {
        if (crossoverRecipes == null || crossoverRecipes.Count == 0) return;

        // ensure any previous genetic return / state is cleared immediately
        // (this aborts any pending returns and will restore sinks if necessary)
        AbortPendingGeneticReturnImmediate();

        int needed = crossoverRecipes.Count;
        int totalPolytrons = polytrons.Count;
        if (totalPolytrons == 0) return;

        int[] offsets = new int[] { 1, -1, 2, -2, 3, -3, 4, -4 };

        int assigned = 0;
        int offsetIdx = 0;

        var arch = polytrons[architronIdx];
        if (arch == null) return;

        // clear any previous lists
        geneticFriends.Clear();
        geneticBackups.Clear();
        geneticRelativeOffsets.Clear();
        geneticReturnTargets.Clear();
        geneticReturning.Clear();

        while (assigned < needed && offsetIdx < offsets.Length)
        {
            int idx = (architronIdx + offsets[offsetIdx]) % totalPolytrons;
            if (idx < 0) idx += totalPolytrons;
            var candidate = polytrons[idx];

            offsetIdx++;

            if (candidate == null) continue;
            if (candidate == arch) continue;
            if (candidate == paletteSelector || candidate == operatorsSelector) continue;
            if (geneticBackups.ContainsKey(candidate)) continue;

            var backup = new PolytronStateBackup
            {
                recipe = candidate.recipe,
                boundSink = candidate.boundSink,
                position = candidate.transform.position
            };
            geneticBackups[candidate] = backup;
            geneticFriends.Add(candidate);

            // detach from sink to suspend global attraction
            if (candidate.boundSink != null)
            {
                candidate.boundSink.boundPolytron = null;
            }
            candidate.boundSink = null;

            // assign new recipe and rebuild
            candidate.recipe = crossoverRecipes[assigned];
            candidate.RebuildMesh();

            // compute relative offset (so target is recomputed each frame relative to arch position)
            float angle = (float)assigned / Mathf.Max(1, needed) * Mathf.PI * 2f;
            Vector3 rel = Vector3.up * geneticFriendsHeight + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * geneticFriendsRadius;
            geneticRelativeOffsets[candidate] = rel;

            assigned++;
        }

        geneticModeActive = geneticFriends.Count > 0;

        Debug.Log($"[MutatronEngine] SetupGeneticFriends: assigned {geneticFriends.Count} friends for {needed} recipes");
    }

    // Clear genetic friends: start a physics-driven return to their backed-up positions (no teleport),
    // restore recipes immediately, then let AttractGeneticFriends pull them back; final rebind happens when close.
    private void ClearGeneticFriends()
    {
        if (!geneticModeActive && geneticFriends.Count == 0 && geneticBackups.Count == 0) return;

        Debug.Log("[MutatronEngine] ClearGeneticFriends - begin return phase");

        // For each friend, restore recipe immediately and schedule a return target (absolute)
        foreach (var friend in geneticFriends)
        {
            if (friend == null) continue;
            if (!geneticBackups.TryGetValue(friend, out var backup)) continue;

            // restore recipe and rebuild so visuals reflect original state while returning
            friend.recipe = backup.recipe;
            friend.RebuildMesh();

            // schedule absolute return target (previous world position)
            geneticReturnTargets[friend] = backup.position;
            geneticReturning.Add(friend);

            // remove any orbit-relative target so orbit attraction stops immediately
            geneticRelativeOffsets.Remove(friend);

            // keep them unbound for now so AttractGeneticFriends moves them toward target
            if (friend.boundSink != null)
            {
                friend.boundSink.boundPolytron = null;
                friend.boundSink = null;
            }

            // ensure outline reset
            var outline = friend.GetComponent<PointerOutlineStateController>();
            outline?.SetState(0);
        }

        // geneticRelativeOffsets no longer used for these friends; we keep geneticBackups until final rebind.
        geneticModeActive = geneticReturning.Count > 0;
    }

    // If a new genetic setup cancels a pending return, this forces immediate cleanup:
    // rebind pending-return friends immediately to their original sinks (best-effort) and clear state.
    private void AbortPendingGeneticReturnImmediate()
    {
        if (geneticReturning.Count == 0) return;

        Debug.Log("[MutatronEngine] AbortPendingGeneticReturnImmediate - force restore");

        foreach (var friend in geneticReturning.ToList())
        {
            if (friend == null) continue;
            if (!geneticBackups.TryGetValue(friend, out var backup)) continue;

            // immediate rebind to previous sink if available
            if (backup.boundSink != null)
            {
                friend.boundSink = backup.boundSink;
                backup.boundSink.boundPolytron = friend;
            }
            // restore recipe has already been restored earlier when scheduled; ensure mesh OK
            friend.RebuildMesh();

            // clear per-friend return state
            geneticReturnTargets.Remove(friend);
            geneticReturning.Remove(friend);
            geneticBackups.Remove(friend);
            geneticFriends.Remove(friend);
        }

        geneticRelativeOffsets.Clear();
        geneticReturnTargets.Clear();
        geneticReturning.Clear();
        geneticModeActive = false;
    }

    // Apply attraction to genetic friends (called from FixedUpdate)
    private void AttractGeneticFriends()
    {
        // First handle active genetic friends that orbit the architron (use relative offsets recomputed every frame)
        if (geneticRelativeOffsets.Count > 0)
        {
            var arch = polytrons[architronIdx];
            if (arch != null)
            {
                foreach (var kv in geneticRelativeOffsets.ToList())
                {
                    var friend = kv.Key;
                    var rel = kv.Value;
                    if (friend == null) continue;
                    // skip friends that are currently returning to home
                    if (geneticReturning.Contains(friend)) continue;
                    var rb = friend.GetComponent<Rigidbody>();
                    if (rb == null) continue;

                    Vector3 target = arch.transform.position + rel;
                    Vector3 toTarget = target - friend.transform.position;
                    Vector3 force = toTarget * geneticAttractionStrength;
                    rb.AddForce(force);
                }
            }
        }

        // Now handle friends that are returning to their saved positions
        if (geneticReturning.Count > 0)
        {
            foreach (var friend in geneticReturning.ToList())
            {
                if (friend == null) 
                {
                    geneticReturning.Remove(friend);
                    continue;
                }
                if (!geneticReturnTargets.TryGetValue(friend, out var returnTarget)) continue;

                var rb = friend.GetComponent<Rigidbody>();
                if (rb == null) continue;

                Vector3 toTarget = returnTarget - friend.transform.position;
                Vector3 force = toTarget * geneticAttractionStrength;
                rb.AddForce(force);

                // when close enough, finalize and rebind to original sink (if any)
                if (toTarget.magnitude < 0.25f)
                {
                    if (geneticBackups.TryGetValue(friend, out var backup))
                    {
                        if (backup.boundSink != null)
                        {
                            friend.boundSink = backup.boundSink;
                            backup.boundSink.boundPolytron = friend;
                        }
                        // cleanup backup and lists
                        geneticBackups.Remove(friend);
                    }

                    geneticReturnTargets.Remove(friend);
                    geneticReturning.Remove(friend);
                    geneticFriends.Remove(friend);
                    geneticRelativeOffsets.Remove(friend);
                }
            }

            // if all returns finalized, clear global flags
            if (geneticReturning.Count == 0)
            {
                geneticModeActive = geneticRelativeOffsets.Count > 0;
                if (!geneticModeActive)
                {
                    // fully cleared
                    geneticBackups.Clear();
                    geneticFriends.Clear();
                    geneticReturnTargets.Clear();
                    geneticRelativeOffsets.Clear();
                }
            }
        }
    }

    // end of class
}
