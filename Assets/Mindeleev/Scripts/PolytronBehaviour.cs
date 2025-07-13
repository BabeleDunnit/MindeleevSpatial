using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PolytronBehaviourType { ParticleLife }

public class PolytronBehaviour
{
    public PolytronBehaviourType Type { get; set; }
}

public class PolytronParticleLifeBehaviour : PolytronBehaviour
{
    PolytronParticleLifeBehaviour()
    {
        Type = PolytronBehaviourType.ParticleLife;
    }
}
