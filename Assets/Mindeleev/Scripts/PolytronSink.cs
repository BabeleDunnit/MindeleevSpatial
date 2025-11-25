using System.Data.Common;
using UnityEngine;

/// <summary>
/// A PolytronSink represents a "slot" in a grid that can attract and bind exactly one Polytron.
/// It can be configured to attract Polytrons by recipe (AttractByRecipe).
/// Once a Polytron is bound, the sink stops attracting others.
/// </summary>
public class PolytronSink : MonoBehaviour
{
    // public string attractedRecipe; // The recipe this sink will attract
    internal Polytron boundPolytron;

    internal float weight = 1f;

    internal HexCoord hexCoord;

    // public bool updated = false;

}