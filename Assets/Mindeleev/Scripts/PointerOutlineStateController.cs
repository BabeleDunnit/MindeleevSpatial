using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls a sequence of visual states for a Polytron, each with its own outline color, width, and symbolic name.
/// Use AdvanceState() to cycle through states. Configure states in the Inspector.
/// </summary>
[RequireComponent(typeof(Outline))]
public class PointerOutlineStateController : MonoBehaviour
{
    [Serializable]
    public class State
    {
        public string name = "State";
        public Color outlineColor = Color.white;
        public float outlineWidth = 0.5f;
    }

    [Tooltip("Configure the sequence of states, each with a name, outline color, and width.")]
    public List<State> states = new List<State>();

    [Tooltip("Initial state index (0-based).")]
    public int initialState = 0;

    private int currentState = 0;
    private Outline outlineComponent;

    public State onHover = new State() { name = "onHover", outlineColor = Color.white, outlineWidth = 0.1f };

    void Awake()
    {
        outlineComponent = GetComponent<Outline>();
        Debug.Assert(outlineComponent != null);
        if (states == null || states.Count == 0)
        {
            // Add a default state if none configured
            states = new List<State>
            {
                new State { name = "Default", outlineColor = Color.white, outlineWidth = 0.5f }
            };
        }
        currentState = Mathf.Clamp(initialState, 0, states.Count - 1);
        ApplyState();
    }

    /// <summary>
    /// Advance to the next state, rolling over to zero after the last state.
    /// </summary>
    public void AdvanceState()
    {
        Debug.Assert(false);

        currentState = (currentState + 1) % states.Count;
        ApplyState();
    }

    /// <summary>
    /// Get the current state index (0-based).
    /// </summary>
    public int GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Get the symbolic name of the current state.
    /// </summary>
    public string GetCurrentStateName()
    {
        return states[currentState].name;
    }

    /// <summary>
    /// Set the state by index (0-based).
    /// </summary>
    public void SetState(int idx)
    {
        if (idx >= 0 && idx < states.Count)
        {
            currentState = idx;
            ApplyState();
        }
    }

    /// <summary>
    /// Apply the outline color and width for the current state.
    /// </summary>
    public void ApplyState()
    {
        if (outlineComponent == null) outlineComponent = GetComponent<Outline>();
        var state = states[currentState];
        outlineComponent.outlineColor = state.outlineColor;
        outlineComponent.outlineWidth = state.outlineWidth;
        //outline.DisableOutline();
        //outline.EnableOutline();

        outlineComponent.DisableOutline();
        if (state.outlineWidth > 0f)
        {
            outlineComponent.EnableOutline();
        }
    }

    public void OnHoverEnter()
    {
        if (outlineComponent == null) outlineComponent = GetComponent<Outline>();
        outlineComponent.outlineColor = onHover.outlineColor;
        outlineComponent.outlineWidth = onHover.outlineWidth;
        outlineComponent.DisableOutline();
        if (onHover.outlineWidth > 0f)
        {
            outlineComponent.EnableOutline();
        }
    }

    public void OnHoverExit()
    {
        // restore the previous outline state
        ApplyState();
    }  

}