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
public class MouseGrab : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private float dragDepth;

    void Start()
    {
        mainCamera = FindSpatialCamera();
    }

    void OnMouseDown()
    {
        if (!isDragging)
        {
            isDragging = true;
            dragDepth = mainCamera.WorldToScreenPoint(transform.position).z;
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth));
            offset = transform.position - mouseWorld;

            // avoid camera rotation when dragging
            DisableCameraRotation(true);

            /*
                        var camService = SpatialBridge.cameraService;
                        if (camService != null)
                        {
                            camService.lockCameraRotation = true;
                        }
            */

        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        DisableCameraRotation(false);
        /*
        var camService = SpatialBridge.cameraService;
        if (camService != null)
        {
            camService.lockCameraRotation = false;
        }
*/

    }

    void Update()
    {
        if (isDragging)
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth));
            transform.position = mouseWorld + offset;
        }
    }

    // Find the camera in a Spatial scene (customize as needed)
    private Camera FindSpatialCamera()
    {

        var cam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        if (cam != null) return cam;
        return null;
    }

    // Disable camera controls during drag (customize for your camera system)
    private void DisableCameraRotation(bool isDisabled)
    {

        // mainCamera.velocity = Vector3.zero;
        // mainCamera.transform.rotation;

        // Grabbing(disable);

        /*
                Component[] components = mainCamera.GetComponents<Component>();

                foreach (Component comp in components)
                {
                    // Debug.Log($"Componente: {comp.GetType().Name}");
                }
        */

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

        // this disables rendering
        /*
                var controller = mainCamera?.GetComponent<Camera>();
                Debug.Assert(controller != null);
                    if (controller != null && controller.enabled != !disable)
                        controller.enabled = !disable;
*/

        var camService = SpatialBridge.cameraService;
        if (camService != null)
        {
            camService.lockCameraRotation = isDisabled;
        }


        // If using Cinemachine or another camera system, disable its input here.
        // If you have a custom camera script, reference and disable it here.
    }


    /*
        // Nel tuo script di grab:
        void Grabbing( bool grabbing)
    {
        // non funziona
        if (grabbing)
            {
                // Evita la rotazione della camera consumando l'input
                Cursor.lockState = CursorLockMode.None;
                // Cursor.visible = true;
            }
            else
            {
                // Ripristina il controllo normale
                Cursor.lockState = CursorLockMode.Locked;
                // Cursor.visible = false;
            }
    }
    */


}