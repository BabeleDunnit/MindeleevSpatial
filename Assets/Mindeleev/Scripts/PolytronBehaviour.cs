using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PolytronBehaviourType { ParticleLife }

public abstract class PolytronBehaviour
{
    public Polytron Owner { get;  set;}  
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

    public override void ComputeForce()
    {
        Debug.Log("computing force on polytron " + Owner.Id);
        var allPolytrons = PolytronEngine.GetAll();

        Vector3 forceAccumulator = new Vector3();

        foreach (Polytron p in allPolytrons)
        {
            if (p.Behaviour == null) continue;
            if (p.Behaviour.Owner == Owner) continue;
            if (p.Behaviour.Type != Type) continue;
            Debug.Log($"computing force between polytrons {p.Behaviour.Owner.Id} and {Owner.Id}");

            GameObject me = Owner.gameObject;
            GameObject other = p.Behaviour.Owner.gameObject;

            Vector3 fromMeToOtherVector = other.transform.position - me.transform.position;
            float fromMeToOtherDistance = fromMeToOtherVector.magnitude;
            float distanceFromEquilibrium = fromMeToOtherDistance - 15.0f;

            Vector3 force = fromMeToOtherVector.normalized * distanceFromEquilibrium;

            forceAccumulator += force;

        }

float frictionCoefficient = 0.9f;
Vector3 friction = -Owner.RigidBody.velocity * frictionCoefficient;
forceAccumulator += friction;

        Owner.RigidBody.AddForce(forceAccumulator);
    }
}

