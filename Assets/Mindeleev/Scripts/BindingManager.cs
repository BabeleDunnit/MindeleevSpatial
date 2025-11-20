using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Encapsulates binding/unbinding and sink allocation logic for polytrons.
/// This class is an incremental refactor target extracted from MutatronEngine.
/// It references the owning MutatronEngine for scene data.
/// </summary>
public class BindingManager
{
    private MutatronEngine engine;

    public BindingManager(MutatronEngine engine)
    {
        this.engine = engine;
    }

    public void BindPolytronToSink(Polytron p, KeyValuePair<HexCoord, MutatronEngine.HexCellData> hckv)
    {
        BindPolytronToSink(p, hckv.Value.sink);
    }

    public void BindPolytronToSink(Polytron p, PolytronSink ps)
    {
        if (p == null) return;

        // Fail-fast: if binding to the Mutatron center and the sink is already
        // owned by another polytron, throw instead of silently reassigning.
        if (ps != null && engine != null && engine.gridCellsMap != null && engine.gridCellsMap.ContainsKey(ps.hexCoord))
        {
            var hcd = engine.gridCellsMap[ps.hexCoord];
            if (engine.IsMutatronCenter(hcd) && ps.boundPolytron != null && ps.boundPolytron != p)
            {
                throw new InvalidOperationException($"[BindPolytronToSink] center sink {ps.name} unexpectedly owned by polytron_id={ps.boundPolytron.sealNumber}");
            }
        }

        if (p.boundSink != null)
        {
            if (p.boundSink.boundPolytron == p) p.boundSink.boundPolytron = null;
            p.boundSink = null;
        }

        if (ps != null && ps.boundPolytron != null && ps.boundPolytron != p)
        {
            var prev = ps.boundPolytron;
            if (prev.boundSink == ps) prev.boundSink = null;
            ps.boundPolytron = null;
        }

        p.boundSink = ps;
        if (ps != null)
        {
            ps.boundPolytron = p;
            if (p.reservedForGenetics)
                Debug.LogWarning($"[BindPolytronToSink] WARNING: REBINDING reserved polytron_id={p.sealNumber} (reservedForGenetics={p.reservedForGenetics}) to sink={ps.name}");
            else
                Debug.Log($"[BindPolytronToSink] binding polytron_id={p.sealNumber} to sink={ps.name}");
        }
        // If this sink is part of the Mutatron (not home), immediately apply
        // the tile's operators (transformation) to the polytron while preserving
        // the polytron's radix (palette index and base polyhedron). Let any
        // parsing/validation exceptions bubble up (fail-fast).
        if (ps != null && engine != null && engine.gridCellsMap != null && engine.gridCellsMap.ContainsKey(ps.hexCoord))
        {
            var hcd = engine.gridCellsMap[ps.hexCoord];
            // ring <= actualLevelConfig.actualRingsCount => on Mutatron
            if (hcd.ring <= engine.actualLevelConfig.actualRingsCount && hcd.tile != null && !p.reservedForGenetics)
            {
                string tileRecipe = hcd.tile.recipe;
                var parsedTile = PolyhedronRecipeParser.Parse(tileRecipe);
                var parsedPoly = PolyhedronRecipeParser.Parse(p.recipe);

                var newRecipeObj = new PolyhedronRecipe
                {
                    Tokens = parsedTile.Tokens,
                    PaletteIdx = parsedPoly.PaletteIdx,
                    BasePolyhedron = parsedPoly.BasePolyhedron
                };

                string newRecipe = newRecipeObj.ToString();

                if (newRecipe != p.recipe)
                {
                    // Use engine helper to rebuild so AddEmanation and notifications are centralized
                    engine.RebuildPolytronFromRecipe(p, newRecipe);
                }
            }
        }

        // Ensure the engine and any subscribed UI are notified of this state change
        engine.NotifyPolytronStateChanged(p);
    }

    public void UnbindPolytron(Polytron p)
    {
        if (p == null) return;
        var sink = p.boundSink;
        if (sink != null)
        {
            Debug.Log($"[UnbindPolytron] UNBINDING polytron_id={p.sealNumber} from sink={sink.name}");
            if (sink.boundPolytron == p) sink.boundPolytron = null;
            p.boundSink = null;
        }
        // Notify engine so UI and other listeners can update
        engine.NotifyPolytronStateChanged(p);
    }

    public Polytron FindPolytronToBind()
    {
        var polytrons = engine.polytrons;

        var toReturn = polytrons.Where(p => !p.reservedForGenetics && p.boundSink != null && engine.gridCellsMap[p.boundSink.hexCoord].ring == 12);

        if (toReturn.Count() == 0)
        {
            toReturn = polytrons.Where(p => !p.reservedForGenetics && p.boundSink == null);
        }

        return toReturn.FirstOrDefault();
    }

