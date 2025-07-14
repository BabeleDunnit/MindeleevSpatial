using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PolytronBehaviourType { ParticleLife }

public abstract class PolytronBehaviour
{
    public Polytron Owner { get; set; }
    public PolytronBehaviourType Type { get; set; }
    public abstract void ComputeForce();
}

public class PolytronParticleLifeBehaviour : PolytronBehaviour
{
    public PolytronParticleLifeBehaviour(Polytron p)
    {
        Debug.Log($"creating a PolytronParticleLifeBehaviour with owner id {p.Id}");
        Type = PolytronBehaviourType.ParticleLife;
        Owner = p;
    }

    /*
        public override void ComputeForce()
        {
            // Debug.Log("computing force on polytron " + Owner.Id);
            var allPolytrons = PolytronEngine.GetAll();

            Vector3 forceAccumulator = new Vector3();

            foreach (Polytron p in allPolytrons)
            {
                if (p.Behaviour == null) continue;
                if (p.Behaviour.Owner == Owner) continue;
                if (p.Behaviour.Type != Type) continue;
                // Debug.Log($"computing force between polytrons {p.Behaviour.Owner.Id} and {Owner.Id}");

                GameObject me = Owner.gameObject;
                GameObject other = p.Behaviour.Owner.gameObject;

                Vector3 fromMeToOtherVector = other.transform.position - me.transform.position;
                float fromMeToOtherDistance = fromMeToOtherVector.magnitude;
                float distanceFromEquilibrium = fromMeToOtherDistance - 1.0f;

                Vector3 force = fromMeToOtherVector.normalized * distanceFromEquilibrium;

                forceAccumulator += force;

                float minDistance = 0.5f;
                float contactStiffness = 500.0f;

                if (fromMeToOtherDistance < minDistance)
                {
                    float penetrationDepth = minDistance - fromMeToOtherDistance;
                    Vector3 repulsion = -fromMeToOtherVector.normalized * penetrationDepth * contactStiffness;
                    forceAccumulator += repulsion;
                }


            }

            float frictionCoefficient = 0.1f;
            Vector3 friction = -Owner.RigidBody.velocity * frictionCoefficient;
            forceAccumulator += friction;

            Owner.RigidBody.AddForce(forceAccumulator);
        }

    */

    /*
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
                float distanceFromEquilibrium = fromMeToOtherDistance - 1.0f;

                Vector3 force = fromMeToOtherVector.normalized * distanceFromEquilibrium;
                forceAccumulator += force;

                float minDistance = 0.5f;
                float contactStiffness = 500.0f;

                if (fromMeToOtherDistance < minDistance)
                {
                    float penetrationDepth = minDistance - fromMeToOtherDistance;
                    Vector3 repulsion = -fromMeToOtherVector.normalized * penetrationDepth * contactStiffness;
                    forceAccumulator += repulsion;

                    // Calcolo torque
                    //Vector3 contactPoint = other.transform.position; // approssimazione
                    //Vector3 r = contactPoint - me.transform.position;
                    Vector3 r = fromMeToOtherVector / 2.0f;

                    Vector3 torque = Vector3.Cross(r, repulsion);
                    // Vector3 torque = Vector3.up;
                    torqueAccumulator += torque;
                }
            }

            // Aggiungi attrito lineare
            float frictionCoefficient = 0.1f;
            Vector3 friction = -Owner.RigidBody.velocity * frictionCoefficient;
            forceAccumulator += friction;

            // Aggiungi attrito angolare
            float angularFrictionCoefficient = 0.05f;
            Vector3 angularFriction = -Owner.RigidBody.angularVelocity * angularFrictionCoefficient;
            torqueAccumulator += angularFriction;

            // Applica forze
            Owner.RigidBody.AddForce(forceAccumulator);
            Owner.RigidBody.AddTorque(torqueAccumulator);
        }
    */


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
        float distanceFromEquilibrium = fromMeToOtherDistance - 10.0f;

        Vector3 attraction = fromMeToOtherVector.normalized * distanceFromEquilibrium * 2.0f;
        forceAccumulator += attraction;

        float minDistance = 0.5f;
        float contactStiffness = 500.0f;

        if (fromMeToOtherDistance < minDistance)
        {
            float penetrationDepth = minDistance - fromMeToOtherDistance;
            Vector3 repulsion = -fromMeToOtherVector.normalized * penetrationDepth * contactStiffness;
            forceAccumulator += repulsion;

            // === Torque da punto di contatto decentrato ===
            Vector3 offset = Vector3.Cross(Vector3.up, fromMeToOtherVector.normalized) * 0.1f;
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

    // === Attrito lineare ===
    float frictionCoefficient = 0.4f;
    Vector3 friction = -Owner.RigidBody.velocity * frictionCoefficient;
    forceAccumulator += friction;

    // === Applica forze e torque ===
    Owner.RigidBody.AddForce(forceAccumulator);
    Owner.RigidBody.AddTorque(torqueAccumulator);

    // Debug opzionali
    // Debug.DrawLine(me.transform.position, contactPoint, Color.red, 1f);
    // Debug.Log("Torque: " + torqueAccumulator);
}

}

