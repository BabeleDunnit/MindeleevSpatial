#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(PolyhedronPalette))]
public class PolyhedronPaletteEditor : Editor
{
    private static Color copiedColor;
    private const float MIN_COLOR_DISTANCE = 0.2f; // Minimum HSV distance between colors

    private float GetHSVDistance(Color a, Color b)
    {
        Color.RGBToHSV(a, out float h1, out float s1, out float v1);
        Color.RGBToHSV(b, out float h2, out float s2, out float v2);
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
                Random.value,
                Random.Range(0.7f, 1f),
                Random.Range(0.9f, 1f)
            );
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

    private static readonly Color[] DefaultColors = new Color[]
    {
        Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow
    };

    public override void OnInspectorGUI()
    {
        PolyhedronPalette palette = (PolyhedronPalette)target;

        EditorGUILayout.LabelField("Polyhedron Palette", EditorStyles.boldLabel);

        // Reset Palette Button
        if (GUILayout.Button("Reset Palette"))
        {
            palette.colors = new List<Color>(DefaultColors);
            EditorUtility.SetDirty(palette);
        }

        // Randomize Palette Button
        if (GUILayout.Button("Randomize Palette"))
        {
            var newColors = new List<Color>();
            for (int i = 0; i < palette.colors.Count; i++)
            {
                newColors.Add(GenerateRandomColor(newColors));
            }
            palette.colors = newColors;
            EditorUtility.SetDirty(palette);
        }

        // Show each color with delete, copy, and paste buttons
        for (int i = 0; i < palette.colors.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            Color oldColor = palette.colors[i];
            palette.colors[i] = EditorGUILayout.ColorField(new GUIContent($"Color {i + 1}"), palette.colors[i], false, false, false);
            if (palette.colors[i] != oldColor)
            {
                EditorUtility.SetDirty(palette);
            }
            if (GUILayout.Button("Copy", GUILayout.Width(45)))
            {
                GUIUtility.systemCopyBuffer = $"#{ColorUtility.ToHtmlStringRGB(palette.colors[i])}";
            }
            if (GUILayout.Button("Paste", GUILayout.Width(45)))
            {
                if (ColorUtility.TryParseHtmlString(GUIUtility.systemCopyBuffer, out Color c))
                {
                    palette.colors[i] = c;
                    EditorUtility.SetDirty(palette);
                }
            }
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                palette.colors.RemoveAt(i);
                EditorUtility.SetDirty(palette);
                break; // Avoid modifying collection during iteration
            }
            EditorGUILayout.EndHorizontal();
        }

        // Add Color Button
        if (GUILayout.Button("Add Color"))
        {
            palette.colors.Add(Color.white);
            EditorUtility.SetDirty(palette);
        }
    }
}

#endif
