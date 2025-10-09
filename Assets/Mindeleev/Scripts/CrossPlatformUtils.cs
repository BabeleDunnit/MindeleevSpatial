using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpatialSys.UnitySDK;

// to use the CameraFollow component we need these packages which are not available in Spatial
#if UNITY_EDITOR
using SpatialSys.UnitySDK.EditorSimulation;
#endif


public static class CrossPlatformUtils
{

    // this works both in Unity and Spatial
    public static Camera FindCamera()
    {
        var cam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        if (cam != null) return cam;
        return null;
    }

    public static void DisableCameraRotation(bool isDisabled)
    {

#if UNITY_EDITOR
        // this works but CameraFollow is not available in Spatial
        // Example for disabling a SimpleCameraController
        var controller = FindCamera()?.GetComponent<CameraFollow>();
        Debug.Assert(controller != null);
        if (controller != null && controller.enabled != !isDisabled)
        {
            controller.enabled = !isDisabled;
        }
#endif

        var camService = SpatialBridge.cameraService;
        if (camService != null)
        {
            camService.lockCameraRotation = isDisabled;
        }
    }


    public static Vector3 GetAvatarPosition()
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

        return avatarPosition;
    }
}
