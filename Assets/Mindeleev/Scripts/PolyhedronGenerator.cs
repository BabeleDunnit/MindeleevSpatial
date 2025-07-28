using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolyhedronGenerator : MonoBehaviour
{

    /* ------------------------------------------------------------------
     *  Inspector settings
     * ----------------------------------------------------------------*/
    public string recipeString = "C"; // default Cube
    public PolyhedronPalette palette;
    private bool showVertexIndices = false;
    public Material polyhedronMaterial; // Add this field
    public PolyhedronRecipe Recipe { get; set; }

    /* ------------------------------------------------------------------ */
    public virtual void Start()
    {
        /*
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        // var polyData = Polyhedronisme.ParsePolyhedronRecipe(polyhedronRecipe);
        // Debug.Log($"PolyhedronGenerator::Start() recipeString: {recipeString}");
        Recipe = PolyhedronRecipeParser.Parse(recipeString);
        var polyData = PolyhedronRecipeBuilder.Build(Recipe, palette.colors.Count);
        var polyFinalData = Polyhedronisme.ApplyFlatShade(polyData);
        filter.mesh = Polyhedronisme.BuildMesh(polyFinalData, palette);
        ApplyPolyhedronMaterial(renderer);

        if (showVertexIndices)
        {
            ShowVertexIndices(polyData.Item1); // Use logical vertices
        }
        */


        RebuildMesh();
    }

    public void RebuildMesh()
    {

        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        // var polyData = Polyhedronisme.ParsePolyhedronRecipe(polyhedronRecipe);
        // Debug.Log($"PolyhedronGenerator::Start() recipeString: {recipeString}");
        Recipe = PolyhedronRecipeParser.Parse(recipeString);
        var polyData = PolyhedronRecipeBuilder.Build(Recipe, palette.colors.Count);
        var polyFinalData = Polyhedronisme.ApplyFlatShade(polyData);
        filter.mesh = Polyhedronisme.BuildMesh(polyFinalData, palette);
        ApplyPolyhedronMaterial(renderer);

        if (showVertexIndices)
        {
            ShowVertexIndices(polyData.Item1); // Use logical vertices
        }
    }

    /* ------------------------- MATERIAL ------------------------------ */
    void ApplyPolyhedronMaterial(MeshRenderer renderer)
    {
        if (polyhedronMaterial == null)
        {
            Debug.LogError("PolyhedronFlatShaded material not assigned in inspector");
            return;
        }

        renderer.material = polyhedronMaterial;
        // Debug.Log($"Successfully applied material on {Application.platform}");
    }

    private void ShowVertexIndices(Vector3[] vertices)
    {
        Debug.Log($"[ShowVertexIndices] vertices count: {vertices.Length}");
        for (int i = 0; i < vertices.Length; i++)
        {
            GameObject label = new GameObject($"VertexLabel_{i}");
            label.transform.SetParent(this.transform, false);
            label.transform.localPosition = vertices[i];
            label.transform.localScale = Vector3.one * 0.15f; // Adjust for readability

            var text = label.AddComponent<TextMesh>();
            text.text = i.ToString();
            text.fontSize = 48;
            text.characterSize = 0.2f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.black;
        }
    }
}

