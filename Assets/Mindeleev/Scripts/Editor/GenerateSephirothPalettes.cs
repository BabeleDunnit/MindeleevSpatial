#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GenerateSephirothPalettes
{
    [MenuItem("Tools/Generate Sephiroth Palettes")]
    public static void GeneratePalettes()
    {
        var paletteData = new Dictionary<string, List<Color>>()
        {
            { "Kether", new List<Color> {
                Hex("#FFFFFF"), Hex("#F8F8FF"), Hex("#FAFAFA"), Hex("#FFFACD")
            }},
            { "Chokmah", new List<Color> {
                Hex("#BEBEBE"), Hex("#E8E8E8"), Hex("#87CEEB"), Hex("#C0C0C0")
            }},
            { "Binah", new List<Color> {
                Hex("#000000"), Hex("#2F2F2F"), Hex("#3B2F2F"), Hex("#2E0854")
            }},
            { "Chesed", new List<Color> {
                Hex("#000080"), Hex("#0F52BA"), Hex("#4682B4"), Hex("#4169E1")
            }},
            { "Geburah", new List<Color> {
                Hex("#DC143C"), Hex("#B22222"), Hex("#8B0000"), Hex("#FF2400")
            }},
            { "Tiphareth", new List<Color> {
                Hex("#FFD700"), Hex("#FFFF00"), Hex("#FFBF00"), Hex("#F4C430")
            }},
            { "Netzach", new List<Color> {
                Hex("#00A86B"), Hex("#90EE90"), Hex("#808000"), Hex("#9ACD32")
            }},
            { "Hod", new List<Color> {
                Hex("#FFA500"), Hex("#FFB733"), Hex("#FF4500"), Hex("#FF8C00")
            }},
            { "Yesod", new List<Color> {
                Hex("#800080"), Hex("#4B0082"), Hex("#9370DB"), Hex("#BA55D3")
            }},
            { "Malkuth", new List<Color> {
                Hex("#FFF44F"), Hex("#708238"), Hex("#000000"), Hex("#654321")
            }},
        };

        int counter = 1;
        foreach (var kv in paletteData)
        {
            var palette = ScriptableObject.CreateInstance<PolyhedronPalette>();
            palette.colors = kv.Value;
            string assetPath = $"Assets/Mindeleev/Palettes/Palette{counter++:00}_{kv.Key}.asset";
            AssetDatabase.CreateAsset(palette, assetPath);
            EditorUtility.SetDirty(palette);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Sephiroth palettes generated!");
    }

    static Color Hex(string hex)
    {
        Color color;
        ColorUtility.TryParseHtmlString(hex, out color);
        return color;
    }
}
#endif
