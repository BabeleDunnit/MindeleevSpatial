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

    /// <summary>
    /// Minimum cooldown duration (in Evolve turns) for polytrons sent home via DIED operations.
    /// Polytrons must rest at home for at least this many turns before becoming available for recall.
    /// Adjust this value to experiment with different polytron availability rhythms.
    /// </summary>
    public const int POLYTRON_HOME_COOLDOWN_TURNS = 1;

    /// <summary>
    /// Snapshot of a tile's state (ops string and coordinate).
    /// Used to detect BORN/DIED/STAY/MOVE transformations between CA evolution cycles.
    /// </summary>
    public struct TileState
    {
        public string ops;          // Operators sequence (e.g., "a(1)d(1)")
        public HexCoord coord;      // Grid coordinate (q, r)
        public int ring;            // Ring number for intuitive coordinate display
        public int idxInRing;       // Index within ring for intuitive coordinate display
    }

    // Tile state snapshots for delta computation
    private List<TileState> beforeCAEvolution = new();
    private List<TileState> afterCAEvolution = new();

    /// <summary>
    /// Delta operation records: BORN/DIED/STAY/MOVE computed from tile state changes.
    /// Cleared at start of each Evolve cycle and populated by ComputeTileStateDeltas().
    /// Used by PolytronsDance() to execute binding changes.
    /// </summary>
    private struct DeltaOperation
    {
        public enum OpType { BORN, DIED, STAY, MOVE }
        public OpType type;
        public string ops;              // Transformation ops for this operation
        public TileState? sourceTile;   // For DIED/MOVE: the before-tile
        public TileState? targetTile;   // For BORN/MOVE: the after-tile
        public Polytron sourcePolytron; // For DIED/MOVE: the polytron being affected
    }

    private List<DeltaOperation> deltaOperations = new();

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
    /// Recalls polytrons from their homes to the Mutatron in the initial configuration.
    /// This is called once when the level is ready (after tiles are created).
    /// 
    /// This mimics a BORN operation for each tile on the Mutatron:
    /// For each tile (excluding the center):
    /// 1. Parse the tile's ops (transformation)
    /// 2. Find ANY eligible polytron at home with cooldown = 0
    /// 3. Bind the polytron to the tile
    /// 4. Retrain the polytron's recipe: tile ops + polytron's radix (palette + base polyhedron)
    /// 5. Add the new recipe to the polytron's MindeleevTable
    /// 6. Rebuild the polytron mesh
    /// 7. Reset cooldown to 0 (now on active duty)
    /// 
    /// The Architron and center tile are handled separately and excluded from this process.
    /// </summary>
    internal void RecallPolytronsToInitialConfiguration()
    {
        Debug.Log("[RecallPolytronsToInitialConfiguration] Starting initial polytron recall for level start");
        
        int recalledCount = 0;
        int missedCount = 0;
        
        // Iterate over all tiles on the Mutatron (excluding ring 12 which is homes, and excluding center)
        foreach (var hckv in engine.gridCellsMap)
        {
            var hcd = hckv.Value;
            
            // Skip homes (ring 12) and tiles beyond the mutatron
            if (hcd.ring > engine.actualLevelConfig.actualRingsCount) continue;
            
            // Skip the center tile (Architron is already bound there)
            if (engine.IsMutatronCenter(hcd)) continue;
            
            // Skip if no tile exists
            if (hcd.tile == null) continue;
            
            // Parse the tile's transformation (ops only - we'll combine with polytron's radix)
            var tileRecipeParsed = PolyhedronRecipeParser.Parse(hcd.tile.recipe);
            string tileOps = tileRecipeParsed.OperatorsSequence();
            
            // Find ANY eligible polytron at home with cooldown = 0
            // Eligible = not reserved, not Architron, at home (ring 12), cooldown = 0
            Polytron eligiblePolytron = null;
            for (int i = 0; i < engine.polytrons.Count; i++)
            {
                var p = engine.polytrons[i];
                if (p == null || p.reservedForGenetics || p.isArchitron) continue;
                
                // Check if polytron is at home
                if (p.boundSink == null) continue;
                var pSinkCoord = p.boundSink.hexCoord;
                if (!engine.gridCellsMap.ContainsKey(pSinkCoord)) continue;
                var pCell = engine.gridCellsMap[pSinkCoord];
                if (pCell.ring != 12) continue;
                
                // Check cooldown
                if (!engine.polytronHomeCooldown.ContainsKey(p.sealNumber)) continue;
                if (engine.polytronHomeCooldown[p.sealNumber] != 0) continue;
                
                // Found an eligible polytron (any one will do - we'll retrain it)
                eligiblePolytron = p;
                break;
            }
            
            if (eligiblePolytron != null)
            {
                // Retrain the polytron's recipe while it is still at home (avoid Rebuild guard)
                try
                {
                    var polytronRecipeParsed = PolyhedronRecipeParser.Parse(eligiblePolytron.recipe);
                    int polytronPaletteIdx = polytronRecipeParsed.PaletteIdx;
                    char polytronBasePolyhedron = polytronRecipeParsed.BasePolyhedron;

                    var retrainedRecipe = new PolyhedronRecipe
                    {
                        Tokens = tileRecipeParsed.Tokens,
                        PaletteIdx = polytronPaletteIdx,
                        BasePolyhedron = polytronBasePolyhedron
                    };
                    string retrainedRecipeStr = retrainedRecipe.ToString();

                    // Rebuild while still at home so the defensive guard in engine allows the change
                    engine.RebuildPolytronFromRecipe(eligiblePolytron, retrainedRecipeStr);

                    // Now move the polytron from home to the Mutatron sink
                    UnbindPolytron(eligiblePolytron);
                    BindPolytronToSink(eligiblePolytron, hcd.sink);

                    // Ensure the MindeleevTable recorded the emanation (Rebuild already calls AddEmanation)
                    engine.polytronHomeCooldown[eligiblePolytron.sealNumber] = 0;

                    Debug.Log($"[RecallPolytronsToInitialConfiguration] BORN: Recalled polytron_id={eligiblePolytron.sealNumber} to tile at (ring={hcd.ring}, idx={hcd.idxInRing}) with ops=<{tileOps}> and new recipe={retrainedRecipeStr}");
                    recalledCount++;
                }
                catch (Exception) { }
            }
            else
            {
                Debug.Log($"[RecallPolytronsToInitialConfiguration] No eligible polytron found for BORN at tile (ring={hcd.ring}, idx={hcd.idxInRing}) with ops=<{tileOps}>");
                missedCount++;
            }
        }
        
        Debug.Log($"[RecallPolytronsToInitialConfiguration] Initial recall complete: {recalledCount} BORN, {missedCount} tiles without available polytron");
    }

    internal void PolytronsDance()
    {
        Debug.Log("[PolytronsDance] Executing polytron movements based on tile delta operations");
        
        // Group operations by type and ops so we can execute them in a coordinated way
        var opsByType = deltaOperations.GroupBy(d => d.type).ToDictionary(g => g.Key, g => g.ToList());
        
        int bornCount = 0, diedCount = 0, stayCount = 0, moveCount = 0;
        
        // Step 1: Handle DIED operations (send polytrons home with cooldown=2)
        if (opsByType.TryGetValue(DeltaOperation.OpType.DIED, out var diedOps))
        {
            foreach (var op in diedOps)
            {
                if (op.sourcePolytron != null && op.sourcePolytron.boundSink != null)
                {
                    Debug.Log($"[PolytronsDance] DIED: Sending polytron_id={op.sourcePolytron.sealNumber} home (ops=<{op.ops}> died)");
                    UnbindPolytron(op.sourcePolytron);
                    BindPolytronToSink(op.sourcePolytron, engine.polytronsHomes[op.sourcePolytron.sealNumber].Value.sink);
                    engine.polytronHomeCooldown[op.sourcePolytron.sealNumber] = POLYTRON_HOME_COOLDOWN_TURNS;
                    diedCount++;
                }
            }
        }
        
        // Step 2: Handle MOVE operations (rebind from source to target sink)
        if (opsByType.TryGetValue(DeltaOperation.OpType.MOVE, out var moveOps))
        {
            foreach (var op in moveOps)
            {
                if (op.sourcePolytron != null && op.sourceTile.HasValue && op.targetTile.HasValue)
                {
                    var targetHcd = engine.gridCellsMap[op.targetTile.Value.coord];
                    Debug.Log($"[PolytronsDance] MOVE: Polytron_id={op.sourcePolytron.sealNumber} from (ring={op.sourceTile.Value.ring}, idx={op.sourceTile.Value.idxInRing}) to (ring={op.targetTile.Value.ring}, idx={op.targetTile.Value.idxInRing}) (ops=<{op.ops}>)");
                    
                    // Ensure target sink is unbound (unbind any polytron currently there)
                    if (targetHcd.sink.boundPolytron != null && targetHcd.sink.boundPolytron != op.sourcePolytron)
                    {
                        UnbindPolytron(targetHcd.sink.boundPolytron);
                    }
                    
                    UnbindPolytron(op.sourcePolytron);
                    BindPolytronToSink(op.sourcePolytron, targetHcd.sink);
                    moveCount++;
                }
            }
        }
        
        // Step 3: Handle BORN operations (call eligible polytrons from home)
        if (opsByType.TryGetValue(DeltaOperation.OpType.BORN, out var bornOps))
        {
            foreach (var op in bornOps)
            {
                if (op.targetTile.HasValue)
                {
                    var targetHcd = engine.gridCellsMap[op.targetTile.Value.coord];
                    
                    // Find an eligible polytron at home (not reserved, not Architron, cooldown=0)
                    Polytron eligiblePolytron = null;
                    for (int i = 0; i < engine.polytrons.Count; i++)
                    {
                        var p = engine.polytrons[i];
                        if (p == null || p.reservedForGenetics || p.isArchitron) continue;
                        
                        // Check if at home
                        if (p.boundSink == null) continue;
                        var pSinkCoord = p.boundSink.hexCoord;
                        if (!engine.gridCellsMap.ContainsKey(pSinkCoord)) continue;
                        var pCell = engine.gridCellsMap[pSinkCoord];
                        if (pCell.ring != 12) continue;
                        
                        // Check cooldown
                        if (!engine.polytronHomeCooldown.ContainsKey(p.sealNumber)) continue;
                        if (engine.polytronHomeCooldown[p.sealNumber] != 0) continue;
                        
                        eligiblePolytron = p;
                        break;
                    }
                    
                    if (eligiblePolytron != null)
                    {
                        // Rebuild the polytron with the tile's recipe while still at home
                        try
                        {
                            var parsedTile = PolyhedronRecipeParser.Parse(targetHcd.tile.recipe);
                            var parsedPoly = PolyhedronRecipeParser.Parse(eligiblePolytron.recipe);
                            
                            var retrainedRecipe = new PolyhedronRecipe
                            {
                                Tokens = parsedTile.Tokens,
                                PaletteIdx = parsedPoly.PaletteIdx,
                                BasePolyhedron = parsedPoly.BasePolyhedron
                            };
                            string retrainedRecipeStr = retrainedRecipe.ToString();
                            
                            // Rebuild while still at home
                            engine.RebuildPolytronFromRecipe(eligiblePolytron, retrainedRecipeStr);
                            
                            // Ensure target sink is unbound (unbind any polytron currently there)
                            if (targetHcd.sink.boundPolytron != null && targetHcd.sink.boundPolytron != eligiblePolytron)
                            {
                                UnbindPolytron(targetHcd.sink.boundPolytron);
                            }
                            
                            // Now move to mutatron
                            UnbindPolytron(eligiblePolytron);
                            BindPolytronToSink(eligiblePolytron, targetHcd.sink);
                            engine.polytronHomeCooldown[eligiblePolytron.sealNumber] = 0;
                            
                            Debug.Log($"[PolytronsDance] BORN: Called polytron_id={eligiblePolytron.sealNumber} to (ring={op.targetTile.Value.ring}, idx={op.targetTile.Value.idxInRing}) with ops=<{op.ops}>");
                            bornCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[PolytronsDance] BORN failed: {ex}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[PolytronsDance] BORN: No eligible polytron available for (ring={op.targetTile.Value.ring}, idx={op.targetTile.Value.idxInRing}) (ops=<{op.ops}>)");
                    }
                }
            }
        }
        
        // Step 4: STAY operations require no action (polytrons already correctly bound)
        stayCount = opsByType.TryGetValue(DeltaOperation.OpType.STAY, out var stayOps) ? stayOps.Count : 0;
        
        Debug.Log($"[PolytronsDance] Complete: {bornCount} BORN, {diedCount} DIED, {stayCount} STAY, {moveCount} MOVE");
    }

    /// <summary>
    /// Captures the tile states BEFORE the CA evolution algorithm runs.
    /// Call this at the START of Evolve(), before the polytronic number CA algorithm.
    /// </summary>
    internal void CaptureBeforeCAEvolution()
    {
        beforeCAEvolution.Clear();
        
        foreach (var hckv in engine.gridCellsMap)
        {
            // Only capture tiles on the mutatron (not homes ring 12)
            if (hckv.Value.ring > engine.actualLevelConfig.actualRingsCount) continue;
            // Skip the mutatron center (ring 0) — it's always bound to the Architron and excluded from dance
            if (hckv.Value.ring == 0) continue;
            if (hckv.Value.tile == null) continue;

                var parsed = PolyhedronRecipeParser.Parse(hckv.Value.tile.recipe);
                string ops = parsed.OperatorsSequence();
                
                beforeCAEvolution.Add(new TileState
                {
                    ops = ops,
                    coord = hckv.Key,
                    ring = hckv.Value.ring,
                    idxInRing = hckv.Value.idxInRing
                });
        }
        
        Debug.Log($"[CaptureBeforeCAEvolution] captured {beforeCAEvolution.Count} tiles before CA evolution");
    }

    /// <summary>
    /// Captures the tile states AFTER the CA evolution and tile update.
    /// Call this at the beginning of PolytronsDance(), after gridManager.UpdateTiles() has run.
    /// </summary>
    internal void CaptureAfterCAEvolution()
    {
        afterCAEvolution.Clear();
        
        foreach (var hckv in engine.gridCellsMap)
        {
            // Only capture tiles on the mutatron (not homes ring 12)
            if (hckv.Value.ring > engine.actualLevelConfig.actualRingsCount) continue;
            // Skip the mutatron center (ring 0) — it's always bound to the Architron and excluded from dance
            if (hckv.Value.ring == 0) continue;
            if (hckv.Value.tile == null) continue;

                var parsed = PolyhedronRecipeParser.Parse(hckv.Value.tile.recipe);
                string ops = parsed.OperatorsSequence();
                
                afterCAEvolution.Add(new TileState
                {
                    ops = ops,
                    coord = hckv.Key,
                    ring = hckv.Value.ring,
                    idxInRing = hckv.Value.idxInRing
                });
        }
        
        Debug.Log($"[CaptureAfterCAEvolution] captured {afterCAEvolution.Count} tiles after CA evolution");
    }

    /// <summary>
    /// Analyzes beforeCAEvolution and afterCAEvolution to identify transformation operations.
    /// Computes BORN (new transformations), DIED (removed transformations), STAY (same transformation at same coord),
    /// and MOVE (same transformation at different coords) using bipartite matching.
    /// 
    /// Key insights:
    /// - Transformations (not tiles) move. A transformation is uniquely identified by its ops string and position.
    /// - Each position can supply ONE transformation (before) and receive ONE transformation (after).
    /// - BORN: transformation appears at a position (no source position)
    /// - DIED: transformation disappears from a position (no target position)
    /// - STAY: transformation remains at the same position
    /// - MOVE: transformation changes position (source→target). After a MOVE, the source position becomes available.
    /// 
    /// All operations happen in parallel: positions and transformations are atomic, simultaneous updates.
    /// </summary>
    internal void ComputeTileStateDeltas()
    {
        // Clear previous delta operations
        deltaOperations.Clear();
        
        // Group transformation snapshots by ops string
        var beforeByOps = new Dictionary<string, List<TileState>>();
        var afterByOps = new Dictionary<string, List<TileState>>();
        
        foreach (var tile in beforeCAEvolution)
        {
            if (!beforeByOps.ContainsKey(tile.ops))
                beforeByOps[tile.ops] = new List<TileState>();
            beforeByOps[tile.ops].Add(tile);
        }
        
        foreach (var tile in afterCAEvolution)
        {
            if (!afterByOps.ContainsKey(tile.ops))
                afterByOps[tile.ops] = new List<TileState>();
            afterByOps[tile.ops].Add(tile);
        }
        
        // Track transformation operation counts
        int totalBorn = 0;
        int totalDied = 0;
        int totalStay = 0;
        int totalMove = 0;
        
        // BORN: transformations that exist in after but not in before (complete ops disappearance)
        foreach (var opsKey in afterByOps.Keys)
        {
            if (!beforeByOps.ContainsKey(opsKey))
            {
                int count = afterByOps[opsKey].Count;
                totalBorn += count;
                var coords = afterByOps[opsKey].Select(t => $"(ring={t.ring}, idx={t.idxInRing})");
                Debug.Log($"[ComputeTileStateDeltas] BORN: {count}x ops=<{opsKey}> at {string.Join(", ", coords)}");
                
                // Record BORN operations in delta list
                foreach (var afterTile in afterByOps[opsKey])
                {
                    deltaOperations.Add(new DeltaOperation
                    {
                        type = DeltaOperation.OpType.BORN,
                        ops = opsKey,
                        targetTile = afterTile,
                        sourcePolytron = null
                    });
                }
            }
        }
        
        // DIED: transformations that exist in before but not in after (complete ops disappearance)
        foreach (var opsKey in beforeByOps.Keys)
        {
            if (!afterByOps.ContainsKey(opsKey))
            {
                int count = beforeByOps[opsKey].Count;
                totalDied += count;
                var coords = beforeByOps[opsKey].Select(t => $"(ring={t.ring}, idx={t.idxInRing})");
                Debug.Log($"[ComputeTileStateDeltas] DIED: {count}x ops=<{opsKey}> (was at {string.Join(", ", coords)})");
                
                // Record DIED operations in delta list
                foreach (var beforeTile in beforeByOps[opsKey])
                {
                    // Find the polytron currently bound to this tile
                    var hcd = engine.gridCellsMap[beforeTile.coord];
                    var polytron = hcd.sink.boundPolytron;
                    
                    deltaOperations.Add(new DeltaOperation
                    {
                        type = DeltaOperation.OpType.DIED,
                        ops = opsKey,
                        sourceTile = beforeTile,
                        sourcePolytron = polytron
                    });
                }
            }
        }
        
        // STAY vs MOVE: same ops exist before and after - use bipartite matching to pair positions
        foreach (var opsKey in beforeByOps.Keys)
        {
            if (!afterByOps.ContainsKey(opsKey)) continue; // Already handled as DIED
            
            var beforeTiles = beforeByOps[opsKey];
            var afterTiles = afterByOps[opsKey];
            
            // First pass: identify STAY (same position before and after)
            var usedBefore = new HashSet<TileState>();
            var usedAfter = new HashSet<TileState>();
            var stayTiles = new List<(TileState before, TileState after)>();
            
            foreach (var bt in beforeTiles)
            {
                var at = afterTiles.FirstOrDefault(t => t.coord.q == bt.coord.q && t.coord.r == bt.coord.r);
                if (at.coord.q != 0 || at.coord.r != 0 || (bt.coord.q == 0 && bt.coord.r == 0)) // Valid match (HexCoord default is 0,0)
                {
                    if (at.ops == bt.ops && !usedAfter.Contains(at))
                    {
                        stayTiles.Add((bt, at));
                        usedBefore.Add(bt);
                        usedAfter.Add(at);
                    }
                }
            }
            
            if (stayTiles.Count > 0)
            {
                totalStay += stayTiles.Count;
                var coords = stayTiles.Select(p => $"(ring={p.before.ring}, idx={p.before.idxInRing})");
                Debug.Log($"[ComputeTileStateDeltas] STAY: {stayTiles.Count}x ops=<{opsKey}> at {string.Join(", ", coords)}");
                
                // Record STAY operations (no action needed, but track them)
                foreach (var (beforeTile, afterTile) in stayTiles)
                {
                    deltaOperations.Add(new DeltaOperation
                    {
                        type = DeltaOperation.OpType.STAY,
                        ops = opsKey,
                        sourceTile = beforeTile,
                        targetTile = afterTile
                    });
                }
            }
            
            // Second pass: match remaining before→after for MOVE using greedy nearest-neighbor
            var unmatchedBefore = beforeTiles.Where(b => !usedBefore.Contains(b)).ToList();
            var unmatchedAfter = afterTiles.Where(a => !usedAfter.Contains(a)).ToList();
            var moveTiles = new List<(TileState before, TileState after)>();
            
            // Greedy matching: repeatedly find the closest pair (source position, target position)
            while (unmatchedBefore.Count > 0 && unmatchedAfter.Count > 0)
            {
                TileState? bestBefore = null;
                TileState? bestAfter = null;
                float bestDistance = float.MaxValue;
                
                foreach (var bt in unmatchedBefore)
                {
                    foreach (var at in unmatchedAfter)
                    {
                        // Manhattan distance in grid coordinates (heuristic for matching)
                        float dist = Mathf.Abs(bt.ring - at.ring) + Mathf.Abs(bt.idxInRing - at.idxInRing);
                        if (dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestBefore = bt;
                            bestAfter = at;
                        }
                    }
                }
                
                if (bestBefore.HasValue && bestAfter.HasValue)
                {
                    moveTiles.Add((bestBefore.Value, bestAfter.Value));
                    unmatchedBefore.Remove(bestBefore.Value);
                    unmatchedAfter.Remove(bestAfter.Value);
                }
                else
                {
                    break;
                }
            }
            
            if (moveTiles.Count > 0)
            {
                totalMove += moveTiles.Count;
                var moves = moveTiles.Select(p => $"(ring={p.before.ring}, idx={p.before.idxInRing}) -> (ring={p.after.ring}, idx={p.after.idxInRing})");
                Debug.Log($"[ComputeTileStateDeltas] MOVE: {moveTiles.Count}x ops=<{opsKey}> {string.Join("; ", moves)}");
                
                // Record MOVE operations
                foreach (var (beforeTile, afterTile) in moveTiles)
                {
                    var hcd = engine.gridCellsMap[beforeTile.coord];
                    var polytron = hcd.sink.boundPolytron;
                    
                    deltaOperations.Add(new DeltaOperation
                    {
                        type = DeltaOperation.OpType.MOVE,
                        ops = opsKey,
                        sourceTile = beforeTile,
                        targetTile = afterTile,
                        sourcePolytron = polytron
                    });
                }
            }
            
            // Remaining unmatched transformations: 
            // - Unmatched before with no after = DIED (transformation disappears)
            // - Unmatched after with no before = BORN (transformation appears)
            if (unmatchedBefore.Count > 0)
            {
                totalDied += unmatchedBefore.Count;
                var coords = unmatchedBefore.Select(t => $"(ring={t.ring}, idx={t.idxInRing})");
                Debug.Log($"[ComputeTileStateDeltas] DIED (within ops group): {unmatchedBefore.Count}x ops=<{opsKey}> (was at {string.Join(", ", coords)})");
                
                foreach (var beforeTile in unmatchedBefore)
                {
                    var hcd = engine.gridCellsMap[beforeTile.coord];
                    var polytron = hcd.sink.boundPolytron;
                    
                    deltaOperations.Add(new DeltaOperation
                    {
                        type = DeltaOperation.OpType.DIED,
                        ops = opsKey,
                        sourceTile = beforeTile,
                        sourcePolytron = polytron
                    });
                }
            }
            
            if (unmatchedAfter.Count > 0)
            {
                totalBorn += unmatchedAfter.Count;
                var coords = unmatchedAfter.Select(t => $"(ring={t.ring}, idx={t.idxInRing})");
                Debug.Log($"[ComputeTileStateDeltas] BORN (within ops group): {unmatchedAfter.Count}x ops=<{opsKey}> at {string.Join(", ", coords)}");
                
                foreach (var afterTile in unmatchedAfter)
                {
                    deltaOperations.Add(new DeltaOperation
                    {
                        type = DeltaOperation.OpType.BORN,
                        ops = opsKey,
                        targetTile = afterTile,
                        sourcePolytron = null
                    });
                }
            }
        }
        
        // Summary: verify that transformation count is balanced (before+born = after+died)
        int beforeTotal = beforeCAEvolution.Count;
        int afterTotal = afterCAEvolution.Count;
        int accounted = totalBorn + totalDied + totalStay + totalMove;
        
        Debug.Log($"[ComputeTileStateDeltas] === Summary: {totalBorn} BORN, {totalDied} DIED, {totalStay} STAY, {totalMove} MOVE ===");
        Debug.Log($"[ComputeTileStateDeltas] Transformation count check: before={beforeTotal}, after={afterTotal}, accounted={accounted}, born-died={totalBorn - totalDied}");
        
        if (beforeTotal + totalBorn != afterTotal + totalDied)
        {
            Debug.LogWarning($"[ComputeTileStateDeltas] MISMATCH: before({beforeTotal}) + born({totalBorn}) != after({afterTotal}) + died({totalDied})");
        }
    }




}
