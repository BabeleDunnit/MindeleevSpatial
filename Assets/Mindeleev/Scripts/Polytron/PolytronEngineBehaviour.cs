using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpatialSys.UnitySDK;

public abstract class PolytronEngineBehaviour : ScriptableObject
{
    public abstract void Simulate(List<Polytron> lp, GameObject myEngine);
}
