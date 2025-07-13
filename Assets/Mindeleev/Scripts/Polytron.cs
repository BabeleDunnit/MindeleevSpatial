using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Polytron : PolyhedronGenerator
{

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
}
