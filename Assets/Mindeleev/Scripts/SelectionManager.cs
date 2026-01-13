using System.Collections.Generic;
using UnityEngine;

public class SelectionManager
{
    private MutatronEngine engine;

    // Selection slots
    internal Polytron PaletteSelector { get; private set; } = null;
    internal Polytron OperatorsSelector { get; private set; } = null;

    // Architron backup recipes for restore semantics
    internal string ArchitronSavedRecipeForSelection { get; private set; } = null;   // saved when first selection is made (for permanent restore)
    private string architronHoverSavedRecipe = null;         // saved when hover preview starts
    private bool architronHoverOverrideActive = false;

    public SelectionManager(MutatronEngine engine)
    {
        this.engine = engine;
    }

    internal void ClearPaletteSelection()
    {
        engine.ClearGeneticFriends();

        var old = PaletteSelector;
        if (old != null)
        {
            var oldOutline = old.GetComponent<PointerOutlineStateController>();
            oldOutline?.SetState(0);
            PaletteSelector = null;
            // notify engine so automatic entangle can re-evaluate this polytron
            engine.NotifyPolytronStateChanged(old);
        }
    }

    internal void ClearOperatorsSelection()
    {
        engine.ClearGeneticFriends();

        var old = OperatorsSelector;
        if (old != null)
        {
            var oldOutline = old.GetComponent<PointerOutlineStateController>();
            oldOutline?.SetState(0);
            OperatorsSelector = null;
            // notify engine so automatic entangle can re-evaluate this polytron
            engine.NotifyPolytronStateChanged(old);
        }
    }

    internal void DeselectAllPolytrons()
    {
        Debug.Log("[SelectionManager] DeselectAllPolytrons()");

        engine.ClearGeneticFriends();

        foreach (var p in engine.polytrons)
        {
            if (p == null) continue;
            var outline = p.GetComponent<PointerOutlineStateController>();
            outline?.SetState(0);
        }

        PaletteSelector = null;
        OperatorsSelector = null;

        architronHoverOverrideActive = false;
        architronHoverSavedRecipe = null;

        var arch = engine.polytrons[engine.architronIdx];
        if (arch == null) return;

        if (!string.IsNullOrEmpty(ArchitronSavedRecipeForSelection))
        {
            engine.ApplyRecipeToArchitron(ArchitronSavedRecipeForSelection);
            ArchitronSavedRecipeForSelection = null;
        }
        else if (!string.IsNullOrEmpty(architronHoverSavedRecipe))
        {
            engine.ApplyRecipeToArchitron(architronHoverSavedRecipe);
            architronHoverSavedRecipe = null;
        }
        else
        {
            engine.ApplyRecipeToArchitron(arch.recipe);
        }

        // Trigger an engine state refresh so automatic entangle overlays are recomputed
        engine.NotifyPolytronStateChanged(arch);
    }

