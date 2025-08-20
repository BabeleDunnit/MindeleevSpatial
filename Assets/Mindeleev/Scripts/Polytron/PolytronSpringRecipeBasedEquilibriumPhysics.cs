using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// spring behaviour with dynamic, recipe-based point of equilibrium
public class PolytronSpringRecipeBasedEquilibriumPhysics : PolytronPhysics
{

    // public float EquilibriumDistance { get; set; } = 5.0f;
    public float AttractionMultiplier { get; set; } = 1.0f;
    public float CollisionDistance { get; set; } = 0.5f;
    public float ContactStiffness { get; set; } = 500f;
    public float LinearFriction { get; set; } = 0.9f;

    public static Dictionary<string, float> eqMap;


    public PolytronSpringRecipeBasedEquilibriumPhysics(Polytron p)
    {
        // Debug.Log($"creating a PolytronSpringRecipeBasedEquilibriumBehaviour with owner id {p.Id}");
        Type = PolytronPhysicsType.RecipeBasedEquilibrium;
        Owner = p;
    }

    public override void ComputeForce()
    {
        /*
        var allPolytrons = Owner.Engine.GetAllPolytrons();
        forceAccumulator = Vector3.zero;
        torqueAccumulator = Vector3.zero;

        foreach (Polytron p in allPolytrons)
        {
            if (p.Behaviour == null) continue;
            if (p.Behaviour.Owner == Owner) continue;
            if (p.Behaviour.Type != Type) continue;

            GameObject me = Owner.gameObject;
            GameObject other = p.Behaviour.Owner.gameObject;

            (Vector3 attractionForce, Vector3 fromMeToOtherVersor, float fromMeToOtherDistance)
                = CalcSpringForce(me.transform.position,
                other.transform.position,
                1.0f,
                eqMap[Owner.recipeString + "|" + p.recipeString]);

            forceAccumulator += attractionForce;

            if (fromMeToOtherDistance < CollisionDistance)
            {
                float penetrationDepth = CollisionDistance - fromMeToOtherDistance;
                Vector3 repulsion = -fromMeToOtherVersor * penetrationDepth * ContactStiffness;
                forceAccumulator += repulsion;

                // === Torque da punto di contatto decentrato ===
                Vector3 offset = Vector3.Cross(Vector3.up, fromMeToOtherVersor) * 0.1f;
                Vector3 contactPoint = me.transform.position + offset;
                Vector3 r = contactPoint - me.transform.position;
                Vector3 torqueFromOffset = Vector3.Cross(r, repulsion);
                torqueAccumulator += torqueFromOffset;

                // === Torque da velocità relativa tangenziale ===
                Vector3 relativeVelocity = Owner.RigidBody.velocity - p.Behaviour.Owner.RigidBody.velocity;
                Vector3 tangential = Vector3.Cross(Vector3.up, relativeVelocity.normalized) * 0.5f;
                Vector3 torqueFromSlip = tangential * penetrationDepth * 10f;
                torqueAccumulator += torqueFromSlip;
            }
        }

        // known: on first frame Owner.RigidBody can be null, it is set in 
        // Polytron::Start() which can be called after this
        Vector3 friction = -Owner.RigidBody.velocity * LinearFriction;
        forceAccumulator += friction;

        forceAccumulator += ComputeForceTowardAvatar();


        Owner.RigidBody.AddForce(forceAccumulator);
        Owner.RigidBody.AddTorque(torqueAccumulator);


        // Debug.DrawLine(me.transform.position, contactPoint, Color.red, 1f);
        // Debug.Log("Torque: " + torqueAccumulator);
        */
        
    }


    public static Dictionary<string, float> CreateEquilibriumDistanceMap(HashSet<string> recipes)
    {
        var map = new Dictionary<string, float>();
        var recipeList = new List<string>(recipes);

        for (int i = 0; i < recipeList.Count; i++)
        {
            for (int j = 0; j < recipeList.Count; j++)
            {
                string a = recipeList[i];
                string b = recipeList[j];

                float complexityA = PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(a));
                float complexityB = PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(b));
                float baseDist = 8.0f;

                // Heuristic depends on order (a to b)
//                 float diff = Mathf.Abs(complexityB - complexityA);  // not absolute
                float diff = complexityB - complexityA;  // not absolute
                float mean = (complexityA + complexityB) * 0.2f;
                float jitter = UnityEngine.Random.Range(-2f, 2f);

                // float eqDist = baseDist + diff * 0.5f + mean + jitter;
                float eqDist = baseDist + diff;
                // float eqDist = baseDist;

                string key = $"{a}|{b}";
                map[key] = eqDist;
            }
        }
        return map;
    }

    /*
    
    /// <summary>
    /// Creates a mapping from a pair of recipes (as "recipeA|recipeB") to a float equilibrium distance.
    /// The mapping is symmetric: (A,B) == (B,A).
    /// </summary>
    public static Dictionary<string, float> CreateEquilibriumDistanceMap(HashSet<string> recipes)
    {
        var map = new Dictionary<string, float>();
        var recipeList = new List<string>(recipes);

        // Example heuristic: base distance + complexity difference + random jitter
        for (int i = 0; i < recipeList.Count; i++)
        {
            for (int j = i; j < recipeList.Count; j++)
            {
                string a = recipeList[i];
                string b = recipeList[j];
                float complexityA = PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(a));
                float complexityB = PolyhedronRecipeUtils.ComputeComplexity(PolyhedronRecipeParser.Parse(b));
                float baseDist = 3.0f;
                float diff = Mathf.Abs(complexityA - complexityB);
                float mean = (complexityA + complexityB) * 0.2f;
                float jitter = UnityEngine.Random.Range(-0.2f, 0.2f);
                float eqDist = baseDist + diff * 0.5f + mean + jitter;

                string key = $"{a}|{b}";
                string keySym = $"{b}|{a}";
                map[key] = eqDist;
                map[keySym] = eqDist; // ensure symmetry
            }
        }
        return map;
    }

*/

}
