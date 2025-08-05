using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class Polytron : PolyhedronGenerator /*, IPointerClickHandler */
{


  private float clickDelay = 0.3f;
    private bool singleClickPending = false;

    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;

    public PolytronEngine Engine { get; set; }

    public int Id { get; set; }
    // public PolytronEngine Engine { get; set; }

    // this is a strategy to encapsulate data and type of polytron behaviour (spring/mass, particleLife, etc)
    // the PolytronEngine will switch on this to execute the relative algorithm
    public PolytronPhysics Behaviour { get; set; }

    public Rigidbody RigidBody { get; set; }

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
            Debug.Log("click");
        }

        private void HandleDoubleClick()
        {
            // ShootPolytron();
            Debug.Log("double click");
        }

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
