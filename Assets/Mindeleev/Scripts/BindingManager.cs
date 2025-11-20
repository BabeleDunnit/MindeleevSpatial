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
        // Record the tile recipe the polytron just bound to as an emanation (if available)
        try
        {
            if (ps != null)
            {
                var map = engine.gridCellsMap;
                if (map != null && map.ContainsKey(ps.hexCoord) && map[ps.hexCoord].tile != null)
                {
                    string tileRecipe = map[ps.hexCoord].tile.recipe;
                    p.AddEmanation(tileRecipe);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BindPolytronToSink] failed to record emanation for polytron_id={p?.sealNumber}: {ex}");
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
                Debug.Assert(hckv.Value.sink.boundPolytron == null);
                BindPolytronToSink(architron, hckv);
                Debug.Assert(hckv.Value.sink.boundPolytron == architron);
                Debug.Assert(architron.boundSink = hckv.Value.sink);
                Debug.Assert(architron.boundSink.boundPolytron = architron);
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

            if (polytronBoundToSink && polytronBoundToSink.recipe != hckv.Value.tile.recipe)
            {
                if (polytronBoundToSink.reservedForGenetics)
                {
                    Debug.Log($"[UnbindNonMatchingPolytrons] SKIPPING reserved friend polytron_id={polytronBoundToSink.sealNumber} (recipe mismatch but reserved)");
                    polytronsThatWillNotMove++;
                    continue;
                }
                UnbindPolytron(polytronBoundToSink);
            }
            else
            {
                polytronsThatWillNotMove++;
            }
        }

        Debug.Log($"[UnbindNonMatchingPolytrons] polytronsThatWillNotMove: {polytronsThatWillNotMove}");
    }
}
