using UnityEngine;
using System;
using SpatialSys.UnitySDK;
// using SpatialSys.UnitySDK.Internal;
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
    private bool cameraWasEnabled = true;

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


        Component[] components = mainCamera.GetComponents<Component>();

        foreach (Component comp in components)
        {
            Debug.Log($"Componente: {comp.GetType().Name}");
        }
/*
        // Example for disabling a SimpleCameraController
        var controller = mainCamera?.GetComponent<CameraFollow>();
        Debug.Assert(controller != null);
        if (controller != null && controller.enabled != !disable)
            controller.enabled = !disable;
*/

        // If using Cinemachine or another camera system, disable its input here.
        // If you have a custom camera script, reference and disable it here.
    }
}