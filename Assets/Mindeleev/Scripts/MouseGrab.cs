using UnityEngine;
using System;
using SpatialSys.UnitySDK;

// to use the CameraFollow component we need these packages which are not available in Spatial
using SpatialSys.UnitySDK.Internal;
using System.Runtime.CompilerServices;
// using SpatialSys.UnitySDK.EditorSimulation;

/// <summary>
/// Attach this to polyhedron GameObjects to enable mouse grab and drag.
/// </summary>
public class MouseGrab : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private float dragDepth;
    // private bool cameraWasEnabled = true;

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

            // Disable camera controls if you have a component for it (example: SimpleCameraController, Cinemachine, etc.)
            DisableCameraControls(true);
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        DisableCameraControls(false);
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
    private void DisableCameraControls(bool disable)
    {

        // Grabbing(disable);

        Component[] components = mainCamera.GetComponents<Component>();

        foreach (Component comp in components)
        {
            // Debug.Log($"Componente: {comp.GetType().Name}");
        }

        // this works but CameraFollow is not available in Spatial
        /* 
                // Example for disabling a SimpleCameraController
                var controller = mainCamera?.GetComponent<CameraFollow>();
                Debug.Assert(controller != null);
                if (controller != null && controller.enabled != !disable)
                    controller.enabled = !disable;
        */

        /*
                var controller = mainCamera?.GetComponent<Camera>();
                Debug.Assert(controller != null);
                    if (controller != null && controller.enabled != !disable)
                        controller.enabled = !disable;

        */


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