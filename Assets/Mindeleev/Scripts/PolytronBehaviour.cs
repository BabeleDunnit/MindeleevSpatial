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
        CalcSpringForce(Transform obj1t, Transform obj2t, float attractionMultiplier, float equilibriumDistance)
    {
        Vector3 from1to2Vector = obj2t.position - obj1t.position;
        float from1To2Distance = from1to2Vector.magnitude;

        float distanceFromEquilibrium = from1To2Distance - equilibriumDistance;

        Vector3 from1To2Versor = from1to2Vector.normalized;
        Vector3 attractionForce = from1To2Versor * distanceFromEquilibrium * attractionMultiplier;

        return (attractionForce, from1To2Versor, from1To2Distance);
    }

    public void AddForceTowardAvatar()
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


        if (SpatialBridge.Instance.LocalAvatar is MonoBehaviour avatarMB)
        {
            GameObject avatarGO = avatarMB.gameObject;
        }


        GameObject localAvatarGO = SpatialBridge.actorService.localActor.avatar.GetLocalAvatarGameObject();




        // Debug.Log(avatarPosition.ToString());

    }

}
