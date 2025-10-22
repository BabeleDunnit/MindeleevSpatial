using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using SpatialSys.UnitySDK;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]

// down, up and click handlers need an UI element to be fired, and this is why we need to have
// a SpatialClikcable3D component. For some reason, enter/exit and drag stuff works 
// (because based on early raycasting??)
public class Polytron : PolyhedronGenerator,
  IPointerEnterHandler,
  IPointerExitHandler,
  IPointerDownHandler,
  IPointerUpHandler,
  IPointerClickHandler,
  IBeginDragHandler,
  IDragHandler,
  IEndDragHandler,
  IDropHandler,
  IScrollHandler
{

    // 0..71
    internal int sealNumber = -1;

    internal string sealName;

    public Rigidbody RigidBody { get; set; }

    internal PolytronSink boundSink;

    internal bool isArchitron = false;

    MutatronEngine mutatron;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[PointerEvent] Down on {gameObject.name}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[PointerEvent] Up on {gameObject.name}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[PointerEvent] BeginDrag on {gameObject.name}");

        /*
                Outline o = GetComponent<Outline>();
                o.outlineColor = Color.blue;
                o.outlineWidth = 0.5f;
                o.DisableOutline();
                o.EnableOutline();
                */

        BeginDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[PointerEvent] Drag on {gameObject.name}");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[PointerEvent] EndDrag on {gameObject.name}");
        /*
                Outline o = GetComponent<Outline>();
                o.outlineColor = Color.magenta;
                o.DisableOutline();
                o.EnableOutline();
*/
        EndDrag();

    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[PointerEvent] Drop on {gameObject.name}");
    }

    public void OnScroll(PointerEventData eventData)
    {
        PointerOutlineStateController csc = GetComponent<PointerOutlineStateController>();
        csc.AdvanceState();

        Debug.Log($"[PointerEvent] Scroll on {gameObject.name}, delta: {eventData.scrollDelta}");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("OnPointerEnter");

        GetComponent<PointerOutlineStateController>()?.OnHoverEnter();
        GetComponent<PolytronInfoPanel>()?.Activate(true);

        // mutatron.currentlyHoveredPolytronSealNumber = sealNumber;
        mutatron.OnPolytronPointerEnter(sealNumber);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("OnPointerExit");

        GetComponent<PointerOutlineStateController>()?.OnHoverExit();
        GetComponent<PolytronInfoPanel>()?.Activate(false);

        // mutatron.currentlyHoveredPolytronSealNumber = -1;
        mutatron.OnPolytronPointerExit(sealNumber);

    }

    public void OnPointerClick(PointerEventData eventData)
    {

        /*
        Outline o = GetComponent<Outline>();
        o.outlineColor = Color.green;
        o.DisableOutline();
        o.EnableOutline();
        */

        if (eventData?.clickCount == 2)
        {
            Debug.Log("double click");
        }

        PointerOutlineStateController csc = GetComponent<PointerOutlineStateController>();
        csc.AdvanceState();

        Debug.Log("[Polytron.OnPointerClick()] Object clicked!");

    }

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private float dragDepth;

    private void BeginDrag()
    {
        if (!isDragging)
        {
            isDragging = true;
            dragDepth = mainCamera.WorldToScreenPoint(transform.position).z;
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth));
            offset = transform.position - mouseWorld;

            // avoid camera rotation when dragging
            CrossPlatformUtils.DisableCameraRotation(true);
        }
    }

    private void EndDrag()
    {
        isDragging = false;
        CrossPlatformUtils.DisableCameraRotation(false);
    }

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();

        RigidBody = GetComponent<Rigidbody>();
        if (RigidBody == null)
        {
            throw new NullReferenceException("Polytron must have a RigidBody component, please check");
        }

        mainCamera = CrossPlatformUtils.FindCamera();

        mutatron = FindObjectOfType<MutatronEngine>();

        Debug.Assert(mutatron != null);


    }

    // Update is called once per frame    
    void Update()
    {
        if (isDragging)
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth));
            transform.position = mouseWorld + offset;
        }
    }
}
