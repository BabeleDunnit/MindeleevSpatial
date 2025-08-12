using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using SpatialSys.UnitySDK;



[RequireComponent(typeof(Collider))]
public class Polytron : PolyhedronGenerator, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{

    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;

    public PolytronEngine Engine { get; set; }

    public int Id { get; set; }
    // public PolytronEngine Engine { get; set; }

    // this is a strategy to encapsulate data and type of polytron behaviour (spring/mass, particleLife, etc)
    // the PolytronEngine will switch on this to execute the relative algorithm
    public PolytronPhysics Behaviour { get; set; }

    public Rigidbody RigidBody { get; set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Pointer over object");
        PolytronOutline o = GetComponent<PolytronOutline>();
        o.outlineColor = Color.yellow;
        o.EnableOutline();

        WorldSpacePanel wsp = GameObject.Find("InspectorCanvas").GetComponent<WorldSpacePanel>();
        wsp.titleText.text = "enter " + name;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Pointer left object");
        PolytronOutline o = GetComponent<PolytronOutline>();
        o.DisableOutline();

        WorldSpacePanel wsp = GameObject.Find("InspectorCanvas").GetComponent<WorldSpacePanel>();
        wsp.titleText.text = "exit " + name;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PolytronOutline o = GetComponent<PolytronOutline>();
        o.outlineColor = Color.green;
        o.DisableOutline();
        o.EnableOutline();
        Debug.Log("[Polytron.OnPointerClick()] Object clicked!");
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
    }

    void Awake()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    /*
        void OnEnable()
        {
            Engine.Register(this);
        }
    */

    void OnDisable()
    {
        Engine.Unregister(this);
    }

    void OnDestroy()
    {
        Engine.Unregister(this);
    }
}
