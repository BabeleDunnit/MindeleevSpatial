using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// spring behaviour with single point of equilibrium
public class PolytronSpring01Behaviour : PolytronBehaviour
{

    public float EquilibriumDistance { get; set; } = 5.0f;
    public float AttractionMultiplier { get; set; } = 1.0f;
    public float CollisionDistance { get; set; } = 0.5f;
    public float ContactStiffness { get; set; } = 500f;
    public float LinearFriction { get; set; } = 0.5f;


    public PolytronSpring01Behaviour(Polytron p)
    {
        // Debug.Log($"creating a PolytronSpring01Behaviour with owner id {p.Id}");
        Type = PolytronBehaviourType.Spring01;
        Owner = p;
    }

    public override void ComputeForce()
    {
        var allPolytrons = PolytronEngine.GetAll();
        Vector3 forceAccumulator = Vector3.zero;
        Vector3 torqueAccumulator = Vector3.zero;

        foreach (Polytron p in allPolytrons)
        {
            if (p.Behaviour == null) continue;
            if (p.Behaviour.Owner == Owner) continue;
            if (p.Behaviour.Type != Type) continue;

            GameObject me = Owner.gameObject;
            GameObject other = p.Behaviour.Owner.gameObject;

            Vector3 fromMeToOtherVector = other.transform.position - me.transform.position;
            float fromMeToOtherDistance = fromMeToOtherVector.magnitude;
            float distanceFromEquilibrium = fromMeToOtherDistance - EquilibriumDistance;

            Vector3 fromMeToOtherVersor = fromMeToOtherVector.normalized;
            Vector3 attraction = fromMeToOtherVersor * distanceFromEquilibrium * AttractionMultiplier;
            forceAccumulator += attraction;

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

        //        if (Owner != null)
        //        {

        // Debug.Assert(Owner != null);
        // Debug.Assert(Owner.GetComponent<Rigidbody>() != null);
        // Debug.Assert(Owner.RigidBody != null);

        // known: on first frame Owner.RigidBody can be null, it is set in 
        // Polytron::Start() which can be called after this
        Vector3 friction = -Owner.RigidBody.velocity * LinearFriction;
        forceAccumulator += friction;

        Owner.RigidBody.AddForce(forceAccumulator);
        Owner.RigidBody.AddTorque(torqueAccumulator);
        //        }

        // Debug.DrawLine(me.transform.position, contactPoint, Color.red, 1f);
        // Debug.Log("Torque: " + torqueAccumulator);
    }


}

