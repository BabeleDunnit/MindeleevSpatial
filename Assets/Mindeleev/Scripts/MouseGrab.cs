using UnityEngine;
using System;
using SpatialSys.UnitySDK;

// to use the CameraFollow component we need these packages which are not available in Spatial
#if UNITY_EDITOR
using SpatialSys.UnitySDK.EditorSimulation;
#endif

/*

il problema maggiore per avere interazioni utente è con il Meta Quest 
perché è limitato rispetto alle altre device, per avere un ambiente virtuale 
che viene usato con tutte le 3 device è necessario, tramite Unity, usare: 
Trigger, Interactable, UI Button, UI Input.
Rimane impossibile in VR accedere a pagine Web.

*/



/// <summary>
/// Attach this to polyhedron GameObjects to enable mouse grab and drag.
/// </summary>

[Obsolete ("functionality merged into Polytron.cs")]
public class MouseGrab : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private float dragDepth;

    void Start()
    {

        mainCamera = CrossPlatformUtils.FindCamera();
    }

    private void BeginDrag()
    {
        if (!isDragging)
        {
            isDragging = true;
            dragDepth = mainCamera.WorldToScreenPoint(transform.position).z;
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth));
            offset = transform.position - mouseWorld;

            // avoid camera rotation when dragging
            DisableCameraRotation(true);
        }
    }


    private void EndDrag()
    {
        isDragging = false;
        DisableCameraRotation(false);

    }

    void OnMouseDown()
    {
        BeginDrag();
    }

    void OnMouseUp()
    {
        EndDrag();
    }

    void Update()
    {
        if (isDragging)
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth));
            transform.position = mouseWorld + offset;
        }
    }

    // Disable camera controls during drag (customize for your camera system)
    private void DisableCameraRotation(bool isDisabled)
    {

#if UNITY_EDITOR
        // this works but CameraFollow is not available in Spatial
        // Example for disabling a SimpleCameraController
        var controller = mainCamera?.GetComponent<CameraFollow>();
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
}
