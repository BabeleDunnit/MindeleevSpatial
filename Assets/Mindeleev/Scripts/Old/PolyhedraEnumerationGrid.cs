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
    public bool doX = true;
    public bool doY = true;
    public bool doZ = true;

    void Start()
    {
        int xMax = doX ? 10 : 1;
        int yMax = doY ? 10 : 1;
        int zMax = doZ ? 10 : 1;

        var chars = new HashSet<char> { 't', 'k', 'n', 'a', 'd', 'l' };
        List<string> permutedRecipes = PolyhedronRecipeUtils.AllPermutationsWithRepetition(chars, 4);

        int polytronNumber = 0;
        for (int x = 0; x < xMax; x++)
        {
            for (int y = 0; y < yMax; y++)
            {
                for (int z = 0; z < zMax; z++)
                {


                    // string recipe = PolyhedronRecipeEnumerator_obsolete.IntToRecipe(polytronNumber);
                    string recipe = permutedRecipes[polytronNumber] + "C";



                    Debug.Log($"{polytronNumber} {recipe}");
                    Vector3 offset = new Vector3(x * distance, y * distance, z * distance);
                    Vector3 position = transform.position + offset;

                    GameObject poly = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
                    poly.transform.localScale = transform.localScale * scale;

                    // this works because is synchronous. The PolyhedronGenerator.Start() method
                    // will be called AFTER we get out from here.
                    var polyGen = poly.GetComponent<PolyhedronGenerator>();
                    if (polyGen != null)
                    {
                        polyGen.recipe = recipe;
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