    public void UpdatePolytronsSinks()
    {
        var gridCellsMap = engine.gridCellsMap;
        var polytrons = engine.polytrons;
        var actualLevelConfig = engine.actualLevelConfig;

        var tilesRecipes = gridCellsMap
            .Where(c => c.Value.ring <= actualLevelConfig.actualRingsCount)
            .Select(c => c.Value.tile.recipe)
            .ToList();

        var matchingRecipeUnboundPolytrons = polytrons.Where(p => !p.reservedForGenetics && tilesRecipes.Contains(p.recipe) && p.boundSink == null && p.isArchitron == false);
        Debug.Log($"[UpdatePolytronsSinks] unbound polytrons with matching recipe: {matchingRecipeUnboundPolytrons.Count()} (excluding {polytrons.Count(p => p.reservedForGenetics)} reserved genetic friends)");
        Debug.Log($"[UpdatePolytronsSinks] reserved polytrons: {string.Join(",", polytrons.Where(p => p.reservedForGenetics).Select(p => p.sealNumber))}");

        int rebuiltPolytrons = 0;
        int movedPolytrons = 0;

        foreach (var matchingRecipeUnboundPolytron in matchingRecipeUnboundPolytrons)
        {
            foreach (var hckv in gridCellsMap)
            {
                if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

                if (engine.IsMutatronCenter(hckv.Value)) continue;

                if (hckv.Value.sink.boundPolytron == null && hckv.Value.tile.recipe == matchingRecipeUnboundPolytron.recipe)
                {
                    BindPolytronToSink(matchingRecipeUnboundPolytron, hckv);
                    movedPolytrons++;
                    break;
                }
            }
        }

        foreach (var pp in matchingRecipeUnboundPolytrons.Where(p => p.boundSink == null && !p.reservedForGenetics))
        {
            var unboundSinkOnExternalRing = gridCellsMap.Where(hckv => hckv.Value.ring == 12 && hckv.Value.sink.boundPolytron == null).Last();
            BindPolytronToSink(pp, unboundSinkOnExternalRing);
        }

        foreach (var hckv in gridCellsMap)
        {
            if (hckv.Value.ring > actualLevelConfig.actualRingsCount) continue;

            if (engine.IsMutatronCenter(hckv.Value))
            {
                Polytron architron = polytrons[engine.architronIdx];
                // Ensure the Architron is bound to the Mutatron center.
                // Don't assume the sink is free (it may have been bound earlier);
                // BindPolytronToSink will safely handle unbinding previous owners.
                BindPolytronToSink(architron, hckv);

                // Sanity check: after binding, the sink should point to the architron.
                if (hckv.Value.sink.boundPolytron != architron || architron.boundSink != hckv.Value.sink)
                {
                    Debug.LogWarning($"[UpdatePolytronsSinks] Architron binding inconsistent: sink.owner={hckv.Value.sink.boundPolytron?.sealNumber.ToString() ?? "null"}, architron.boundSink={(architron.boundSink==null?"null":architron.boundSink.name)}");
                }
                continue;
            }

            if (hckv.Value.sink.boundPolytron == null)
            {
                Polytron p = FindPolytronToBind();
                if (p && !p.reservedForGenetics)
                {
                    BindPolytronToSink(p, hckv);
                    string tileRecipe = hckv.Value.tile.recipe;
                    rebuiltPolytrons++;
                }
            }
        }

        Debug.Log($"[UpdatePolytronsSinks] moved: {movedPolytrons}, rebuilt: {rebuiltPolytrons}");
    }

    public void SendUnboundPolytronsHome()
    {
        var unboundPolytrons = engine.polytrons.Where(p => p.boundSink == null && !p.reservedForGenetics).ToList();
        Debug.Log($"[SendUnboundPolytronsHome] sending home {unboundPolytrons.Count} unbound polytrons (skipping {engine.polytrons.Count(p => p.reservedForGenetics)} reserved)");
        for (int i = 0; i < unboundPolytrons.Count; i++)
        {
            BindPolytronToSink(unboundPolytrons[i], engine.polytronsHomes[unboundPolytrons[i].sealNumber]);
        }
    }

    public void UnbindNonMatchingPolytrons()
    {
        int polytronsThatWillNotMove = 0;
        foreach (var hckv in engine.gridCellsMap)
        {
            if (hckv.Value.ring > engine.actualLevelConfig.actualRingsCount) continue;

            PolytronSink sink = hckv.Value.sink;
            Polytron polytronBoundToSink = sink.boundPolytron;
            if (polytronBoundToSink)
            {
                string tileRecipe = hckv.Value.tile != null ? hckv.Value.tile.recipe : null;
                if (!string.IsNullOrEmpty(tileRecipe) && polytronBoundToSink.recipe != tileRecipe)
                {
                    if (polytronBoundToSink.reservedForGenetics)
                    {
                        Debug.Log($"[UnbindNonMatchingPolytrons] SKIPPING reserved friend polytron_id={polytronBoundToSink.sealNumber} (recipe mismatch but reserved)");
                        polytronsThatWillNotMove++;
                        continue;
                    }

                    // Instead of unbinding, update the polytron's operators sequence to match the tile
                    // while preserving the polytron's radix (palette index and base polyhedron).
                    var parsedTile = PolyhedronRecipeParser.Parse(tileRecipe);
                    var parsedPoly = PolyhedronRecipeParser.Parse(polytronBoundToSink.recipe);

                    var newRecipeObj = new PolyhedronRecipe
                    {
                        Tokens = parsedTile.Tokens,
                        PaletteIdx = parsedPoly.PaletteIdx,
                        BasePolyhedron = parsedPoly.BasePolyhedron
                    };

                    string newRecipe = newRecipeObj.ToString();

                    // Only rebuild if the resulting recipe differs
                    if (newRecipe != polytronBoundToSink.recipe)
                    {
                        Debug.Log($"[UnbindNonMatchingPolytrons] Updating polytron_id={polytronBoundToSink.sealNumber} recipe -> {newRecipe} (preserving radix)");
                        // Use engine helper to rebuild so AddEmanation and notifications are centralized
                        engine.RebuildPolytronFromRecipe(polytronBoundToSink, newRecipe);
                    }
                }
                else
                {
                    polytronsThatWillNotMove++;
                }
            }
        }

        Debug.Log($"[UnbindNonMatchingPolytrons] polytronsThatWillNotMove: {polytronsThatWillNotMove}");
    }
}
