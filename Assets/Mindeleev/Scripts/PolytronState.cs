using UnityEngine;
using System.Linq;
using System.Collections.Generic;

// Small datatype describing a Polytron "state" (derived from engine + polytron)
public enum PolytronLocation
{
    Unknown,
    Home,           // at home ring (external)
    MutatronCenter, // architron sink (center)
    Mutatron,       // somewhere on the mutatron (inside rings)
    FollowingAvatar, // architron outside the mutatron following the avatar
    Returning       // currently returning after genetic behavior
}

public enum PolytronRole
{
    Normal,
    Architron,
    GeneticFriend
}

public enum SelectionSlot
{
    None,
    Palette,
    Operators
}

public struct PolytronState
{
    public PolytronLocation Location;
    public PolytronRole Role;
    public SelectionSlot Selection;
    public bool Interactive;
    public bool IsBound;
    public bool IsReturning;
    public string Recipe;
    public int SealNumber;
    public string SealName;

    public override string ToString()
    {
        return $"PolytronState(seal:{SealNumber} name:{SealName} role:{Role} loc:{Location} sel:{Selection} interactive:{Interactive})";
    }
}

/// <summary>
/// If you want authoritative state, implement IPolytronStateProvider on MutatronEngine
/// or another manager and return precise data (selected slots, genetic friends, home/mutatron detection).
/// </summary>
public interface IPolytronStateProvider
{
    PolytronState ComputeState(Polytron p);
}

/// <summary>
/// Lightweight local heuristic evaluator used as fallback when there is no global provider.
/// It inspects the Polytron component and a few heuristics only.
/// </summary>
public static class LocalPolytronStateEvaluator
{
    public static PolytronState ComputeState(Polytron p)
    {
        var s = new PolytronState();
        if (p == null) return s;

        s.SealNumber = p.sealNumber;
        s.SealName = p.sealName;
        s.Recipe = p.recipe;
        s.Interactive = p.interactive;
        s.IsBound = p.boundSink != null;
        s.IsReturning = false;

        // best-effort role/location heuristics
        s.Role = p.isArchitron ? PolytronRole.Architron : PolytronRole.Normal;

        if (p.boundSink != null)
        {
            // fallback: if sink.y > 1.5 treat as center (approx)
            var sinkGO = p.boundSink.gameObject;
            if (sinkGO != null)
            {
                var pos = sinkGO.transform.position;
                if (pos.y > 1.9f) s.Location = PolytronLocation.MutatronCenter;
                else s.Location = PolytronLocation.Mutatron;
            }
            else
            {
                s.Location = PolytronLocation.Mutatron;
            }
        }
        else
        {
            s.Location = PolytronLocation.Home;
        }

        s.Selection = SelectionSlot.None; // can't know without engine provider

        return s;
    }
}