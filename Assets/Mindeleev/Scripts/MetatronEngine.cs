using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetatronEngine : MonoBehaviour
{

    // i dont think we will ever build a metatron bigger than 10 rings
    int maxRings = 10;

    public class HexCellData
    {
        public int ring;
        public int idxInRing;
        public Vector3 worldCoords;
    }

    private Dictionary<HexCoord, HexCellData> gridCells = new Dictionary<HexCoord, HexCellData>();
    private List<HexCoord> gridHexes = new List<HexCoord>();

    // Start is called before the first frame update
    void Start()
    {
        // hide the placeholder
        GetComponent<MeshRenderer>().enabled = false;


    }

    // Update is called once per frame
    void Update()
    {

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

                gridCells[hex] = cellData;
            }
        }

    }
}
