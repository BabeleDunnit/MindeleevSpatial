using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Polyhedron Palette", menuName = "Mindeleev/Polyhedron Palette")]
public class PolyhedronPalette : ScriptableObject 
{
    public List<Color> colors = new List<Color>();

    void OnEnable()
    {
        if (colors == null || colors.Count == 0)
        {
            colors = new List<Color>
            {
                new Color(0.2f, 0.6f, 1.0f),  // Blue
                new Color(0.2f, 1.0f, 0.2f),  // Green
                new Color(1.0f, 0.4f, 0.4f),  // Red
                new Color(1.0f, 1.0f, 0.2f),  // Yellow
                new Color(1.0f, 0.6f, 0.0f),  // Orange
                new Color(0.8f, 0.2f, 0.8f),  // Purple
            };
        }
    }

    public Color GetColor(int colorIndex)
    {
        if (colors.Count == 0) return Color.white;
        return colors[colorIndex % colors.Count];
    }
}