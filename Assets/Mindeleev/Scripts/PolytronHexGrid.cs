using UnityEngine;
using TMPro;

// This script creates a hex grid using HexCoords (pointy-topped) and instantiates Polytrons with recipes by ring.
public class PolytronHexGrid : MonoBehaviour
{
    public GameObject polytronPrefab; // Assign in inspector
    public GameObject polytronTilePrefab; // Assign in inspector

    // public float hexDistance = 2.0f;  // Distance between hex centers
    public float polytronScale = 0.3f;

    void Start()
    {
        Vector2 center2D = new Vector2(transform.position.x, transform.position.z);
        int maxRing = 3;
        int polytronNumber = 0;

        for (int ring = 0; ring <= maxRing; ring++)
        {
            int hexesInRing = ring == 0 ? 1 : 6 * ring;
            for (int i = 0; i < hexesInRing; i++)
            {
                HexCoord hex;
                if (ring == 0)
                {
                    hex = new HexCoord(0, 0); // Center
                }
                else
                {
                    // Use AtPolar to get the hex at (ring, i) in polar coordinates
                    hex = HexCoord.AtPolar(ring, i);
                }

                // Get world position for hex center
                Vector2 hexPos2D = hex.Position() + center2D;
                Vector3 position = new Vector3(hexPos2D.x, transform.position.y, hexPos2D.y);

                GameObject poly = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
                poly.transform.localScale = transform.localScale * polytronScale;

                var polyGen = poly.GetComponent<PolyhedronGenerator>();
                if (polyGen != null)
                {
                    string recipe = ring switch
                    {
                        0 => "C",
                        1 => "tC",
                        2 => "ttC",
                        3 => "ltC",
                        _ => "C"
                    };
                    polyGen.recipeString = recipe;
                }

                poly.name = $"HexP_{polytronNumber}_R{ring}_I{i}";
                CreateLabel(poly, poly.name, position);

                polytronNumber++;

                // add a "pavement"
                float angleToCenter = hex.PolarAngle();
                Quaternion pavementRotation = Quaternion.Euler(0f, - angleToCenter * 360f / 6.28f, 0f);
                Vector3 pavementPosition = new Vector3(hexPos2D.x, transform.position.y - 1f, hexPos2D.y);
                GameObject tile = Instantiate(polytronTilePrefab, pavementPosition, pavementRotation, transform);
                tile.transform.localScale = new Vector3(0.6f, 0.01f, 0.8f);

                polyGen = tile.GetComponent<PolyhedronGenerator>();
                if (polyGen != null)
                {
                    string recipe = ring switch
                    {
                        0 => "O",
                        1 => "tO",
                        2 => "ttO",
                        3 => "ltO",
                        _ => "O"
                    };
                    polyGen.recipeString = recipe;
                }
            }
        }
    }

    private void CreateLabel(GameObject parent, string recipe, Vector3 position)
    {
        GameObject label = new GameObject($"Label_{recipe}");
        label.transform.parent = parent.transform;
        label.transform.localPosition = Vector3.down * 1.5f;

        TextMeshPro tmpText = label.AddComponent<TextMeshPro>();
        tmpText.text = recipe;
        tmpText.fontSize = 3;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.enableAutoSizing = true;
        tmpText.fontSizeMin = 1;
        tmpText.fontSizeMax = 2;
        tmpText.material = new Material(Shader.Find("TextMeshPro/Mobile/Distance Field"));
        label.transform.localRotation = Quaternion.identity;
        RectTransform rectTransform = tmpText.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(2, 0.5f);
    }
}