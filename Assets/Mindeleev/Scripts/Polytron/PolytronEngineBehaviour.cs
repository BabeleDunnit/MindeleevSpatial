using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpatialSys.UnitySDK;

public abstract class PolytronEngineBehaviour : ScriptableObject
{
    public abstract void Setup(GameObject myEngine);
    public abstract void Loop(List<Polytron> registeredPolytrons, GameObject myEngine);

    public virtual int Invoke(string s, GameObject myEngine) { return 0; }
}
