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
    public string recipe = "C"; // default Cube

    [Header("Palettes")]
    public List<PolyhedronPalette> palettes = new List<PolyhedronPalette>();
    private int currentPaletteIndex = 0;

    private bool showVertexIndices = false;
    public Material polyhedronMaterial;

    internal PolyhedronRecipe _recipe;

    /* ------------------------------------------------------------------ */
    public virtual void Start()
    {
        RebuildMesh();
    }

    public void RebuildMesh()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        /*PolyhedronRecipe*/ _recipe = PolyhedronRecipeParser.Parse(recipe);
        currentPaletteIndex = _recipe.PaletteIdx;

        var palette = GetCurrentPalette();
        // Debug.Log($"[RebuildMesh] palette: {palette}, currentPaletteIndex: {currentPaletteIndex}");
        var polyData = PolyhedronRecipeBuilder.Build(_recipe, palette.colors.Count);
        var polyFinalData = Polyhedronisme.ApplyFlatShade(polyData);
        filter.mesh = Polyhedronisme.BuildMesh(polyFinalData, palette);
        ApplyPolyhedronMaterial(renderer);

        if (showVertexIndices)
        {
            ShowVertexIndices(polyData.Item1); // Use logical vertices
        }
    }

    PolyhedronPalette GetCurrentPalette()
    {
        if (currentPaletteIndex < 0 || currentPaletteIndex >= palettes.Count)
            currentPaletteIndex = 0;
        return palettes[currentPaletteIndex];
    }

    public void SetPalette(int index)
    {
        if (palettes == null || palettes.Count == 0) return;
        currentPaletteIndex = Mathf.Clamp(index, 0, palettes.Count - 1);
        Debug.Log($"[SetPalette] currentPaletteIndex: {currentPaletteIndex}");
        RebuildMesh();
    }

    public void NextPalette()
    {
        if (palettes == null || palettes.Count == 0) return;
        currentPaletteIndex = (currentPaletteIndex + 1) % palettes.Count;
        Debug.Log($"[NextPalette] currentPaletteIndex: {currentPaletteIndex}");
        RebuildMesh();
    }

    public void PreviousPalette()
    {
        if (palettes == null || palettes.Count == 0) return;
        currentPaletteIndex = (currentPaletteIndex - 1 + palettes.Count) % palettes.Count;
        RebuildMesh();
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

    void Update()
    {
        // Example: Switch palettes with keys 1-0 (for 10 palettes)
        for (int i = 0; i < 10; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                SetPalette(i);
            }
        }
        // Example: Cycle palettes with left/right arrow
        if (Input.GetKeyDown(KeyCode.LeftArrow)) PreviousPalette();
        if (Input.GetKeyDown(KeyCode.RightArrow)) NextPalette();
    }
}

