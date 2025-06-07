using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PolyhedronPalette))]
public class PolyhedronPaletteEditor : Editor
{
    private bool showColors = true;

    public override void OnInspectorGUI()
    {
        PolyhedronPalette palette = (PolyhedronPalette)target;

        showColors = EditorGUILayout.Foldout(showColors, "Color Sequence");
        
        if (showColors)
        {
            EditorGUI.indentLevel++;
            
            // Show existing colors
            for (int i = 0; i < palette.colors.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                palette.colors[i] = EditorGUILayout.ColorField($"Color {i}", palette.colors[i]);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    palette.colors.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            // Add new color button
            if (GUILayout.Button("Add Color"))
            {
                palette.colors.Add(Color.white);
            }

            EditorGUI.indentLevel--;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(palette);
        }
    }
}