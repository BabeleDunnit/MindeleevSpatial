using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PolyhedronPalette))]
public class PolyhedronPaletteEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var palette = (PolyhedronPalette)target;
        
        if (GUILayout.Button("Add Color Set"))
        {
            var colorSet = new PolyhedronPalette.ColorSet
            {
                faceColor = Random.ColorHSV(0f, 1f, 0.5f, 0.7f, 0.8f, 1f),
                edgeColor = Random.ColorHSV(0f, 1f, 0.6f, 0.8f, 0.7f, 0.9f),
                vertexColor = Random.ColorHSV(0f, 1f, 0.7f, 0.9f, 0.6f, 0.8f)
            };
            palette.colorSets.Add(colorSet);
            EditorUtility.SetDirty(palette);
        }

        base.OnInspectorGUI();
    }
}