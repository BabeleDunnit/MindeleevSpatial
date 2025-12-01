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
    
    // Track polytron positions BEFORE Evolve() to detect which ones need to move
    private Dictionary<int, (HexCoord coord, string ops)> prevolatronPositions = new();

    public BindingManager(MutatronEngine engine)
    {
        this.engine = engine;
    }

    /*
        public void BindPolytronToSink(Polytron p, KeyValuePair<HexCoord, MutatronEngine.HexCellData> hckv)
        {
            BindPolytronToSink(p, hckv.Value.sink);
        }

        public void BindPolytronToSink(Polytron p, PolytronSink ps)
        {

            Debug.Assert(p != null);
            Debug.Assert(ps != null);

            // Remember previous binding to decide whether this is a "call from home" or a simple move on the Mutatron.
            var prevBoundSink = p.boundSink;

            var hcd = engine.gridCellsMap[ps.hexCoord];
            if (engine.IsMutatronCenter(hcd) && ps.boundPolytron != null && ps.boundPolytron != p)
            {
                // trying to reassign the mutatron center
                throw new InvalidOperationException($"[BindPolytronToSink] center sink {ps.name} unexpectedly owned by polytron_id={ps.boundPolytron.sealNumber}");
            }

            // We'll defer clearing previous binding until after any pre-bind rebuild
            // so that the rebuild helper can see the polytron's prior location (home).

            // If this sink is part of the Mutatron (not home), decide whether to
            // alter the polytron's recipe. Only calls from home should change the polytron's emanation.
            // IMPORTANT: perform recipe rebuild BEFORE assigning the new binding so the polytron is
            // still recognized as "at home" by the rebuild helper.
                // ring <= actualLevelConfig.actualRingsCount => on Mutatron
                if (hcd.ring <= engine.actualLevelConfig.actualRingsCount && hcd.tile != null && !p.reservedForGenetics)
                {
                    // Only consider this a "call from home" if the polytron was actually
                    // bound at its home sink (ring 12). An unbound polytron is NOT treated
                    // as a home-call; it must be sent home and rest before being eligible.
                    bool wasAtHome = false;
                    if (prevBoundSink != null
                    && engine.gridCellsMap[prevBoundSink.hexCoord].ring == 12)
                        wasAtHome = true;

                    if (wasAtHome)
                    {
                        try
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
                                // Rebuild while the polytron is still considered at-home (prevBoundSink)
                                engine.RebuildPolytronFromRecipe(p, newRecipe);
                            }
                        }
                        catch (Exception) { }
                    }
                    else
                    {
                        // Simple move on the Mutatron: do not change the polytron's recipe.
                        Debug.Log($"[BindPolytronToSink] moved polytron_id={p.sealNumber} on Mutatron without changing recipe");
                    }
                }

            // Now that any pre-bind rebuild has run (and used prevBoundSink to decide),
            // clear previous bindings and assign the new binding.
            if (p.boundSink != null)
            {
                if (p.boundSink.boundPolytron == p) p.boundSink.boundPolytron = null;
                p.boundSink = null;
            }

            // If the target sink is already owned, clear it (we'll reassign below)
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
                var p = unboundPolytrons[i];
                BindPolytronToSink(p, engine.polytronsHomes[p.sealNumber]);
                // enforce a 3-evolve rest cooldown after being sent home so they won't be immediately eligible to be called
                engine.polytronHomeCooldown[p.sealNumber] = 3;
            }
        }

        public void SendAllPolytronsHome()
        {
            Debug.Log($"[BindingManager.SendAllPolytronsHome] sending all polytrons home (count={engine.polytrons.Count})");
            for (int i = 0; i < engine.polytrons.Count; i++)
            {
                var p = engine.polytrons[i];
                if (p == null) continue;
                try
                {
                    UnbindPolytron(p);
                    BindPolytronToSink(p, engine.polytronsHomes[p.sealNumber]);
                    // reset cooldown so they can be called immediately after level build
                    engine.polytronHomeCooldown[p.sealNumber] = 0;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BindingManager.SendAllPolytronsHome] failed to send polytron_id={p?.sealNumber.ToString() ?? "?"} home: {ex.Message}");
                    throw ex;
                }
            }
        }

        public void UnbindNonMatchingPolytrons()
        {
            Debug.Log("[BindingManager.UnbindNonMatchingPolytrons] enter (manager path)");
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

        Debug.Log($"[BindingManager.UnbindNonMatchingPolytrons] ops groups discovered: {opsToSinks.Count}");

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

        Debug.Log($"[BindingManager.UnbindNonMatchingPolytrons] polytrons on mutatron counted: {polytronsOnMutList.Count}");

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

                // polytrons already on mut with this ops
                opsToPolytronsOnMut.TryGetValue(ops, out var polysWithOps);
                polysWithOps = polysWithOps ?? new List<Polytron>();

                // 2a: keep polytrons that are already on the correct sink (do not move)
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

                // pool of movable polytrons (on mutatron with this ops but not already assigned)
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

                // update remaining sinks after assigning existing polys
                remainingSinks = remainingSinks.Skip(moveCount).ToList();

                // 2d: for sinks still unfilled, call polytrons from home (respecting cooldowns)
                if (remainingSinks.Count > 0)
                {
                    // Only call polytrons that are actually at their home (ring 12) and have finished their cooldown.
                    // Exclude currently unbound polytrons: unbound polytrons should first be sent home and rest for one evolve.
                    var availableHomePolys = engine.polytrons.Where(p => p != null && !p.reservedForGenetics
                        && engine.polytronHomeCooldown.TryGetValue(p.sealNumber, out var cd) && cd <= 0
                        && (p.boundSink != null && engine.gridCellsMap.ContainsKey(p.boundSink.hexCoord) && engine.gridCellsMap[p.boundSink.hexCoord].ring == 12))
                        .ToList();
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

                    // any remaining sinks after calling homes will remain empty
                }

                // 2c: surplus polytrons (on mutatron with this ops but no matching sinks) must go home
                int sinksCount = sinks.Count;
                    if (polysWithOps.Count > sinksCount)
                {
                    var surplus = polysWithOps.Where(p => !assignedPolys.Contains(p)).ToList();
                    foreach (var sPoly in surplus)
                    {
                            Debug.Log($"[BindingManager] sending surplus polytron_id={sPoly.sealNumber} home from sink={sPoly.boundSink?.name ?? "null"}");
                            UnbindPolytron(sPoly);
                            BindPolytronToSink(sPoly, engine.polytronsHomes[sPoly.sealNumber]);
                            engine.polytronHomeCooldown[sPoly.sealNumber] = 3;
                            Debug.Log($"[BindingManager] polytron_id={sPoly.sealNumber} sent home and cooldown set");
                    }
                }
            }

            // Finally, any polytrons on mutatron whose operators are not present in opsToSinks should go home
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
                        engine.polytronHomeCooldown[p.sealNumber] = 3;
                        Debug.Log($"[BindingManager] polytron_id={p.sealNumber} sent home due to dead operators");
                    }
                }
                catch (Exception) { }
            }

            int totalPolytrons = engine.polytrons.Count;
            int boundOnMut = engine.polytrons.Count(p => p != null && p.boundSink != null && engine.gridCellsMap.ContainsKey(p.boundSink.hexCoord) && engine.gridCellsMap[p.boundSink.hexCoord].ring <= engine.actualLevelConfig.actualRingsCount);
            int boundOnHome = engine.polytrons.Count(p => p != null && p.boundSink != null && engine.gridCellsMap.ContainsKey(p.boundSink.hexCoord) && engine.gridCellsMap[p.boundSink.hexCoord].ring == 12);
            int unbound = engine.polytrons.Count(p => p != null && p.boundSink == null);
            Debug.Log($"[BindingManager.UnbindNonMatchingPolytrons] reconciliation complete: opsGroups={opsToSinks.Count}, onMut={polytronsOnMutList.Count}, boundOnMut={boundOnMut}, boundHome={boundOnHome}, unbound={unbound}, totalPolytrons={totalPolytrons}");
        }

        */



    public void SendAllPolytronsHome()
    {
        Debug.Log($"[BindingManager.SendAllPolytronsHome] sending all polytrons home (count={engine.polytrons.Count})");
        for (int i = 0; i < engine.polytrons.Count; i++)
        {
            var p = engine.polytrons[i];
            if (p == null) continue;
            UnbindPolytron(p);
            BindPolytronToSink(p, engine.polytronsHomes[p.sealNumber]);
            // reset cooldown so they can be called immediately after level build
            engine.polytronHomeCooldown[p.sealNumber] = 0;
        }
    }

    public void BindPolytronToSink(Polytron p, KeyValuePair<HexCoord, MutatronEngine.HexCellData> hckv)
    {
        BindPolytronToSink(p, hckv.Value.sink);
    }

    public void BindPolytronToSink(Polytron p, PolytronSink ps)
    {
        Debug.Assert(p != null);
        Debug.Assert(ps != null);

        // we request that, to bind a polytron to a sink, the polytron and the sink are actually unbound
        Debug.Assert(p.boundSink == null);
        Debug.Assert(ps.boundPolytron == null);

        Debug.Log($"[UnbindPolytron] BINDING polytron_id={p.sealNumber} to sink={ps.name}");

        p.boundSink = ps;
        ps.boundPolytron = p;

        engine.NotifyPolytronStateChanged(p);
    }

    public void UnbindPolytron(Polytron p)
    {

        Debug.Assert(p != null);

        var sink = p.boundSink;
        if (sink != null)
        {
            Debug.Assert(sink.boundPolytron == p, "If a polytron is bound to a sink, it is supposed that the sink backlinks the polytron");

            Debug.Log($"[UnbindPolytron] UNBINDING polytron_id={p.sealNumber} from sink={sink.name}");

            sink.boundPolytron = null;
            p.boundSink = null;
        }

        // Notify engine so UI and other listeners can update
        engine.NotifyPolytronStateChanged(p);
    }

    /// <summary>
    /// Capture polytron positions BEFORE evolution so we can detect which ones moved after.
    /// Called at the START of Evolve(), before CA algorithm runs.
    /// </summary>
    internal void CapturePreEvolutionState()
    {
        prevolatronPositions.Clear();
        
        foreach (var p in engine.polytrons)
        {
            if (p == null || p.boundSink == null || !engine.gridCellsMap.ContainsKey(p.boundSink.hexCoord))
                continue;
            
            var hcd = engine.gridCellsMap[p.boundSink.hexCoord];
            if (hcd.tile == null) continue;
            
            try
            {
                var parsed = PolyhedronRecipeParser.Parse(hcd.tile.recipe);
                string ops = parsed.OperatorsSequence();
                prevolatronPositions[p.sealNumber] = (p.boundSink.hexCoord, ops);
            }
            catch (Exception) { }
        }
        
        Debug.Log($"[PolytronsDance] captured pre-evolution state for {prevolatronPositions.Count} polytrons");
    }


    internal void PolytronsDance()
    {
        Debug.Log("[PolytronsDance] starting dance orchestration");

        // Step 1: Build a map of current transformations on the Mutatron (tiles)
        //         and collect polytrons currently bound to the Mutatron
        var mutatronTilesByOps = new Dictionary<string, List<KeyValuePair<HexCoord, MutatronEngine.HexCellData>>>();
        var polytronsByOps = new Dictionary<string, List<Polytron>>();
        var polytronsOnMut = new List<Polytron>();

        // Scan all tiles on the mutatron
        foreach (var hckv in engine.gridCellsMap)
        {
            if (hckv.Value.ring > engine.actualLevelConfig.actualRingsCount) continue;
            if (engine.IsMutatronCenter(hckv.Value)) continue; // Architron handles center separately
            if (hckv.Value.tile == null) continue;

            try
            {
                var parsedTile = PolyhedronRecipeParser.Parse(hckv.Value.tile.recipe);
                string ops = parsedTile.OperatorsSequence();

                if (!mutatronTilesByOps.ContainsKey(ops))
                    mutatronTilesByOps[ops] = new List<KeyValuePair<HexCoord, MutatronEngine.HexCellData>>();
                mutatronTilesByOps[ops].Add(hckv);
            }
            catch (Exception) { }
        }

        // Scan polytrons currently bound to mutatron
        foreach (var p in engine.polytrons)
        {
            if (p == null || p.isArchitron || p.reservedForGenetics) continue;
            if (p.boundSink == null || !engine.gridCellsMap.ContainsKey(p.boundSink.hexCoord)) continue;

            var hcd = engine.gridCellsMap[p.boundSink.hexCoord];
            if (!engine.IsMutatronCell(hcd)) continue; // only mutatron cells (not home)

            polytronsOnMut.Add(p);
            try
            {
                var parsedPoly = PolyhedronRecipeParser.Parse(p.recipe);
                string ops = parsedPoly.OperatorsSequence();

                if (!polytronsByOps.ContainsKey(ops))
                    polytronsByOps[ops] = new List<Polytron>();
                polytronsByOps[ops].Add(p);
            }
            catch (Exception) { }
        }

        Debug.Log($"[PolytronsDance] tile transformations: {mutatronTilesByOps.Count}, polytrons on mut: {polytronsOnMut.Count}");
        
        // Log polytrons by ops for debugging
        foreach (var kvp in polytronsByOps)
        {
            var polyIds = string.Join(",", kvp.Value.Select(p => p.sealNumber.ToString()));
            Debug.Log($"[PolytronsDance] polytrons by ops: {kvp.Key} -> polytrons=[{polyIds}]");
        }
        
        // Log tiles by ops for debugging
        foreach (var kvp in mutatronTilesByOps)
        {
            var coords = string.Join(",", kvp.Value.Select(h => $"({h.Key.q},{h.Key.r})"));
            Debug.Log($"[PolytronsDance] tiles by ops: {kvp.Key} -> coords=[{coords}]");
        }

        // Step 2: Identify BORN, DIED, STAY, and MOVE transformations
        var bornOps = new HashSet<string>(mutatronTilesByOps.Keys);
        var diedOps = new HashSet<string>(polytronsByOps.Keys);
        diedOps.ExceptWith(mutatronTilesByOps.Keys); // died = ops in polytrons but not in tiles

        var stayOps = new HashSet<string>(polytronsByOps.Keys);
        stayOps.IntersectWith(mutatronTilesByOps.Keys); // stay = ops in both

        bornOps.ExceptWith(polytronsByOps.Keys); // born = ops in tiles but not in polytrons

        Debug.Log($"[PolytronsDance] born: {bornOps.Count}, died: {diedOps.Count}, stay: {stayOps.Count}");

        // Step 3: Handle DIED transformations
        //         Polytrons with died ops go home and get cooldown of 2
        foreach (var deadOps in diedOps)
        {
            if (polytronsByOps.TryGetValue(deadOps, out var dyingPolytrons))
            {
                foreach (var p in dyingPolytrons)
                {
                    Debug.Log($"[PolytronsDance] DIED: sending polytron_id={p.sealNumber} home (ops={deadOps})");
                    UnbindPolytron(p);
                    BindPolytronToSink(p, engine.polytronsHomes[p.sealNumber]);
                    engine.polytronHomeCooldown[p.sealNumber] = 2;
                }
            }
        }

        // Step 4: Handle MOVE transformations
        //         Polytrons stay bound to the same transformation but the tiles may have moved
        //         Use pre-evolution positions to detect which polytrons need to move
        foreach (var stayingOps in stayOps)
        {
            if (polytronsByOps.TryGetValue(stayingOps, out var movedPolytrons) &&
                mutatronTilesByOps.TryGetValue(stayingOps, out var tileSinks))
            {
                Debug.Log($"[PolytronsDance] MOVE ops={stayingOps}: {movedPolytrons.Count} polytrons, {tileSinks.Count} tiles");

                // Determine which polytrons need to move by comparing pre-evolution positions
                var needsMove = new List<Polytron>();
                var alreadyPlaced = new HashSet<PolytronSink>();

                foreach (var p in movedPolytrons)
                {
                    // Check if this polytron was at this ops before evolution
                    if (prevolatronPositions.TryGetValue(p.sealNumber, out var preState))
                    {
                        var preCoord = preState.coord;
                        var preOps = preState.ops;
                        
                        // If ops haven't changed, check if it's on the same tile
                        if (preOps == stayingOps)
                        {
                            // Was on a tile with these ops. Find if that tile still has the polytron
                            bool foundOnSameTile = false;
                            foreach (var tile in tileSinks)
                            {
                                if (tile.Key == preCoord && tile.Value.sink == p.boundSink)
                                {
                                    // Still on the same tile, no move needed
                                    alreadyPlaced.Add(p.boundSink);
                                    Debug.Log($"[PolytronsDance] MOVE polytron_id={p.sealNumber} stayed at same tile {p.boundSink.name}");
                                    foundOnSameTile = true;
                                    break;
                                }
                            }
                            
                            if (foundOnSameTile)
                                continue;
                        }
                    }
                    
                    // Polytron needs to move
                    needsMove.Add(p);
                }

                // Try to place polytrons that need to move
                foreach (var p in needsMove)
                {
                    // Find an available tile that hasn't been used and is unbound
                    PolytronSink availableSink = null;
                    KeyValuePair<HexCoord, MutatronEngine.HexCellData>? availableTile = null;
                    
                    foreach (var tile in tileSinks)
                    {
                        if (!alreadyPlaced.Contains(tile.Value.sink) && tile.Value.sink.boundPolytron == null)
                        {
                            availableTile = tile;
                            availableSink = tile.Value.sink;
                            break;
                        }
                    }
                    
                    if (availableSink != null && availableTile.HasValue)
                    {
                        alreadyPlaced.Add(availableSink);
                        Debug.Log($"[PolytronsDance] MOVE: polytron_id={p.sealNumber} from {p.boundSink?.name ?? "null"} to {availableSink.name}");
                        UnbindPolytron(p);
                        BindPolytronToSink(p, availableSink);
                    }
                    else
                    {
                        Debug.LogWarning($"[PolytronsDance] MOVE: polytron_id={p.sealNumber} could not find available tile for ops={stayingOps}");
                    }
                }

                // Report unfilled tiles
                if (alreadyPlaced.Count < tileSinks.Count)
                {
                    Debug.Log($"[PolytronsDance] MOVE ops={stayingOps}: {tileSinks.Count - alreadyPlaced.Count} tiles remain empty");
                }
            }
        }

        // Step 5: Handle BORN transformations
        //         Recall polytrons from home and bind them to born tiles
        foreach (var ops in bornOps)
        {
            if (mutatronTilesByOps.TryGetValue(ops, out var bornTiles))
            {
                // Find eligible home-bound polytrons (not reserved, cooldown expired, at home)
                var eligiblePolytrons = engine.polytrons
                    .Where(p => p != null && !p.isArchitron && !p.reservedForGenetics &&
                                p.boundSink != null && engine.gridCellsMap.ContainsKey(p.boundSink.hexCoord) &&
                                engine.gridCellsMap[p.boundSink.hexCoord].ring == 12 && // at home
                                engine.polytronHomeCooldown.TryGetValue(p.sealNumber, out var cd) && cd <= 0)
                    .ToList();

                // Filter born tiles to only those with unbound sinks
                var availableBornTiles = new List<KeyValuePair<HexCoord, MutatronEngine.HexCellData>>();
                foreach (var bornTileHckv in bornTiles)
                {
                    if (bornTileHckv.Value.sink != null && bornTileHckv.Value.sink.boundPolytron == null)
                    {
                        availableBornTiles.Add(bornTileHckv);
                    }
                }

                int recallCount = Math.Min(eligiblePolytrons.Count, availableBornTiles.Count);
                for (int i = 0; i < recallCount; i++)
                {
                    var p = eligiblePolytrons[i];
                    var bornTileHckv = availableBornTiles[i];
                    var bornTile = bornTileHckv.Value.tile;

                    try
                    {
                        // Compose new recipe: tile's ops + polytron's palette+base
                        var parsedTile = PolyhedronRecipeParser.Parse(bornTile.recipe);
                        var parsedPoly = PolyhedronRecipeParser.Parse(p.recipe);

                        var newRecipeObj = new PolyhedronRecipe
                        {
                            Tokens = parsedTile.Tokens, // new ops from tile
                            PaletteIdx = parsedPoly.PaletteIdx, // keep polytron's palette
                            BasePolyhedron = parsedPoly.BasePolyhedron // keep polytron's base
                        };

                        string newRecipe = newRecipeObj.ToString();

                        Debug.Log($"[PolytronsDance] BORN: calling polytron_id={p.sealNumber} with new ops={ops} to sink={bornTileHckv.Value.sink.name}");

                        // Rebuild while polytron is still at home
                        engine.RebuildPolytronFromRecipe(p, newRecipe);

                        // Bind to the born tile
                        UnbindPolytron(p);
                        BindPolytronToSink(p, bornTileHckv.Value.sink);

                        // No cooldown set for BORN: polytron is immediately available if called again
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[PolytronsDance] BORN failed for polytron_id={p.sealNumber}: {ex.Message}");
                    }
                }

                if (recallCount < availableBornTiles.Count)
                {
                    Debug.Log($"[PolytronsDance] BORN transformations={ops}: {availableBornTiles.Count - recallCount} tiles remain unfilled (no eligible polytrons)");
                }
            }
        }

        // Step 6: Handle center tile (Architron special case)
        //         The Architron is always bound to the center and always updates its transformation
        try
        {
            var centerHckv = engine.gridCellsMap.FirstOrDefault(h => engine.IsMutatronCenter(h.Value));
            if (centerHckv.Key != null && centerHckv.Value.tile != null)
            {
                var arch = engine.polytrons[engine.architronIdx];
                if (arch != null)
                {
                    var parsedTile = PolyhedronRecipeParser.Parse(centerHckv.Value.tile.recipe);
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
                        Debug.Log($"[PolytronsDance] Architron at center updates transformation");
                        engine.RebuildPolytronFromRecipe(arch, newRecipe);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PolytronsDance] Architron update failed: {ex.Message}");
        }

        Debug.Log("[PolytronsDance] dance complete");
    }
}
