using UnityEngine;

/// <summary>
/// A PolytronSink represents a "slot" in a grid that can attract and bind exactly one Polytron.
/// It can be configured to attract Polytrons by recipe (AttractByRecipe).
/// Once a Polytron is bound, the sink stops attracting others.
/// </summary>
public class MutatronTile : PolyhedronGenerator
{

    // if [0..71], this is the home of a polytron
    public int sealHome = -1;

}