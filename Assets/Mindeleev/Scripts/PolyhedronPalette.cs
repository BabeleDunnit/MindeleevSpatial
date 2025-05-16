using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Polyhedron Palette", menuName = "Mindeleev/Polyhedron Palette")]
public class PolyhedronPalette : ScriptableObject 
{
    [System.Serializable]
    public struct ColorSet
    {
        public Color faceColor;
        public Color edgeColor;
        public Color vertexColor;
    }

    public List<ColorSet> colorSets = new List<ColorSet>();

    void OnEnable()
    {
        if (colorSets == null || colorSets.Count == 0)
        {
            colorSets = new List<ColorSet>
            {
                new ColorSet
                {
                    faceColor = new Color(0.2f, 0.6f, 1.0f),
                    edgeColor = new Color(0.1f, 0.4f, 0.8f),
                    vertexColor = new Color(0.0f, 0.3f, 0.7f)
                },
                new ColorSet
                {
                    faceColor = new Color(1.0f, 0.4f, 0.4f),
                    edgeColor = new Color(0.8f, 0.2f, 0.2f),
                    vertexColor = new Color(0.7f, 0.1f, 0.1f)
                }
            };
        }
    }

    public Color GetColor(int index, FaceKind kind)
    {
        if (colorSets.Count == 0) return Color.white;
        var set = colorSets[index % colorSets.Count];
        return kind switch
        {
            FaceKind.Face => set.faceColor,
            FaceKind.Edge => set.edgeColor,
            FaceKind.Vertex => set.vertexColor,
            _ => Color.white
        };
    }
}