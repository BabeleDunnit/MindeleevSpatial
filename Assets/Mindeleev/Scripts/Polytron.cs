using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using SpatialSys.UnitySDK;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
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

    public PolytronEngine Engine { get; set; }

    public int Id { get; set; }

    public Rigidbody RigidBody { get; set; }

    internal PolytronSink boundSink;

    public void OnPointerDown(PointerEventData eventData)
    {
        // Debug.Log($"[PointerEvent] Down on {gameObject.name}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Debug.Log($"[PointerEvent] Up on {gameObject.name}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Debug.Log($"[PointerEvent] BeginDrag on {gameObject.name}");

        Outline o = GetComponent<Outline>();
        o.outlineColor = Color.blue;
        o.outlineWidth = 0.5f;
        o.DisableOutline();
        o.EnableOutline();

        BeginDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Debug.Log($"[PointerEvent] Drag on {gameObject.name}");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Debug.Log($"[PointerEvent] EndDrag on {gameObject.name}");

        Outline o = GetComponent<Outline>();
        o.outlineColor = Color.magenta;
        o.DisableOutline();
        o.EnableOutline();

        EndDrag();

    }

    public void OnDrop(PointerEventData eventData)
    {
        // Debug.Log($"[PointerEvent] Drop on {gameObject.name}");
    }

    public void OnScroll(PointerEventData eventData)
    {
        // Debug.Log($"[PointerEvent] Scroll on {gameObject.name}, delta: {eventData.scrollDelta}");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Debug.Log("Pointer over object");

        Outline o = GetComponent<Outline>();
        o.outlineColor = Color.yellow;
        o.EnableOutline();

        WorldSpacePanel wsp = GameObject.Find("InspectorCanvas").GetComponent<WorldSpacePanel>();
        wsp.titleText.text = "enter " + name;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Debug.Log("Pointer left object");

        Outline o = GetComponent<Outline>();
        o.DisableOutline();

        WorldSpacePanel wsp = GameObject.Find("InspectorCanvas").GetComponent<WorldSpacePanel>();
        wsp.titleText.text = "exit " + name;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Outline o = GetComponent<Outline>();
        o.outlineColor = Color.green;
        o.DisableOutline();
        o.EnableOutline();

        Debug.Log("[Polytron.OnPointerClick()] Object clicked!");

/*
        GameObject engineObj = GameObject.Find("HexCellularAutomata");
        if (engineObj != null)
        {
            PolytronEngine engine = engineObj.GetComponent<PolytronEngine>();
            if (engine != null)
            {
                Debug.Log("Found PolytronEngine02 and casted to PolytronEngine.");
                // You can now use 'engine' as needed
                engine.Register(this);
            }
            else
            {
                Debug.LogWarning("PolytronEngine component not found on PolytronEngine02.");
            }
        }
        else
        {
            Debug.LogWarning("GameObject 'PolytronEngine02' not found.");
        }
        */
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

    }

    void Awake()
    {
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
