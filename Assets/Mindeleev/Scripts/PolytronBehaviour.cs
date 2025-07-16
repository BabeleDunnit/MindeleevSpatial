using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpatialSys.UnitySDK;



public enum PolytronBehaviourType { Spring01, RecipeBasedEquilibrium, ParticleLife }

// a Polytron has a PolytronBehaviour which controls how it behave in respect to other polytrons, etc
public abstract class PolytronBehaviour
{
    public Polytron Owner { get; set; }
    public PolytronBehaviourType Type { get; set; }
    public abstract void ComputeForce();
    public Vector3 forceAccumulator = Vector3.zero;
    public Vector3 torqueAccumulator = Vector3.zero;

    public (Vector3 attractionForce, Vector3 from1To2Versor, float from1To2Distance)
        CalcSpringForce(Vector3 obj1pos, Vector3 obj2pos, float attractionMultiplier, float equilibriumDistance)
    {
        Vector3 from1to2Vector = obj2pos - obj1pos;
        float from1To2Distance = from1to2Vector.magnitude;

        float distanceFromEquilibrium = from1To2Distance - equilibriumDistance;

        Vector3 from1To2Versor = from1to2Vector.normalized;
        Vector3 attractionForce = from1To2Versor * distanceFromEquilibrium * attractionMultiplier;

        return (attractionForce, from1To2Versor, from1To2Distance);
    }

    public Vector3 ComputeForceTowardAvatar()
    {
        Vector3 avatarPosition;

        GameObject avatar = GameObject.Find("[Spatial SDK] Editor Local Avatar");
        if (avatar == null)
        {
            // Get a reference to the local avatar
            IAvatar localAvatar = SpatialBridge.actorService.localActor.avatar;
            // localAvatar.position = new Vector3(1, 0, 0);
            avatarPosition = localAvatar.position;
        }
        else
        {
            avatarPosition = avatar.transform.position;
        }

        Vector3 myPosition = Owner.gameObject.transform.position;

        (Vector3 attractionForce, Vector3 fromMeToOtherVersor, float fromMeToOtherDistance)
            = CalcSpringForce(myPosition,
            avatarPosition,
            1.0f,
            3.0f);


        return attractionForce;
    }

}
