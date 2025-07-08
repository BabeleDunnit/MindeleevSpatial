using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Globalization;
using TMPro;



public class PolytronOrbital : MonoBehaviour
{
    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public float orbitalRadius = 2.0f;
    public float orbitalYOffset = 0.0f;

    private void GenerateOrbital()
    {
        var generator = GetComponent<PolyhedronGenerator>();
        if (generator == null || polytronPrefab == null)
        {
            Debug.LogWarning("PolyhedronGenerator or PolytronPrefab not assigned.");
            return;
        }

        // Extract the base polyhedron (last uppercase letter in the recipe)
        string recipe = generator.polyhedronRecipe;
        char basePoly = recipe.LastOrDefault(c => char.IsUpper(c));
        if (basePoly == default)
        {
            Debug.LogWarning("No base polyhedron found in recipe: " + recipe);
            return;
        }

        // Prepare the base recipe string
        string baseRecipe = basePoly.ToString();

        // try to clone the Pauli/Aufbau sequence
        // use something analogous to quantic numbers

        /*
          for (int n = 1; n <= 7; n++) {
    for (int l = 0; l <= n - 1; l++) {
      for (int m = -l; m <= l; m++) {
        for (int s = -1; s <= 1; s += 2) {
*/



        string[] ops = { "k", "a", "n", "l" };

        for (int i = 2; i < 6; i++)
        {
            orbitalRadius = i * 2;
            for (int j = 0; j < i; j++)
            {
                if (i < 2) continue;
                float angle = j * Mathf.PI * 2f / (float)i;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * orbitalRadius,
                    orbitalYOffset,
                    Mathf.Sin(angle) * orbitalRadius
                );
                Vector3 position = transform.position + offset;

                GameObject orbital = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
                orbital.transform.localScale = transform.localScale * 0.3f; // Match nucleus scale

                // Set the base recipe
                var orbitalGen = orbital.GetComponent<PolyhedronGenerator>();
                if (orbitalGen != null)
                {
                    orbitalGen.polyhedronRecipe = string.Concat(Enumerable.Repeat(ops[i - 2], j)) + baseRecipe;
                }

                orbital.name = $"Orbital_{baseRecipe}_{i + 1}";
            }
        }

    }

    void Start()
    {
        GenerateOrbital();
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
