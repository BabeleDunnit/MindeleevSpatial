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

        /*
                // Assicurati che ci sia un Interactable
                var interactable = GetComponent<SpatialInteractable>();
                if (interactable == null)
                {
                    interactable = gameObject.AddComponent<SpatialInteractable>();
                }

                // Registra gli handler
                interactable.onEnterEvent += OnGrabStarted;
                interactable.onExitEvent += OnGrabEnded;
        */

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

        // funge
        /*
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit))
                    {
                        Debug.Log("Hai cliccato su " + hit.collider.gameObject.name);
                    }
                }
        */


        //         Ray ray = new Ray(transform.position, transform.forward);

        /*
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red);
                Debug.Log("Oggetto colpito: " + hit.collider.name);
            }
            else
            {
                Debug.DrawRay(ray.origin, ray.direction * 10f, Color.green);
            }
        */



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

    /*
        public void OnRayHoverEnter()
        {
            Debug.Log("Hover enter");
        }

        public void OnRayHoverExit()
        {
            Debug.Log("Hover exit");
        }

        public void OnRayClick()
        {
            isDragging = !isDragging;
            if (isDragging)
            {
                Debug.Log("Grabbed");
            }
            else
            {
                Debug.Log("Released");
            }
        }
    */

    /*
        private void Awake()
        {

            // Assicurati che ci sia un Interactable
            var interactable = GetComponent<SpatialInteractable>();
            if (interactable == null)
            {
                interactable = gameObject.AddComponent<SpatialInteractable>();
            }

            // Registra gli handler
            interactable.onEnterEvent += OnGrabStarted;
            interactable.onExitEvent += OnGrabEnded;
        }

        */

    /*
            private void OnGrabStarted()
        {
            Debug.Log("Grab iniziato!");
            // Qui puoi iniziare il drag o la trasformazione
        }

        private void OnGrabEnded()
        {
            Debug.Log("Grab finito!");
            // Qui puoi rilasciare o fermare il movimento
        }
    */



}


/*
using UnityEngine;
using SpatialSys.UnitySDK;

public class PolytronGrabHandler : MonoBehaviour
{
    private void Awake()
    {
        // Assicurati che ci sia un Interactable
        var interactable = GetComponent<Interactable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<Interactable>();
        }

        // Registra gli handler
        interactable.onInteractionStarted += OnGrabStarted;
        interactable.onInteractionEnded += OnGrabEnded;
    }

    private void OnGrabStarted()
    {
        Debug.Log("Grab iniziato!");
        // Qui puoi iniziare il drag o la trasformazione
    }

    private void OnGrabEnded()
    {
        Debug.Log("Grab finito!");
        // Qui puoi rilasciare o fermare il movimento
    }
}
*/


/*
using UnityEngine;
using UnityEngine.InputSystem; // se usi il nuovo sistema di Input

public class VRGrabTarget : MonoBehaviour
{
    private bool isGrabbed = false;

    public void OnRayHoverEnter()
    {
        Debug.Log("Hover enter");
    }

    public void OnRayHoverExit()
    {
        Debug.Log("Hover exit");
    }

    public void OnRayClick()
    {
        isGrabbed = !isGrabbed;
        if (isGrabbed)
        {
            Debug.Log("Grabbed");
        }
        else
        {
            Debug.Log("Released");
        }
    }
}

*/

