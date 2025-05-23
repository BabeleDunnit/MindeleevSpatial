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
        // Always use first color set for base colors
        var baseColors = colorSets[0];
        
        // Get base color by face kind
        Color baseColor = kind switch
        {
            
            FaceKind.Vertex => baseColors.vertexColor,
            FaceKind.Edge => baseColors.edgeColor,
            FaceKind.Face => baseColors.faceColor,
            
/*
            FaceKind.Face => Color.red,
            FaceKind.Edge => Color.green,
            FaceKind.Vertex => Color.blue,
*/
            _ => Color.white
        };

        
                // If index > 0 and we have additional color sets, blend with variation colors
                if (index > 0 && colorSets.Count > 1)
                {
                    var variationColor = colorSets[index % (colorSets.Count - 1) + 1].faceColor;
                    return Color.Lerp(baseColor, variationColor, 0.3f);
                }
        
        return baseColor;
    }

    // Add this property to get the number of color sets
    public int ColorSetCount => colorSets.Count;
}