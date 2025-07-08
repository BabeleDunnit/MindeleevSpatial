using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Globalization;
using TMPro;



public class PolyhedraEnumerationGrid : MonoBehaviour
{
    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public float distance = 2.0f;
    public float scale = 0.3f;
    void Start()
    {

        int polytronNumber = 0;
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                for (int z = 0; z < 10; z++)
                {
                    string recipe = PolyhedronRecipeEnumerator.IntToRecipe(polytronNumber);
                    Debug.Log($"{polytronNumber} {recipe}");
                    Vector3 offset = new Vector3(x * distance, y * distance, z * distance);
                    Vector3 position = transform.position + offset;

                    GameObject poly = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
                    poly.transform.localScale = transform.localScale * scale;

                    var polyGen = poly.GetComponent<PolyhedronGenerator>();
                    if (polyGen != null)
                    {
                        polyGen.polyhedronRecipe = recipe;
                    }

                    poly.name = $"P_{polytronNumber}_{recipe}";

                    CreateLabel(poly, poly.name, position);

                    polytronNumber++;

                }

            }

        }


    }


    private void CreateLabel(GameObject parent, string recipe, Vector3 position)
    {
        // Create a TextMeshPro object for the label
        GameObject label = new GameObject($"Label_{recipe}");
        label.transform.parent = parent.transform;
        label.transform.localPosition = Vector3.down * 1.5f;
        
        // Add TextMeshPro component instead of TextMesh
        TextMeshPro tmpText = label.AddComponent<TextMeshPro>();
        tmpText.text = recipe;
        tmpText.fontSize = 3;  // TMP uses different scale for font size
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        
        // Configure the TMP text
        tmpText.enableAutoSizing = true;
        tmpText.fontSizeMin = 1;
        tmpText.fontSizeMax = 2;
        
        // Set material and other rendering properties
        tmpText.material = new Material(Shader.Find("TextMeshPro/Mobile/Distance Field"));
        
        // Make text more readable by placing it vertically and facing forward
        label.transform.localRotation = Quaternion.identity;
        
        // Set rect transform properties
        RectTransform rectTransform = tmpText.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(2, 0.5f);  // Width and height of the text area
    
        // Optional: Add horizontal billboard script for camera facing
        // label.AddComponent<HorizontalBillboard>();
    }


}
