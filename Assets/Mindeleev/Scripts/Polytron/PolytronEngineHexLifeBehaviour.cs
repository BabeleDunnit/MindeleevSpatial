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
    float polytronScale = 0.3f;

    private Dictionary<HexCoord, bool> currentState = new Dictionary<HexCoord, bool>();
    private Dictionary<HexCoord, bool> nextState = new Dictionary<HexCoord, bool>();
    private Dictionary<HexCoord, GameObject> polytrons = new Dictionary<HexCoord, GameObject>();
    private List<HexCoord> gridHexes = new List<HexCoord>();

    private float evolveTimer = 0f;
    private const float evolveInterval = 1.5f;

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

                bool alive = Random.value > 0.1f;
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

                    // Add and trigger Appear animation
                    var waveAnim = poly.GetComponent<WaveAnimation>();
                    if (waveAnim != null)
                    {
                        waveAnim.SetAnimation(WaveAnimation.AnimationType.Appear);
                    }
                }
            }
        }
        evolveTimer = 0f;
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
        return 0;
    }

    public void Evolve(GameObject myEngine)
    {
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

        Vector2 center2D = new Vector2(myEngine.transform.position.x, myEngine.transform.position.z);

        foreach (var hex in gridHexes)
        {
            bool wasAlive = currentState[hex];
            bool isAlive = nextState[hex];

            Vector2 hexPos2D = hex.Position() * hexDistance + center2D;
            Vector3 position = new Vector3(hexPos2D.x, myEngine.transform.position.y, hexPos2D.y);

            if (isAlive && !wasAlive)
            {
                GameObject poly = PolytronsFactory.Instance.Create("polytron");
                poly.transform.position = position;
                poly.transform.localScale = myEngine.transform.localScale * polytronScale;
                poly.transform.SetParent(myEngine.transform);
                polytrons[hex] = poly;

                // Add and trigger Appear animation
                var waveAnim = poly.GetComponent<WaveAnimation>();
                if (waveAnim != null)
                {
                    waveAnim.SetAnimation(WaveAnimation.AnimationType.Appear);
                }
            }
            else if (!isAlive && wasAlive)
            {
                if (polytrons.ContainsKey(hex) && polytrons[hex] != null)
                {
                    // Trigger Disappear animation before destroying
                    var waveAnim = polytrons[hex].GetComponent<WaveAnimation>();
                    if (waveAnim != null)
                    {
                        waveAnim.SetAnimation(WaveAnimation.AnimationType.Disappear);
                        // Optionally, delay destruction to allow animation to finish
                        Destroy(polytrons[hex], 1.0f); // 1 second delay for animation
                    }
                    else
                    {
                        Destroy(polytrons[hex]);
                    }
                    polytrons.Remove(hex);
                }
            }
            else if (isAlive && wasAlive)
            {
                if (polytrons.ContainsKey(hex) && polytrons[hex] != null)
                {
                    polytrons[hex].transform.position = position;
                }
            }
        }

        var temp = currentState;
        currentState = nextState;
        nextState = temp;
    }
}