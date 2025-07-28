using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LSystemEvolutor))]
public class LSystemEvolutorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LSystemEvolutor evolutor = (LSystemEvolutor)target;
        if (GUILayout.Button("Evolve"))
        {
            evolutor.Evolve();
        }
    }
}