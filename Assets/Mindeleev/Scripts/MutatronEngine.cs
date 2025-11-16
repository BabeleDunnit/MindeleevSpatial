using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using TMPro;

public class MutatronEngine : MonoBehaviour, IPolytronStateProvider
{
    // event panels can subscribe to (polytron, newState)
    public event Action<Polytron, PolytronState> OnPolytronStateChanged;

    bool mustBuildFirstTime = true;
    bool isRebuildingLevel = false;

    int maxRings = 12;

    // all the 12 rings, prebuilt
    internal Dictionary<HexCoord, HexCellData> gridCellsMap = new Dictionary<HexCoord, HexCellData>();

    // the 72 polytrons
    internal List<Polytron> polytrons = new();

    // the polytrons homes
    internal List<KeyValuePair<HexCoord, HexCellData>> polytronsHomes = new();

    internal HexCellData mutatronCenter;

    // the Architron
    internal int architronIdx = 70;

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

    internal struct LevelConfig
    {

        // increased at every rebuild
        int levelCount;

        // we will start with 2
        public int actualRingsCount;
        internal int energyQuantumExchanged;
    }

    internal LevelConfig actualLevelConfig;

    // binding manager (extracted)
    private BindingManager bindingManager;
    // selection manager (extracted)
    internal SelectionManager selectionManager;

