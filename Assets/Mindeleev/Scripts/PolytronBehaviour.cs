using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PolytronBehaviourType { Spring01, RecipeBasedEquilibrium, ParticleLife }

public abstract class PolytronBehaviour
{
    public Polytron Owner { get; set; }
    public PolytronBehaviourType Type { get; set; }
    public abstract void ComputeForce();
}
