using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Conway's Game of Life on a hexagonal grid using HexCoord.
/// Generates a grid with a center hex and several rings, and evolves the automaton synchronously.
/// </summary>
[CreateAssetMenu(fileName = "PolytronEngineHexLifeBehaviour", menuName = "Polytron Engine/Hex Life")]
public class PolytronEngineHexLifeBehaviour : PolytronEngineBehaviour
{
    [Header("Hex Grid Settings")]
    public int maxRing = 3;
    public float hexDistance = 2.0f;
    float polytronScale = 0.3f;

    // Struct to hold all cell data
    public class HexCellData
    {
        public bool currentState;
        public bool nextState;
        public GameObject polytron;

        public int ring;

        public int ringPlace;
        // Extend here with more fields as needed
    }

    private Dictionary<HexCoord, HexCellData> gridCells = new Dictionary<HexCoord, HexCellData>();
    private List<HexCoord> gridHexes = new List<HexCoord>();

    private float evolveTimer = 0f;
    private const float evolveInterval = 1.5f;

    private void Reset(GameObject myEngine)
    {
        Debug.Log("Entering HexLifeBehaviour.Reset()");

        gridCells.Clear();
        gridHexes.Clear();

        Vector2 center2D = new Vector2(myEngine.transform.position.x, myEngine.transform.position.z);

        for (int ring = 0; ring <= maxRing; ring++)
        {
            int hexesInRing = ring == 0 ? 1 : 6 * ring;
            for (int i = 0; i < hexesInRing; i++)
            {
                HexCoord hex = ring == 0 ? new HexCoord(0, 0) : HexCoord.AtPolar(ring, i);
                gridHexes.Add(hex);

                bool alive = Random.value > 0.1f;
                // bool alive = (ring == 0);

                // bool alive = IsMetatronCoord(ring, i);


                var cell = new HexCellData
                {
                    currentState = alive,
                    nextState = false,
                    polytron = null,
                    ring = ring,
                    ringPlace = i
                };

                Vector2 hexPos2D = hex.Position() * hexDistance + center2D;
                Vector3 position = new Vector3(hexPos2D.x, myEngine.transform.position.y, hexPos2D.y);

                if (alive)
                {
                    GameObject poly = PolytronsFactory.Instance.Create("polytron", polytronScale);
                    poly.transform.position = position;
                    poly.transform.SetParent(myEngine.transform);
                    cell.polytron = poly;

                    var waveAnim = poly.GetComponent<WaveAnimation>();
                    if (waveAnim != null)
                    {
                        waveAnim.SetAnimation(WaveAnimation.AnimationType.Appear);
                    }
                }

                gridCells[hex] = cell;
            }
        }
        evolveTimer = 0f;
    }

    public override void Setup(GameObject myEngine)
    {
        Reset(myEngine);
    }

    bool IsMetatronCoord(int ring, int i)
    {
        if (ring == 0 || ring == 1) return true;
        if (ring == 2 && (i % 2 == 0)) return true;
        return false;
    }

    public override void Loop(List<Polytron> registeredPolytrons, GameObject myEngine)
    {
        evolveTimer += Time.deltaTime;
        if (evolveTimer >= evolveInterval)
        {
            Evolve(myEngine);
            evolveTimer = 0f;
        }
    }

    public override int Invoke(string s, GameObject myEngine)
    {
        if (s == "evolve")
        {
            Evolve(myEngine);
            return 1;
        }

        if (s == "reset")
        {
            Reset(myEngine);
            return 1;
        }

        return 0;
    }

    public void Evolve(GameObject myEngine)
    {
        // Compute next state
        foreach (var hex in gridHexes)
        {
            int aliveNeighbors = 0;
            for (int n = 0; n < 6; n++)
            {
                HexCoord neighbor = hex.Neighbor(n);
                if (gridCells.ContainsKey(neighbor) && gridCells[neighbor].currentState)
                    aliveNeighbors++;
            }

            HexCellData cellData = gridCells[hex];
            bool alive = cellData.currentState;
            bool nextAlive = false;

        
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

            // rule for pattern breeder
            // nextAlive = ((aliveNeighbors % 2) == 1) /* || cellData.ring == 0 */;

            cellData.nextState = nextAlive;
            

        }

        Vector2 center2D = new Vector2(myEngine.transform.position.x, myEngine.transform.position.z);

        // Apply next state and update Polytrons
        foreach (var hex in gridHexes)
        {
            var cell = gridCells[hex];
            bool wasAlive = cell.currentState;
            bool isAlive = cell.nextState;

            Vector2 hexPos2D = hex.Position() * hexDistance + center2D;
            Vector3 position = new Vector3(hexPos2D.x, myEngine.transform.position.y, hexPos2D.y);

            if (isAlive && !wasAlive)
            {
                GameObject poly = PolytronsFactory.Instance.Create("polytron", polytronScale);
                poly.transform.position = position;
                poly.transform.SetParent(myEngine.transform);
                cell.polytron = poly;

                var waveAnim = poly.GetComponent<WaveAnimation>();
                if (waveAnim != null)
                {
                    waveAnim.SetAnimation(WaveAnimation.AnimationType.Appear);
                }
            }
            else if (!isAlive && wasAlive)
            {
                if (cell.polytron != null)
                {
                    var waveAnim = cell.polytron.GetComponent<WaveAnimation>();
                    if (waveAnim != null)
                    {
                        waveAnim.SetAnimation(WaveAnimation.AnimationType.Disappear);
                        Destroy(cell.polytron, 1.0f);
                    }
                    else
                    {
                        Destroy(cell.polytron);
                    }
                    cell.polytron = null;
                }
            }
            else if (isAlive && wasAlive)
            {
                if (cell.polytron != null)
                {
                    cell.polytron.transform.position = position;
                }
            }

            // Swap state
            cell.currentState = cell.nextState;
        }
    }
}