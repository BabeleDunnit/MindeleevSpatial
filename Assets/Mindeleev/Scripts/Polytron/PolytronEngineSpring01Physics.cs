using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// spring behaviour with single point of equilibrium
public class PolytronEngineSpring01Physics : PolytronEnginePhysics
{

    public float equilibriumDistance = 6.0f;
    public float attractionMultiplier = 1.0f;
    public float collisionDistance = 0.5f;
    public float contactStiffness = 500f;
    public float linearFriction = 0.5f;

    public override void Simulate(List<Polytron> allPolytrons)
    {
        foreach (Polytron p1 in allPolytrons)
        {
            Vector3 forceAccumulator = Vector3.zero;
            Vector3 torqueAccumulator = Vector3.zero;

            foreach (Polytron p2 in allPolytrons)
            {
                if (p1 == p2) continue;

                GameObject go1 = p1.gameObject;
                GameObject go2 = p2.gameObject;

                Vector3 fromGo1ToGo2Vector = go2.transform.position - go1.transform.position;
                float fromGo1ToGo2Distance = fromGo1ToGo2Vector.magnitude;
                float distanceFromEquilibrium = fromGo1ToGo2Distance - equilibriumDistance;

                Vector3 fromMeToOtherVersor = fromGo1ToGo2Vector.normalized;
                Vector3 attraction = fromMeToOtherVersor * distanceFromEquilibrium * attractionMultiplier;
                forceAccumulator += attraction;

                if (fromGo1ToGo2Distance < collisionDistance)
                {
                    float penetrationDepth = collisionDistance - fromGo1ToGo2Distance;
                    Vector3 repulsion = -fromMeToOtherVersor * penetrationDepth * contactStiffness;
                    forceAccumulator += repulsion;

                    Vector3 offset = Vector3.Cross(Vector3.up, fromMeToOtherVersor) * 0.1f;
                    Vector3 contactPoint = go1.transform.position + offset;
                    Vector3 r = contactPoint - go1.transform.position;
                    Vector3 torqueFromOffset = Vector3.Cross(r, repulsion);
                    torqueAccumulator += torqueFromOffset;

                    Vector3 relativeVelocity = p1.RigidBody.velocity - p2.RigidBody.velocity;
                    Vector3 tangential = Vector3.Cross(Vector3.up, relativeVelocity.normalized) * 0.5f;
                    Vector3 torqueFromSlip = tangential * penetrationDepth * 10f;
                    torqueAccumulator += torqueFromSlip;
                }
            }

            Vector3 friction = -p1.RigidBody.velocity * linearFriction;
            forceAccumulator += friction;

            forceAccumulator += ComputeForceTowardOriginGameObject(this.gameObject, p1);

            p1.RigidBody.AddForce(forceAccumulator);
            p1.RigidBody.AddTorque(torqueAccumulator);
        }
    }

}