    void InitializeCellsCAParametersForCurrentLevel()
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
        // Use the symmetric unbind helper to keep the two-way invariant intact.
        foreach (var polytron in polytrons)
        {
            UnbindPolytron(polytron);
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


    bool BuildLevel(int levelNumber)
    {

        if(isRebuildingLevel)
        {
            return false;
        }

        Debug.Log($"Building level {levelNumber}");

        isRebuildingLevel = true;

        DeselectAllPolytrons();

        // the level number will determine the Metatron complexity
        // and set actualRingsCount, etc.

        actualLevelConfig.actualRingsCount = 4 - levelNumber; // max con 72 polytroni se riempi tutto: 4
        actualLevelConfig.energyQuantumExchanged = 1;

        ResetLevelGraphics();
        SendAllPolytronsHome();
        InitializeCellsCAParametersForCurrentLevel();


        StartCoroutine(DrawMetatronGraphicsCoroutine());
        StartCoroutine(BuildTilesCoroutine());

        mustBuildFirstTime = false;
        evolveCount = 0;
        return true;
    }

    void AfterTilesCreation()
    {
        UpdatePolytronsSinks();

        isRebuildingLevel = false;
    }

    void Start()
    {
        // hide placeholder
        GetComponent<MeshRenderer>().enabled = false;

        CreateHexGridDataStructure();
        Create72PolytronsImmediate();
    // initialize extracted managers after core data structures exist
    bindingManager = new BindingManager(this);
    selectionManager = new SelectionManager(this);
    geneticsManager = new GeneticsManager(this);
        StartCoroutine(Create72PolytronsAndHomesCoroutine());
    }

    Polytron FindPolytronToBind()
    {
        return bindingManager != null ? bindingManager.FindPolytronToBind() : null;
    }

    internal bool IsMutatronCenter(HexCellData cd)
    {
        // Debug.Assert(cd == mutatronCenter);
        return cd.ring == 0 && cd.idxInRing == 0;
    }

    // this is called to update the attraction of polytrons to sinks after each evolution round
    // and at the beginning of a new level
    void UpdatePolytronsSinks()
    {
        if (bindingManager != null)
        {
            bindingManager.UpdatePolytronsSinks();
            return;
        }
    }

    internal void BindPolytronToSink(Polytron p, KeyValuePair<HexCoord, HexCellData> hckv)
    {
        if (bindingManager != null) bindingManager.BindPolytronToSink(p, hckv);
        else BindPolytronToSink(p, hckv.Value.sink);
    }

    internal void BindPolytronToSink(Polytron p, PolytronSink ps)
    {
        // Clear previous binding of this polytron (both sides)
        if (p == null) return;

        if (p.boundSink != null)
        {
            if (p.boundSink.boundPolytron == p) p.boundSink.boundPolytron = null;
            p.boundSink = null;
        }

        // If the sink is already owned by another polytron, clear that other polytron's pointer too
        if (ps != null && ps.boundPolytron != null && ps.boundPolytron != p)
        {
            var prev = ps.boundPolytron;
            if (prev.boundSink == ps) prev.boundSink = null;
            ps.boundPolytron = null;
        }

        // Now do the symmetric bind
        p.boundSink = ps;
        if (ps != null)
        {
            ps.boundPolytron = p;
            // Log binding if this might be a genetic friend
            if (p.reservedForGenetics)
            {
                Debug.LogWarning($"[BindPolytronToSink] WARNING: REBINDING reserved polytron_id={p.sealNumber} (reservedForGenetics={p.reservedForGenetics}) to sink={ps.name}");
            }
            else
            {
                Debug.Log($"[BindPolytronToSink] binding polytron_id={p.sealNumber} to sink={ps.name}");
            }
        }

        NotifyPolytronStateChanged(p);
    }

    // Safe unbind helper: clears both sides of the binding
    internal void UnbindPolytron(Polytron p)
    {
        if (bindingManager != null)
        {
            bindingManager.UnbindPolytron(p);
            NotifyPolytronStateChanged(p);
            return;
        }

        if (p == null) return;
        var sink = p.boundSink;
        if (sink != null)
        {
            Debug.Log($"[UnbindPolytron] UNBINDING polytron_id={p.sealNumber} from sink={sink.name}");
            if (sink.boundPolytron == p) sink.boundPolytron = null;
            p.boundSink = null;
        }

        NotifyPolytronStateChanged(p);
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
            if (p.isArchitron && false)
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
                    NotifyPolytronStateChanged(polytronComponent);
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

                /*
                                PolytronInfoPanel pip = polytronGameObject.GetComponent<PolytronInfoPanel>();
                                if (polytronComponent.isArchitron == false)
                                {
                                    pip.button4Text = "Make Architron";
                                }
                                */



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

                polytronComponent.recipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(polytronId) 
                    + $"{(polytronId % 11):D2}" 
                    + (hckv.Value.idxInRing == 0 ? "T" : "C");
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
        //p.GetComponent<PolytronInfoPanel>().bodyText = r;
        NotifyPolytronStateChanged(p);
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

    // helper: find a cell by ring and index
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
        if (bindingManager != null)
        {
            bindingManager.UnbindNonMatchingPolytrons();
            return;
        }

        int polytronsThatWillNotMove = 0;
        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

            PolytronSink sink = hckv.Value.sink;
            Polytron polytronBoundToSink = sink.boundPolytron;

            if (polytronBoundToSink && polytronBoundToSink.recipe != hckv.Value.tile.recipe)
            {
                // IMPORTANT: Skip reserved genetic friends, they must NOT be unbound during evolution
                if (polytronBoundToSink.reservedForGenetics)
                {
                    Debug.Log($"[UnbindNonMatchingPolytrons] SKIPPING reserved friend polytron_id={polytronBoundToSink.sealNumber} (recipe mismatch but reserved)");
                    polytronsThatWillNotMove++;
                    continue;
                }
                // Use UnbindPolytron to clear both sides safely
                UnbindPolytron(polytronBoundToSink);
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
        if (bindingManager != null)
        {
            bindingManager.SendUnboundPolytronsHome();
            return;
        }

        // Skip polytrons reserved for genetic friends: they must remain unbound until cleared/returned
        var unboundPolytrons = polytrons.Where(p => p.boundSink == null && !p.reservedForGenetics).ToList();
        Debug.Log($"[SendUnboundPolytronsHome] sending home {unboundPolytrons.Count} unbound polytrons (skipping {polytrons.Count(p => p.reservedForGenetics)} reserved)");
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

        // Extra diagnostics to help trace rebinding/reservation races
        try
        {
            var reservedList = polytrons.Where(p => p != null && p.reservedForGenetics).Select(p => p.sealNumber.ToString()).ToList();
            Debug.Log($"[Evolve] reserved polytrons: {string.Join(",", reservedList)}");

            var unboundList = polytrons.Where(p => p != null && p.boundSink == null).Select(p => p.sealNumber + (p.interactive ? "(interactive)" : "")).ToList();
            Debug.Log($"[Evolve] unbound polytrons: {string.Join(",", unboundList)}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Evolve] diagnostics failed: {ex}");
        }

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
        // apply attraction to genetic friends if any (delegated to GeneticsManager)
        if (geneticsManager != null) geneticsManager.AttractGeneticFriends();
    }

    int levelCount = 0;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if(BuildLevel(levelCount))
            {
                levelCount++;   
            }
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

    internal bool IsHome(HexCellData hcd)
    {
        return hcd.ring == 12;
    }

    internal bool IsMutatronCell(HexCellData hcd)
    {
        return hcd.ring <= actualLevelConfig.actualRingsCount;
    }

    internal bool IsHome(HexCoord hc)
    {
        return gridCellsMap[hc].ring == 12;
    }

    internal bool IsMutatronCell(HexCoord hc)
    {
        HexCellData hcd = gridCellsMap[hc];
        return hcd.ring <= actualLevelConfig.actualRingsCount && !IsMutatronCenter(hcd);
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
            // swap bindings atomically using helpers
            var aSink = actualArchitron.boundSink;
            var nSink = newArchitron.boundSink;

            UnbindPolytron(actualArchitron);
            UnbindPolytron(newArchitron);

            BindPolytronToSink(actualArchitron, nSink);
            BindPolytronToSink(newArchitron, aSink);
        }


        actualArchitron.isArchitron = false;
        newArchitron.isArchitron = true;

        architronIdx = newArchitronIdx;

        NotifyPolytronStateChanged(actualArchitron);
        NotifyPolytronStateChanged(newArchitron);

        Debug.Assert(actualArchitron.isArchitron == false);
        Debug.Assert(newArchitron.isArchitron == true);

        Debug.Assert(gridCellsMap[newArchitron.boundSink.hexCoord].ring == 0);
        Debug.Assert(gridCellsMap[newArchitron.boundSink.hexCoord].idxInRing == 0);

        Debug.Assert(actualArchitron.boundSink.boundPolytron == actualArchitron);
        Debug.Assert(newArchitron.boundSink.boundPolytron == newArchitron);

    }

    string oldArchitronRecipe;

    // Selection state moved to SelectionManager
    // Genetics handled by GeneticsManager (extracted)
    private GeneticsManager geneticsManager;

    // expose genetic active state for SelectionManager and other callers
    internal bool geneticModeActive => geneticsManager != null && geneticsManager.GeneticModeActive;

    internal void DeselectAllPolytrons()
    {
        if (selectionManager != null) selectionManager.DeselectAllPolytrons();
    }

    // Called by Polytron on click -> delegate to SelectionManager
    internal void OnPolytronClicked(Polytron p)
    {
        if (selectionManager != null) selectionManager.OnPolytronClicked(p);
    }

    // Build combined recipe string from two selected Polytrons:
    // paletteSelector provides PaletteIdx and BasePolyhedron,
    // operatorsSelector provides OperatorsSequence().
    internal string CombineUsingSelectors(Polytron paletteP, Polytron opsP)
    {
        string ops = opsP._recipe?.OperatorsSequence() ?? "";
        string paletteIdxStr = paletteP._recipe?.PaletteIdx.ToString("D2") ?? "00";
        char baseChar = paletteP._recipe?.BasePolyhedron ?? 'C';
        return ops + paletteIdxStr + baseChar;
    }

    // Apply string recipe to the architron (set recipe and rebuild)
    internal void ApplyRecipeToArchitron(string recipe)
    {
        var arch = polytrons[architronIdx];
        if (arch == null) return;
        if (arch.recipe == recipe) return;
        arch.recipe = recipe;
        arch.RebuildMesh();
        arch.name = $"Architron_{recipe}";
    }

    // Called by Polytron on pointer enter -> delegate to SelectionManager
    internal void OnPolytronPointerEnter(Polytron hovered)
    {
        if (selectionManager != null) selectionManager.OnPolytronPointerEnter(hovered);
    }

    // Called by Polytron on pointer exit -> delegate to SelectionManager
    internal void OnPolytronPointerExit(Polytron p)
    {
        if (selectionManager != null) selectionManager.OnPolytronPointerExit(p);
    }

    // Return a recipe string representing only the radix (palette index + base polyhedron)
    internal string GetRadixRecipe(Polytron p)
    {
        if (p == null || p._recipe == null) return "";
        string paletteIdxStr = p._recipe.PaletteIdx.ToString("D2");
        char baseChar = p._recipe.BasePolyhedron;
        return paletteIdxStr + baseChar;
    }

    // --- Genetic feature helpers ---

    // Compute cartesian product of ops / palette / base between two polytrons.
    // Returns unique recipe strings (ops + paletteIdx D2 + baseChar).
    internal List<string> ComputeCrossoverRecipes(Polytron a, Polytron b)
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
    internal void SetupGeneticFriends(List<string> crossoverRecipes)
    {
        if (geneticsManager != null) geneticsManager.SetupGeneticFriends(crossoverRecipes);
    }

    internal void ClearGeneticFriends()
    {
        if (geneticsManager != null) geneticsManager.ClearGeneticFriends();
    }

    internal void AbortPendingGeneticReturnImmediate()
    {
        if (geneticsManager != null) geneticsManager.AbortPendingGeneticReturnImmediate();
    }

    // authoritative single-shot state computation
    public PolytronState ComputeState(Polytron p)
    {
        var s = new PolytronState();

        // Debug.Assert(p.boundSink != null);

        if (p == null)
        {
            s.Location = PolytronLocation.Unknown;
            s.Role = PolytronRole.Normal;
            s.Selection = SelectionSlot.None;
            s.Interactive = false;
            s.SealNumber = -1;
            s.SealName = null;
            s.Recipe = null;
            return s;
        }

        s.SealNumber = p.sealNumber;
        s.SealName = p.sealName;
        s.Recipe = p.recipe;
        s.Interactive = p.interactive;
        s.IsBound = p.boundSink != null;
    s.IsReturning = geneticsManager != null && geneticsManager.IsReturning(p);

        // Role
    if (p.isArchitron) s.Role = PolytronRole.Architron;
    else if (s.IsReturning || (geneticsManager != null && geneticsManager.IsGeneticFriend(p))) s.Role = PolytronRole.GeneticFriend;
        else s.Role = PolytronRole.Normal;

        // Location: home / mutatron center / mutatron
        /*
        if (p.boundSink == null) s.Location = PolytronLocation.Home;
        else if (mutatronCenter != null && p.boundSink == mutatronCenter.sink) s.Location = PolytronLocation.MutatronCenter;
        else s.Location = PolytronLocation.Mutatron;
        */

        s.Location = PolytronLocation.Unknown;
        if(p.boundSink != null)
        {
            PolytronSink ps = p.boundSink;
            HexCoord hc = ps.hexCoord;
            HexCellData hcd = gridCellsMap[hc];

            /*
            HexCellData hcd = gridCellsMap[hc];
            if(hcd.idxInRing == p.sealNumber && hcd.ring == 12)
            {
                s.Location = PolytronLocation.Home;
            }
            else if (mutatronCenter != null && p.boundSink == mutatronCenter.sink) s.Location = PolytronLocation.MutatronCenter;
            */

            if(IsHome(hc))
            {
//                 HexCellData hcd = gridCellsMap[hc];
                Debug.Assert(hcd.idxInRing == p.sealNumber && hcd.ring == 12);
                s.Location = PolytronLocation.Home;                
            }
/*            else if(IsMutatronCenter(hcd))
            {
                s.Location = PolytronLocation.MutatronCenter;                
            }
            */
            /*
            else if (mutatronCenter != null && p.boundSink == mutatronCenter.sink) 
            {
                s.Location = PolytronLocation.MutatronCenter;
            }
            */
            else if(IsMutatronCell(hc))
            {
                s.Location = PolytronLocation.Mutatron;                
            }
            else if(IsMutatronCenter(hcd))
            {
                s.Location = PolytronLocation.MutatronCenter;                
            }
            /*
            else if (mutatronCenter != null && p.boundSink == mutatronCenter.sink) 
            {
                s.Location = PolytronLocation.MutatronCenter;
            }
            */



        }



    // Selection slots (authoritative from this engine)
    if (selectionManager != null && selectionManager.PaletteSelector == p) s.Selection = SelectionSlot.Palette;
    else if (selectionManager != null && selectionManager.OperatorsSelector == p) s.Selection = SelectionSlot.Operators;
    else s.Selection = SelectionSlot.None;

        return s;
    }

    // call this from places where bindings/selection/genetic state change
    public void NotifyPolytronStateChanged(Polytron p)
    {
        if (p == null) return;
        try
        {
            OnPolytronStateChanged?.Invoke(p, ComputeState(p));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"NotifyPolytronStateChanged threw: {ex}");
        }
    }

    // Example: places you should insert NotifyPolytronStateChanged calls (not exhaustive)
    // - after BindPolytronToSink(...) and UnbindPolytron(...)
    // - at end of SetupGeneticFriends(...)
    // - at end of ClearGeneticFriends(...)
    // - after SetNewArchitron(...)
    // - when paletteSelector/operatorsSelector change
    //
    // Example:
    // BindPolytronToSink(a, sink);
    // NotifyPolytronStateChanged(a);
    // NotifyPolytronStateChanged(otherAffectedPolytron);
}
