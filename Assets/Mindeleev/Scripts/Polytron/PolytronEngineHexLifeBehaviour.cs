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
    public int maxRing = 6;
    public float hexDistance = 2.0f;
    // public GameObject polytronPrefab; // Assign in inspector

    float polytronScale = 0.3f;

    // Internal grid state: true = alive, false = dead
    private Dictionary<HexCoord, bool> currentState = new Dictionary<HexCoord, bool>();
    private Dictionary<HexCoord, bool> nextState = new Dictionary<HexCoord, bool>();
    private Dictionary<HexCoord, GameObject> polytrons = new Dictionary<HexCoord, GameObject>();

    // List of all hexes in the grid
    private List<HexCoord> gridHexes = new List<HexCoord>();

    public override void Setup(GameObject myEngine)
    {
        Debug.Log("Entering HexLifeBehaviour.Setup()");

        currentState.Clear();
        nextState.Clear();
        polytrons.Clear();
        gridHexes.Clear();

        Vector2 center2D = new Vector2(myEngine.transform.position.x, myEngine.transform.position.z);

        for (int ring = 0; ring <= maxRing; ring++)
        {
            int hexesInRing = ring == 0 ? 1 : 6 * ring;
            for (int i = 0; i < hexesInRing; i++)
            {
                HexCoord hex = ring == 0 ? new HexCoord(0, 0) : HexCoord.AtPolar(ring, i);
                gridHexes.Add(hex);

                // Random initial state (or set as desired)
                bool alive = Random.value > 0.1f;
//                 bool alive = true;
                currentState[hex] = alive;
                nextState[hex] = false;

                Vector2 hexPos2D = hex.Position() * hexDistance + center2D;
                Vector3 position = new Vector3(hexPos2D.x, myEngine.transform.position.y, hexPos2D.y);

                if (alive)
                {
                    GameObject poly = PolytronsFactory.Instance.Create("polytron");
                    poly.transform.position = position;
                    poly.transform.SetParent(myEngine.transform);
                    poly.transform.localScale = myEngine.transform.localScale * polytronScale;
                    polytrons[hex] = poly;
                }
            }
        }
    }

    public override void Loop(List<Polytron> registeredPolytrons, GameObject myEngine)
    {
    }

    public override int Invoke(string s, GameObject myEngine)
    {
        if (s == "evolve")
        {
            Evolve(myEngine);
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Synchronously calculates the next state of all cells and updates the grid.
    /// Implements Conway's Game of Life rules for a hex grid:
    /// - Any live cell with 2 or 3 live neighbors survives.
    /// - Any dead cell with exactly 3 live neighbors becomes alive.
    /// - All other live cells die in the next generation. All other dead cells stay dead.
    /// </summary>
    public void Evolve(GameObject myEngine)
    {
        // Calculate next state for all cells
        foreach (var hex in gridHexes)
        {
            int aliveNeighbors = 0;
            for (int n = 0; n < 6; n++)
            {
                HexCoord neighbor = hex.Neighbor(n);
                if (currentState.ContainsKey(neighbor) && currentState[neighbor])
                    aliveNeighbors++;
            }

            bool alive = currentState[hex];
            bool nextAlive = false;

            // Game of Life rules for hex grid
            if (alive)
            {
                nextAlive = (aliveNeighbors == 3 || aliveNeighbors == 6);
            }
            else
            {
                nextAlive = (aliveNeighbors == 2);
            }

            nextState[hex] = nextAlive;
        }

        // Apply next state and update Polytrons
        Vector2 center2D = new Vector2(myEngine.transform.position.x, myEngine.transform.position.z);

        foreach (var hex in gridHexes)
        {
            bool wasAlive = currentState[hex];
            bool isAlive = nextState[hex];

            Vector2 hexPos2D = hex.Position() * hexDistance + center2D;
            Vector3 position = new Vector3(hexPos2D.x, myEngine.transform.position.y, hexPos2D.y);

            if (isAlive && !wasAlive)
            {
                // Cell became alive: create Polytron
                GameObject poly = PolytronsFactory.Instance.Create("polytron");
                poly.transform.position = position;
                poly.transform.localScale = myEngine.transform.localScale * polytronScale;
                poly.transform.SetParent(myEngine.transform);
                polytrons[hex] = poly;
            }
            else if (!isAlive && wasAlive)
            {
                // Cell died: destroy Polytron
                if (polytrons.ContainsKey(hex) && polytrons[hex] != null)
                {
                    GameObject.Destroy(polytrons[hex]);
                    polytrons.Remove(hex);
                }
            }
            else if (isAlive && wasAlive)
            {
                // Cell stays alive: move Polytron to correct position (optional)
                if (polytrons.ContainsKey(hex) && polytrons[hex] != null)
                {
                    polytrons[hex].transform.position = position;
                }
            }
            // else: stays dead, do nothing
        }

        // Swap states
        var temp = currentState;
        currentState = nextState;
        nextState = temp;
    }
}