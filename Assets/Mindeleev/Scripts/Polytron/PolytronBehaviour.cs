using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PolytronBehaviour
{

    public abstract void Simulate(); 
}


public class PolytronPhysicalBehaviour : PolytronBehaviour
{

    public PolytronPhysics Physics { get; set; }

    public override void Simulate()
    {

    }
}
