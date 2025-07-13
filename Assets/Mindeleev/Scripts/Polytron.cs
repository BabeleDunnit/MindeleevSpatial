using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Polytron : PolyhedronGenerator
{

    public int Id { get; set; }

    // this is a strategy to encapsulate data and type of polytron behaviour (spring/mass, particleLife, etc)
    // the PolytronEngine will switch on this to execute the relative algorithm
    public PolytronBehaviour behaviour;

    Rigidbody rigidBody;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        rigidBody = GetComponent<Rigidbody>();
        if (rigidBody == null)
        {
            throw new NullReferenceException("Polytron must have a RigidBody component, please check");
        }


    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnEnable()
    {
        PolytronEngine.Register(this);
    }

    void OnDisable()
    {
        PolytronEngine.Unregister(this);
    }

    void OnDestroy()
    {
        PolytronEngine.Unregister(this);
    }


}
