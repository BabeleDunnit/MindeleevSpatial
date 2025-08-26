using UnityEngine;

/// <summary>
/// A PolytronSink represents a "slot" in a grid that can attract and bind exactly one Polytron.
/// It can be configured to attract Polytrons by recipe (AttractByRecipe).
/// Once a Polytron is bound, the sink stops attracting others.
/// </summary>
public class PolytronSink : MonoBehaviour
{
    public string attractedRecipe; // The recipe this sink will attract
    public Polytron boundPolytron;

    public float weight = 1f;

    // public bool IsEmpty => boundPolytron == null;

    // Call this to attempt to bind a Polytron to this sink
    /*
    public bool TryBindPolytron(Polytron polytron)
    {
        if (!IsEmpty) return false;
        if (polytron.recipeString != attractedRecipe) return false;

        boundPolytron = polytron;
        // Optionally: Move polytron to sink position, parent it, etc.
        // polytron.transform.position = transform.position;
        // polytron.transform.SetParent(transform);

        // Sink stops attracting after binding
        return true;
    }

    // Call this to release the bound Polytron (if needed)
    public void ReleasePolytron()
    {
        if (boundPolytron != null)
        {
            // Optionally: Unparent, etc.
            // boundPolytron.transform.SetParent(null);
            boundPolytron = null;
        }
    }
    */

}