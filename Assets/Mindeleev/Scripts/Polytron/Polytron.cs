using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Polytron : PolyhedronGenerator
{

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
