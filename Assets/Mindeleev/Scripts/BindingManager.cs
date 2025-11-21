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
        // Mirror engine reconciliation behavior here so the bindingManager path behaves identically.
        var opsToSinks = new Dictionary<string, List<KeyValuePair<HexCoord, MutatronEngine.HexCellData>>>();
        KeyValuePair<HexCoord, MutatronEngine.HexCellData>? centerHckv = null;
        foreach (var hckv in engine.gridCellsMap)
        {
            if (hckv.Value.ring > engine.actualLevelConfig.actualRingsCount) continue;
            if (engine.IsMutatronCenter(hckv.Value)) { centerHckv = hckv; continue; }
            if (hckv.Value.tile == null) continue;
            try
            {
                var parsed = PolyhedronRecipeParser.Parse(hckv.Value.tile.recipe);
                string ops = parsed.OperatorsSequence();
                if (!opsToSinks.ContainsKey(ops)) opsToSinks[ops] = new List<KeyValuePair<HexCoord, MutatronEngine.HexCellData>>();
                opsToSinks[ops].Add(hckv);
            }
            catch (Exception) { }
        }

        var opsToPolytronsOnMut = new Dictionary<string, List<Polytron>>();
        var polytronsOnMutList = new List<Polytron>();
        foreach (var p in engine.polytrons)
        {
            if (p == null) continue;
            if (p.isArchitron) continue;
            if (p.boundSink == null) continue;
            if (!engine.gridCellsMap.ContainsKey(p.boundSink.hexCoord)) continue;
            var hcd = engine.gridCellsMap[p.boundSink.hexCoord];
            if (!engine.IsMutatronCell(hcd) && !engine.IsMutatronCenter(hcd)) continue;
            if (p.reservedForGenetics) continue;
            try
            {
                var parsed = PolyhedronRecipeParser.Parse(p.recipe);
                string ops = parsed.OperatorsSequence();
                if (!opsToPolytronsOnMut.ContainsKey(ops)) opsToPolytronsOnMut[ops] = new List<Polytron>();
                opsToPolytronsOnMut[ops].Add(p);
                polytronsOnMutList.Add(p);
            }
            catch (Exception) { }
        }

        // center handling
        if (centerHckv.HasValue && engine.polytrons != null && engine.architronIdx >= 0 && engine.architronIdx < engine.polytrons.Count)
        {
            var ch = centerHckv.Value;
            var arch = engine.polytrons[engine.architronIdx];
            if (ch.Value.tile != null && arch != null)
            {
                if (arch.boundSink != ch.Value.sink)
                {
                    BindPolytronToSink(arch, ch);
                }
                try
                {
                    var parsedTile = PolyhedronRecipeParser.Parse(ch.Value.tile.recipe);
                    var parsedArch = PolyhedronRecipeParser.Parse(arch.recipe);
                    var newRecipeObj = new PolyhedronRecipe
                    {
                        Tokens = parsedTile.Tokens,
                        PaletteIdx = parsedArch.PaletteIdx,
                        BasePolyhedron = parsedArch.BasePolyhedron
                    };
                    string newRecipe = newRecipeObj.ToString();
                    if (newRecipe != arch.recipe)
                    {
                        engine.RebuildPolytronFromRecipe(arch, newRecipe);
                    }
                }
                catch (Exception) { }
            }
        }

        // reconcile per-operator groups
        foreach (var kv in opsToSinks)
        {
            string ops = kv.Key;
            var sinks = new List<KeyValuePair<HexCoord, MutatronEngine.HexCellData>>(kv.Value);

            opsToPolytronsOnMut.TryGetValue(ops, out var polysWithOps);
            polysWithOps = polysWithOps ?? new List<Polytron>();

            var assignedPolys = new HashSet<Polytron>();
            var remainingSinks = new List<KeyValuePair<HexCoord, MutatronEngine.HexCellData>>();

            foreach (var sinkH in sinks)
            {
                var bound = sinkH.Value.sink.boundPolytron;
                if (bound != null && !bound.reservedForGenetics)
                {
                    try
                    {
                        var parsed = PolyhedronRecipeParser.Parse(bound.recipe);
                        if (parsed.OperatorsSequence() == ops)
                        {
                            assignedPolys.Add(bound);
                            continue;
                        }
                    }
                    catch (Exception) { }
                }
                remainingSinks.Add(sinkH);
            }

            var movablePolys = polysWithOps.Where(p => !assignedPolys.Contains(p)).ToList();

            int moveCount = Math.Min(movablePolys.Count, remainingSinks.Count);
            for (int i = 0; i < moveCount; i++)
            {
                var poly = movablePolys[i];
                var targetH = remainingSinks[i];
                var prevSink = poly.boundSink;
                if (prevSink != null) UnbindPolytron(poly);
                    Debug.Log($"[BindingManager] moving polytron_id={poly.sealNumber} from {prevSink?.name ?? "null"} to {targetH.Value.sink.name}");
                    BindPolytronToSink(poly, targetH);
                    Debug.Log($"[BindingManager] moved polytron_id={poly.sealNumber} now bound to {poly.boundSink?.name ?? "null"}");
                assignedPolys.Add(poly);
            }

            remainingSinks = remainingSinks.Skip(moveCount).ToList();

                if (remainingSinks.Count > 0)
            {
                var availableHomePolys = engine.polytrons.Where(p => p != null && p.boundSink == null && !p.reservedForGenetics && engine.polytronHomeCooldown.TryGetValue(p.sealNumber, out var cd) && cd <= 0).ToList();
                int callCount = Math.Min(availableHomePolys.Count, remainingSinks.Count);
                for (int i = 0; i < callCount; i++)
                {
                    var poly = availableHomePolys[i];
                    var targetH = remainingSinks[i];
                        Debug.Log($"[BindingManager] calling home polytron_id={poly.sealNumber} to sink={targetH.Value.sink.name}");
                    try
                    {
                        var parsedPoly = PolyhedronRecipeParser.Parse(poly.recipe);
                        var parsedTile = PolyhedronRecipeParser.Parse(targetH.Value.tile.recipe);
                        var newRecipeObj = new PolyhedronRecipe
                        {
                            Tokens = parsedTile.Tokens,
                            PaletteIdx = parsedPoly.PaletteIdx,
                            BasePolyhedron = parsedPoly.BasePolyhedron
                        };
                        string newRecipe = newRecipeObj.ToString();
                        engine.RebuildPolytronFromRecipe(poly, newRecipe);
                    }
                    catch (Exception) { }
                        BindPolytronToSink(poly, targetH);
                        Debug.Log($"[BindingManager] called polytron_id={poly.sealNumber} now bound to {poly.boundSink?.name ?? "null"}");
                }
            }

            int sinksCount = sinks.Count;
                if (polysWithOps.Count > sinksCount)
            {
                var surplus = polysWithOps.Where(p => !assignedPolys.Contains(p)).ToList();
                foreach (var sPoly in surplus)
                {
                        Debug.Log($"[BindingManager] sending surplus polytron_id={sPoly.sealNumber} home from sink={sPoly.boundSink?.name ?? "null"}");
                        UnbindPolytron(sPoly);
                        BindPolytronToSink(sPoly, engine.polytronsHomes[sPoly.sealNumber]);
                        engine.polytronHomeCooldown[sPoly.sealNumber] = 1;
                        Debug.Log($"[BindingManager] polytron_id={sPoly.sealNumber} sent home and cooldown set");
                }
            }
        }

        var opsPresent = new HashSet<string>(opsToSinks.Keys);
        foreach (var p in polytronsOnMutList)
        {
            try
            {
                var parsed = PolyhedronRecipeParser.Parse(p.recipe);
                if (!opsPresent.Contains(parsed.OperatorsSequence()))
                {
                    Debug.Log($"[BindingManager] polytron_id={p.sealNumber} operators died on mutatron; sending home");
                    UnbindPolytron(p);
                    BindPolytronToSink(p, engine.polytronsHomes[p.sealNumber]);
                    engine.polytronHomeCooldown[p.sealNumber] = 1;
                    Debug.Log($"[BindingManager] polytron_id={p.sealNumber} sent home due to dead operators");
                }
            }
            catch (Exception) { }
        }

        Debug.Log("[UnbindNonMatchingPolytrons] reconciliation complete (manager path)");
    }
}
