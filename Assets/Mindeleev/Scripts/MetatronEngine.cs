using System.Collections;
using System.Collections.Generic;
using SpatialSys.UnitySDK.Internal;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;

public class MetatronEngine : MonoBehaviour
{
    // public GameObject circlePrefab;
    int maxRings = 12;

    public class HexCellData
    {
        public int ring;
        public int idxInRing;
        public Vector3 worldCoords;
        public bool isOnMetatronPattern;
    }

    private Dictionary<HexCoord, HexCellData> gridCells = new Dictionary<HexCoord, HexCellData>();
    private List<HexCoord> gridHexes = new List<HexCoord>();
    private List<HexCoord> metatronHexes = new List<HexCoord>();

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
    }

    void BuildLevel(int levelNumber)
    {

            // the level number will determine the Metatron complexity 

            StartCoroutine(BuildMetatronCoroutine(3));
            StartCoroutine(BuildSinksCoroutine(3));
            StartCoroutine(BuildTilesCoroutine(3));

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

    IEnumerator BuildSinksCoroutine(int ringsToBuild)
    {
        yield return new WaitForSeconds(2f);
        foreach (HexCellData hcd in gridCells.Values)
        {
            if (hcd.ring <= ringsToBuild)
            {
                if (hcd.isOnMetatronPattern)
                {
                    GameObject poly = PolytronsFactory.Instance.Create("polytron/ttC", 0.3f);
                    poly.transform.position = new Vector3(hcd.worldCoords.x, 1.0f, hcd.worldCoords.z);
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }
    }

    IEnumerator BuildTilesCoroutine(int ringsToBuild)
    {
        yield return new WaitForSeconds(1f);
        foreach (var hc in gridCells)
        {
            if (hc.Value.ring <= ringsToBuild)
            {

                float angleToCenter = hc.Key.PolarAngle();
                Quaternion tileRotation = Quaternion.Euler(0f, - angleToCenter * 360f / 6.28f, 0f);

                GameObject tile = PolytronsFactory.Instance.Create("tile/ttC", 1f);
                tile.transform.localScale = new Vector3(0.6f, 0.01f, 0.8f);
                tile.transform.position = hc.Value.worldCoords + new Vector3(0,0.01f,0);
                tile.transform.localRotation = tileRotation;

                    yield return new WaitForSeconds(0.2f);
                
            }
        }
    }


    IEnumerator BuildMetatronCoroutine(int ringsToBuild)
    {
        // Draw circles
        foreach (HexCellData hcd in gridCells.Values)
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

        yield return new WaitForSeconds(0.5f);

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

        yield return new WaitForSeconds(0.3f);

        // Draw central cross
        for (int i = 0; i < 3; i++)
        {
            int idxInRing1 = (i * ringsToBuild);
            int idxInRing2 = ((i + 3) * ringsToBuild);
            KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(ringsToBuild, idxInRing1);
            KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(ringsToBuild, idxInRing2);
            DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.gray, 0.02f);
        }

        yield return new WaitForSeconds(0.3f);

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
                        yield return new WaitForSeconds(0.1f);
                        DrawLine(hc2.Value.Value.worldCoords, hc3.Value.Value.worldCoords, Color.gray, 0.01f);
                        yield return new WaitForSeconds(0.1f);
                        DrawLine(hc3.Value.Value.worldCoords, hc1.Value.Value.worldCoords, Color.gray, 0.01f);
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
    }

        void BuildMetatronImmediate(int ringsToBuild)
    {
        Dictionary<string, int> skippedLines = new();
        foreach (HexCellData hcd in gridCells.Values)
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
        foreach (var kvp in gridCells)
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
                gridHexes.Add(hex);

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
                    metatronHexes.Add(hex);
                    cellData.isOnMetatronPattern = true;
                }

                gridCells[hex] = cellData;
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
}