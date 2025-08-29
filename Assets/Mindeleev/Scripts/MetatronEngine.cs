using System.Collections;
using System.Collections.Generic;
using SpatialSys.UnitySDK.Internal;
using System.Linq;
// using System.Numerics;
using UnityEngine;

public class MetatronEngine : MonoBehaviour
{


    public GameObject circlePrefab; // un prefab con CircleDrawer

    // i dont think we will ever build a metatron bigger than 10 rings
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

    // Start is called before the first frame update
    void Start()
    {
        // hide the placeholder
        GetComponent<MeshRenderer>().enabled = false;

        BuildMetatron(2);

    }

    // Update is called once per frame
    void Update()
    {

    }

    void BuildMetatron(int ringsToBuild)
    {
        int linesCount = 0;
        Dictionary<string, int> skippedLines = new();
        foreach (HexCellData hcd in gridCells.Values)
        {
            if (hcd.isOnMetatronPattern && hcd.ring <= ringsToBuild)
            {
                CircleRenderer cr = CreateCircle(hcd.worldCoords, 0.87f);

                /*                
                                foreach (HexCellData hcd2 in gridCells.Values)
                                {
                                    Color lineColor = Color.white;
                                    Vector3 coord1 = hcd.worldCoords;
                                    Vector3 coord2 = hcd2.worldCoords;

                                    if (!hcd2.isOnMetatronPattern) continue;
                                    if (hcd2.ring > rings) continue;
                                    if (hcd == hcd2) continue;

                                    if (hcd.ring > 0 && hcd2.ring > 0 && ((hcd.idxInRing / hcd.ring) == (hcd2.idxInRing / hcd2.ring)))
                                    {
                                        skippedLines["sameBranch"] = skippedLines.GetValueOrDefault("sameBranch") + 1;
                                        // ok, all the lines are skipped in both directions
                                        lineColor = Color.red;
                                        coord1.y += 0.3f;
                                        coord2.y += 0.3f;
                                        cr.DrawLine(coord1, coord2, lineColor);
                                        continue;
                                    }

                                    if (((hcd.ring * hcd.idxInRing) % 3 == (hcd2.ring * hcd2.idxInRing) % 3)
                                        && hcd.ring < rings)
                                    {
                                        skippedLines["mod3"] = skippedLines.GetValueOrDefault("mod3") + 1;
                                        float m = (coord1 - coord2).magnitude;
                                        lineColor = Color.red;
                                        coord1.y += m/3f;
                                        coord2.y += m/3f;
                                        cr.DrawLine(coord1, coord2, lineColor);
                                        continue;
                                    }

                                    if (((hcd.ring * hcd.idxInRing) % 2 == (hcd2.ring * hcd2.idxInRing) % 2)
                                        && hcd.ring < rings)
                                    {
                                        skippedLines["mod2"] = skippedLines.GetValueOrDefault("mod2") + 1;
                                        float m = (coord1 - coord2).magnitude;
                                        lineColor = Color.red;
                                        coord1.y += m / 3f;
                                        coord2.y += m / 3f;
                                        cr.DrawLine(coord1, coord2, lineColor);
                                        continue;
                                    }

                                    // if ((hcd.worldCoords - hcd2.worldCoords).magnitude > 2.5f) continue;

                                    if (hcd.ring == 0 || hcd2.ring == 0)
                                    {
                                        skippedLines["central"] = skippedLines.GetValueOrDefault("central") + 1;
                                        continue;
                                    }



                                    cr.DrawLine(coord1, coord2, lineColor);
                                    linesCount++;
                                }
                */
            }
        }

        Debug.Log($"[BuildMetatron] draw {linesCount} lines");
        int totalSkipped = 0;
        foreach (var kvp in skippedLines)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}");
            totalSkipped += kvp.Value;
        }
        Debug.Log($"Total skipped: {totalSkipped}");

        var orderedHexCoords = gridCells
            .OrderBy(kvp => kvp.Value.ring)
            .ThenBy(kvp => kvp.Value.idxInRing)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (HexCoord hc in orderedHexCoords)
        {
            Debug.Log($"{gridCells[hc].ring} {gridCells[hc].idxInRing}");
        }

        // create the hexagons. For each ring, the "corner" hexagons have coord (ring, ring * i) for i (0, 5)
        for (int r = 1; r <= ringsToBuild; r++)
        {
            for (int i = 0; i < 6; i++)
            {
                int idxInRing = r * i;
                KeyValuePair<HexCoord, HexCellData>? hc1 = FindCellByRingAndIdx(r, idxInRing);
                KeyValuePair<HexCoord, HexCellData>? hc2 = FindCellByRingAndIdx(r, (idxInRing + r) % (r * 6));
                CircleRenderer cr = new CircleRenderer();
                cr.DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.yellow, 0.05f);
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
            CircleRenderer cr = new CircleRenderer();
            cr.DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.blue, 0.05f);
        }

        // create the two opposite triangles
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
                CircleRenderer cr = new CircleRenderer();
                cr.DrawLine(hc1.Value.Value.worldCoords, hc2.Value.Value.worldCoords, Color.red, 0.05f);
                cr.DrawLine(hc2.Value.Value.worldCoords, hc3.Value.Value.worldCoords, Color.red, 0.05f);
                cr.DrawLine(hc3.Value.Value.worldCoords, hc1.Value.Value.worldCoords, Color.red, 0.05f);
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

                // Get world position for hex center
                Vector2 hexPos2D = hex.Position() + center2D;
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

    /*
        void BuildPavement()
        {
            Vector2 center2D = new Vector2(transform.position.x, transform.position.z);
            for (int ring = 0; ring <= maxRings; ring++)
            {
                int hexesInRing = ring == 0 ? 1 : 6 * ring;
                for (int i = 0; i < hexesInRing; i++)
                {
                    HexCoord hex = ring == 0 ? new HexCoord(0, 0) : HexCoord.AtPolar(ring, i);
                    Vector3 hexWorldPos = gridCells[hex].worldCoords;

                    if (IsMetatronCoord(ring, i))
                    {
                        CreateCircle(hexWorldPos, 0.87f);
                        metatronHexes.Add(hex);
                        gridCells[hex].isOnMetatronPattern = true;
                    }
                }
            }
        }
    */

    bool IsMetatronCoord(int ring, int idxInRing)
    {
        if (ring == 0 || ring == 1) return true;
        for (int i = 2; i < 10; i++)
        {
            if (ring == i && (idxInRing % i == 0)) return true;
        }
        return false;
    }


    CircleRenderer CreateCircle(Vector3 pos, float radius)
    {
        var go = Instantiate(circlePrefab, pos, Quaternion.identity);
        var cd = go.GetComponent<CircleRenderer>();
        cd.radius = radius;
        // cd.color = color;
        cd.DrawCircle();
        return cd;
    }

}

/*
public class CircleManager : MonoBehaviour
{
    public GameObject circlePrefab; // un prefab con CircleDrawer

    void Start()
    {
        CreateCircle(new Vector3(0,0,0), 1f, Color.red);
        CreateCircle(new Vector3(5,0,0), 2f, Color.green);
        CreateCircle(new Vector3(-3,0,4), 0.5f, Color.blue);
    }

    void CreateCircle(Vector3 pos, float radius, Color color)
    {
        var go = Instantiate(circlePrefab, pos, Quaternion.identity);
        var cd = go.GetComponent<CircleDrawer>();
        cd.radius = radius;
        cd.color = color;
        cd.DrawCircle();
    }
}
*/
