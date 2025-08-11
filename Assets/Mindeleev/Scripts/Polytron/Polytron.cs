using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using SpatialSys.UnitySDK;



[RequireComponent(typeof(Collider))]
public class Polytron : PolyhedronGenerator, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{

    // private float clickDelay = 0.3f;
    // private bool singleClickPending = false;

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

        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void Awake()
    {
        //PolytronOutline o = GetComponent<PolytronOutline>();
        //o.EnableOutline();
    }

    /*
        void OnMouseDown()
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;

            if (timeSinceLastClick <= doubleClickThreshold)
            {
                HandleDoubleClick();
            }
            else
            {
                HandleClick();
            }
        }

        private void HandleClick()
        {
            // ShootPolytron();
            Debug.Log("[Polytron] click");

            PolytronOutline o = GetComponent<PolytronOutline>();
            o.EnableOutline();
            // Invoke(nameof(DisableOutlineLater), 5f);

        }

        private void DisableOutlineLater()
        {

            Debug.Log("[Polytron] call DisableOutline()");
            PolytronOutline o = GetComponent<PolytronOutline>();
            o.DisableOutline();

        }

        private void HandleDoubleClick()
        {
            // ShootPolytron();
            Debug.Log("[Polytron] double click");

            PolytronOutline o = GetComponent<PolytronOutline>();
            o.DisableOutline();

        }
    */

    /*

     public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2)
            {
                // Double click
                singleClickPending = false;
                CancelInvoke(nameof(SingleClick));
                DoubleClick();
            }
            else if (eventData.clickCount == 1)
            {
                // Start waiting for second click
                singleClickPending = true;
                Invoke(nameof(SingleClick), clickDelay);
            }
        }

        private void SingleClick()
        {
            if (singleClickPending)
            {
                Debug.Log("Single Click on UI Element!");
                singleClickPending = false;
            }
        }

        private void DoubleClick()
        {
            Debug.Log("Double Click on UI Element!");
        }

    */

    int frameCount = 0;
    bool outlined = false;
    // Update is called once per frame
    void Update()
    {
        /*
        if (frameCount++ % 100 == 0)
        {
            outlined = !outlined;
            if (outlined)
            {
                PolytronOutline o = GetComponent<PolytronOutline>();
                o.EnableOutline();

            }
            else
            {
                PolytronOutline o = GetComponent<PolytronOutline>();
                o.DisableOutline();

            }
        }
        */

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