    internal void OnPolytronClicked(Polytron p)
    {
        if (p == null || p.isArchitron) return;

        var pOutline = p.GetComponent<PointerOutlineStateController>();

        var archBefore = engine.polytrons.Count > 0 ? engine.polytrons[engine.architronIdx] : null;
        if (archBefore != null && ArchitronSavedRecipeForSelection == null)
        {
            ArchitronSavedRecipeForSelection = archBefore.recipe;
        }

        engine.ClearGeneticFriends();

        if (PaletteSelector == p)
        {
            if (OperatorsSelector == null)
            {
                ClearPaletteSelection();
                OperatorsSelector = p;
                pOutline?.SetState(2);
            }
            else
            {
                ClearPaletteSelection();
            }
        }
        else if (OperatorsSelector == p)
        {
            ClearOperatorsSelection();
        }
        else
        {
            if (PaletteSelector == null)
            {
                PaletteSelector = p;
                pOutline?.SetState(1);
            }
            else if (OperatorsSelector == null)
            {
                OperatorsSelector = p;
                pOutline?.SetState(2);
            }
            else
            {
                ClearOperatorsSelection();
                OperatorsSelector = p;
                pOutline?.SetState(2);
            }
        }

        var arch = engine.polytrons[engine.architronIdx];
        if ((PaletteSelector != null || OperatorsSelector != null) && ArchitronSavedRecipeForSelection == null)
            ArchitronSavedRecipeForSelection = arch.recipe;

        if (PaletteSelector != null && OperatorsSelector != null)
        {
            if (PaletteSelector.recipe == OperatorsSelector.recipe)
            {
                string combined = engine.CombineUsingSelectors(PaletteSelector, OperatorsSelector);
                engine.ApplyRecipeToArchitron(combined);
            }
            else
            {
                var crossovers = engine.ComputeCrossoverRecipes(PaletteSelector, OperatorsSelector);
                if (crossovers.Count == 1)
                {
                    engine.ApplyRecipeToArchitron(crossovers[0]);
                }
                else
                {
                    engine.SetupGeneticFriends(crossovers);
                }
            }
        }
        else
        {
            if (PaletteSelector == null && OperatorsSelector == null && ArchitronSavedRecipeForSelection != null)
            {
                engine.ApplyRecipeToArchitron(ArchitronSavedRecipeForSelection);
                ArchitronSavedRecipeForSelection = null;
            }
        }

        Debug.Log($"[SelectionManager.OnPolytronClicked] palette: {(PaletteSelector == null ? "null" : PaletteSelector.name)}, operators: {(OperatorsSelector == null ? "null" : OperatorsSelector.name)}");
    }

    internal void OnPolytronPointerEnter(Polytron hovered)
    {
        if (hovered == null || hovered.isArchitron) return;

        if (engine.geneticModeActive) return;

        var arch = engine.polytrons[engine.architronIdx];

        if (hovered == PaletteSelector || hovered == OperatorsSelector)
        {
            if (!architronHoverOverrideActive)
            {
                architronHoverSavedRecipe = arch.recipe;
                architronHoverOverrideActive = true;
            }

            string radix = engine.GetRadixRecipe(hovered);
            engine.ApplyRecipeToArchitron(radix);
            return;
        }

        if (PaletteSelector == null && OperatorsSelector == null) return;

        if (!architronHoverOverrideActive)
        {
            architronHoverSavedRecipe = arch.recipe;
            architronHoverOverrideActive = true;
        }

        string newRecipe = null;

        if (PaletteSelector != null && OperatorsSelector != null)
        {
            newRecipe = engine.CombineUsingSelectors(PaletteSelector, OperatorsSelector);
        }
        else if (PaletteSelector != null)
        {
            string hoveredOps = hovered._recipe?.OperatorsSequence() ?? "";
            string paletteIdxStr = PaletteSelector._recipe?.PaletteIdx.ToString("D2") ?? "00";
            char baseChar = PaletteSelector._recipe?.BasePolyhedron ?? 'C';
            newRecipe = hoveredOps + paletteIdxStr + baseChar;
        }
        else
        {
            string ops = OperatorsSelector._recipe?.OperatorsSequence() ?? "";
            string paletteIdxStr = hovered._recipe?.PaletteIdx.ToString("D2") ?? "00";
            char baseChar = hovered._recipe?.BasePolyhedron ?? 'C';
            newRecipe = ops + paletteIdxStr + baseChar;
        }

        if (newRecipe != null)
            engine.ApplyRecipeToArchitron(newRecipe);
    }

    internal void OnPolytronPointerExit(Polytron p)
    {
        if (engine.geneticModeActive) return;

        var arch = engine.polytrons[engine.architronIdx];

        if (!architronHoverOverrideActive) return;

        if (PaletteSelector != null && OperatorsSelector != null)
        {
            string combined = engine.CombineUsingSelectors(PaletteSelector, OperatorsSelector);
            engine.ApplyRecipeToArchitron(combined);
        }
        else
        {
            if (ArchitronSavedRecipeForSelection != null)
            {
                engine.ApplyRecipeToArchitron(ArchitronSavedRecipeForSelection);
            }
            else
            {
                if (architronHoverSavedRecipe != null)
                {
                    engine.ApplyRecipeToArchitron(architronHoverSavedRecipe);
                }
            }
        }

        architronHoverOverrideActive = false;
        architronHoverSavedRecipe = null;
    }
}
