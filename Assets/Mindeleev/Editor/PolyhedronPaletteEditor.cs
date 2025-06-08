using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PolyhedronPalette))]
public class PolyhedronPaletteEditor : Editor
{
    private static Color copiedColor;
    private static bool hasCopiedColor = false;
    private const float MIN_COLOR_DISTANCE = 0.2f; // Minimum HSV distance between colors

    private float GetHSVDistance(Color a, Color b)
    {
        Color.RGBToHSV(a, out float h1, out float s1, out float v1);
        Color.RGBToHSV(b, out float h2, out float s2, out float v2);
        
        // Handle hue wraparound
        float hueDiff = Mathf.Min(Mathf.Abs(h1 - h2), 1f - Mathf.Abs(h1 - h2));
        
        return hueDiff + 0.5f * (Mathf.Abs(s1 - s2) + Mathf.Abs(v1 - v2));
    }

    private Color GenerateRandomColor(List<Color> existingColors)
    {
        Color newColor;
        int maxAttempts = 100;
        int attempts = 0;

        do
        {
            newColor = Color.HSVToRGB(
                Random.value,           // Hue: full random range [0-1]
                Random.Range(0.7f, 1f), // Saturation: high [0.7-1]
                Random.Range(0.9f, 1f)  // Value: very high [0.9-1]
            );
            
            // Check if color is different enough from existing ones
            bool isDifferentEnough = true;
            foreach (Color existing in existingColors)
            {
                if (GetHSVDistance(newColor, existing) < MIN_COLOR_DISTANCE)
                {
                    isDifferentEnough = false;
                    break;
                }
            }
            
            if (isDifferentEnough) break;
            attempts++;
        } 
        while (attempts < maxAttempts);

        return newColor;
    }

    public override void OnInspectorGUI()
    {
        PolyhedronPalette palette = (PolyhedronPalette)target;

        EditorGUI.BeginChangeCheck();
        SerializedProperty colorsProp = serializedObject.FindProperty("colors");

        // Add Random Colors button at the top
        if (GUILayout.Button("Randomize Palette"))
        {
            Undo.RecordObject(palette, "Randomize Palette");
            var newColors = new List<Color>();
            
            for (int i = 0; i < colorsProp.arraySize; i++)
            {
                var colorProp = colorsProp.GetArrayElementAtIndex(i);
                Color newColor = GenerateRandomColor(newColors);
                newColors.Add(newColor);
                colorProp.colorValue = newColor;
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(palette);
            GUI.changed = true;
        }

        EditorGUILayout.PropertyField(colorsProp.FindPropertyRelative("Array.size"));

        for (int i = 0; i < colorsProp.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Color field
            SerializedProperty colorProp = colorsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(colorProp, new GUIContent($"Color {i}"));
            
            // Copy button
            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                copiedColor = palette.colors[i];
                hasCopiedColor = true;
            }
            
            // Paste button
            GUI.enabled = hasCopiedColor;
            if (GUILayout.Button("Paste", GUILayout.Width(50)))
            {
                Undo.RecordObject(palette, "Paste Color");
                colorProp.colorValue = copiedColor;  // Use the serialized property instead
                serializedObject.ApplyModifiedProperties();  // Apply changes
                EditorUtility.SetDirty(palette);  // Mark for saving
                GUI.changed = true;  // Force inspector refresh
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}