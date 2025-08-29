using System.Collections;
using System.Collections.Generic;
using SpatialSys.UnitySDK.Internal;
using System.Linq;
using UnityEngine;

public class MetatronEngine : MonoBehaviour
{
    public GameObject circlePrefab;
    int maxRings = 10;

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
        GetComponent<MeshRenderer>().enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
//             StartCoroutine(BuildMetatronCoroutine(3));
            StartCoroutine(BuildPavementCoroutine());
        }
    }

    IEnumerator BuildPavementCoroutine()
    {
        yield return BuildMetatronCoroutine(2);
        yield return new WaitForSeconds(4f);
        yield return BuildMetatronCoroutine(4);
    }


    IEnumerator BuildMetatronCoroutine(int ringsToBuild)
    {
        // Draw circles
        foreach (HexCellData hcd in gridCells.Values)
        {
            if (hcd.isOnMetatronPattern && hcd.ring <= ringsToBuild)
            {
                DrawCircle(hcd.worldCoords, 1.73f);
                yield return new WaitForSeconds(0.1f);
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
                DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.yellow, 0.05f);
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
            DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.blue, 0.05f);
        }

        yield return new WaitForSeconds(0.3f);

        // Draw opposite equilateral triangles
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
                yield return new WaitForSeconds(0.1f);
                DrawLine(hc2.Value.Value.worldCoords, hc3.Value.Value.worldCoords, Color.red, 0.05f);
                yield return new WaitForSeconds(0.1f);
                DrawLine(hc3.Value.Value.worldCoords, hc1.Value.Value.worldCoords, Color.red, 0.05f);
                yield return new WaitForSeconds(0.1f);
            }
        }

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
                    DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.cyan, 0.05f);
                    yield return new WaitForSeconds(0.1f);
                    DrawLine(hc2.Value.Value.worldCoords, hc3.Value.Value.worldCoords, Color.cyan, 0.05f);
                    yield return new WaitForSeconds(0.1f);
                    DrawLine(hc3.Value.Value.worldCoords, hc1.Value.Value.worldCoords, Color.cyan, 0.05f);
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
                DrawCircle(hcd.worldCoords, 1.73f);
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

    void DrawCircle(Vector3 pos, float radius)
    {
        var go = Instantiate(circlePrefab, pos, Quaternion.identity);
        var cd = go.GetComponent<CircleRenderer>();
        cd.radius = radius;
        cd.DrawCircle();
    }

    void DrawLine(Vector3 start, Vector3 end, Color color, float width = 0.05f)
    {
        var go = new GameObject("Line");
        var lr = go.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
    }
}
