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

        // Diagnostic log to trace rebuild calls and identify missed rebuilds.
        var polytronComp = GetComponent<Polytron>();
        string idInfo = polytronComp != null ? $"polytron_id={polytronComp.sealNumber}" : "no-polytron";
        Debug.Log($"[RebuildMesh] {idInfo} GameObject='{gameObject.name}' recipe='{recipe}'");

        // Defensive parse: if the recipe is malformed (for example missing a base
        // uppercase polyhedron character) the parser will throw. Catch that so
        // starting the editor doesn't abort and we get a useful error with the
        // offending GameObject name and recipe value.
        try
        {
            _recipe = PolyhedronRecipeParser.Parse(recipe);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"[PolyhedronGenerator] Failed to parse recipe '{recipe}' on GameObject '{gameObject.name}': {ex.Message}");
        }

        currentPaletteIndex = _recipe.PaletteIdx;

        var palette = GetCurrentPalette();
        // Debug.Log($"[RebuildMesh] palette: {palette}, currentPaletteIndex: {currentPaletteIndex}");
        var polyData = PolyhedronRecipeBuilder.Build(_recipe, palette.colors.Count);
        var polyFinalData = Polyhedronisme.ApplyFlatShade(polyData);

        // Diagnostics: record previous mesh instance id
        int? prevMeshId = null;
        try { prevMeshId = filter.mesh != null ? (int?)filter.mesh.GetInstanceID() : null; } catch { prevMeshId = null; }

        var newMesh = Polyhedronisme.BuildMesh(polyFinalData, palette);
        filter.mesh = newMesh;
        ApplyPolyhedronMaterial(renderer);

        int? newMeshId = null;
        try { newMeshId = newMesh != null ? (int?)newMesh.GetInstanceID() : null; } catch { newMeshId = null; }

        Debug.Log($"[RebuildMesh] mesh ids prev={prevMeshId?.ToString() ?? "null"} new={newMeshId?.ToString() ?? "null"}");

        // Ensure renderer enabled so changes are visible immediately. Also toggle to force GPU update
        if (renderer != null)
        {
            if (!renderer.enabled)
            {
                renderer.enabled = true;
                Debug.Log($"[RebuildMesh] enabled renderer for {gameObject.name}");
            }
            // Force a renderer refresh by toggling enabled briefly. This helps cases
            // where the mesh change doesn't immediately appear in the Scene/Game view
            // on some platforms or when occlusion/visibility caches are stale.
            try
            {
                renderer.enabled = false;
                renderer.enabled = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RebuildMesh] renderer toggle failed: {ex}");
            }
        }

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

