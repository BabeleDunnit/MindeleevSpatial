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

    // new: when false, pointer handlers/selection/hover are ignored
    public bool interactive = true;
    // when true this polytron is temporarily reserved by the genetic mechanism and must be
    // excluded from normal binding/selection flows until it is returned
    public bool reservedForGenetics = false;

    // 0..71
    internal int sealNumber = -1;

    internal string sealName;

    public Rigidbody RigidBody { get; set; }

    internal PolytronSink boundSink;

    internal bool isArchitron = false;

    MutatronEngine mutatron;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactive) return;
        Debug.Log($"[PointerEvent] Down on {gameObject.name}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!interactive) return;
        Debug.Log($"[PointerEvent] Up on {gameObject.name}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!interactive) return;
        Debug.Log($"[PointerEvent] BeginDrag on {gameObject.name}");
        BeginDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!interactive) return;
        Debug.Log($"[PointerEvent] Drag on {gameObject.name}");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!interactive) return;
        Debug.Log($"[PointerEvent] EndDrag on {gameObject.name}");
        EndDrag();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactive) return;

        if (eventData?.clickCount == 2)
        {
            Debug.Log("double click");
        }

        // Forward click to MutatronEngine which manages selection states and outline states.
        if (mutatron != null)
        {
            mutatron.OnPolytronClicked(this);
        }
        else
        {
            // fallback local behaviour
            PointerOutlineStateController csc = GetComponent<PointerOutlineStateController>();
            csc?.AdvanceState();
        }

        Debug.Log("[Polytron.OnPointerClick()] Object clicked!");

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactive) return;
        Debug.Log("OnPointerEnter");

        GetComponent<PointerOutlineStateController>()?.OnHoverEnter();
        GetComponent<PolytronInfoPanel>()?.Activate(true);

        // Delegate hover handling to MutatronEngine (which will be a no-op if no selection)
        mutatron?.OnPolytronPointerEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!interactive) return;
        Debug.Log("OnPointerExit");

        GetComponent<PointerOutlineStateController>()?.OnHoverExit();
        GetComponent<PolytronInfoPanel>()?.Activate(false);

        mutatron?.OnPolytronPointerExit(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!interactive) return;
        Debug.Log($"[PointerEvent] Drop on {gameObject.name}");
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!interactive) return;
        PointerOutlineStateController csc = GetComponent<PointerOutlineStateController>();
        csc.AdvanceState();

        Debug.Log($"[PointerEvent] Scroll on {gameObject.name}, delta: {eventData.scrollDelta}");
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
