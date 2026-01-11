using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GeneticsManager
{
    private MutatronEngine engine;

    private struct PolytronStateBackup
    {
        public string recipe;
        public PolytronSink boundSink;
        public Vector3 position;
        public Vector3 localScale;
    }

    internal bool GeneticModeActive { get; private set; } = false;
    private List<Polytron> geneticFriends = new List<Polytron>();
    private Dictionary<Polytron, PolytronStateBackup> geneticBackups = new Dictionary<Polytron, PolytronStateBackup>();
    private Dictionary<Polytron, Vector3> geneticRelativeOffsets = new Dictionary<Polytron, Vector3>();
    private Dictionary<Polytron, Vector3> geneticReturnTargets = new Dictionary<Polytron, Vector3>();
    private HashSet<Polytron> geneticReturning = new HashSet<Polytron>();

    private float geneticFriendsRadius = 1.7f;
    private float geneticFriendsHeight = 1f;
    private float geneticAttractionStrength = 5f;

    public GeneticsManager(MutatronEngine engine)
    {
        this.engine = engine;
    }

    internal void SetupGeneticFriends(List<string> crossoverRecipes)
    {
        if (crossoverRecipes == null || crossoverRecipes.Count == 0) return;

        AbortPendingGeneticReturnImmediate();

        int needed = crossoverRecipes.Count;
        int totalPolytrons = engine.polytrons.Count;
        if (totalPolytrons == 0) return;

        int assigned = 0;

        var arch = engine.polytrons[engine.architronIdx];
        if (arch == null) return;

        geneticFriends.Clear();
        geneticBackups.Clear();
        geneticRelativeOffsets.Clear();
        geneticReturnTargets.Clear();
        geneticReturning.Clear();

        float R = geneticFriendsRadius;
        float h = geneticFriendsHeight;
        float horizRadius = 0f;
        if (R > Mathf.Abs(h)) horizRadius = Mathf.Sqrt(R * R - h * h);

        var neighborOffsets = new List<int>();
        for (int d = 1; neighborOffsets.Count < totalPolytrons - 1 && d < totalPolytrons; d++)
        {
            neighborOffsets.Add(d);
            if (neighborOffsets.Count >= totalPolytrons - 1) break;
            neighborOffsets.Add(-d);
        }

        var orderedCandidates = new List<Polytron>();
        foreach (var off in neighborOffsets)
        {
            int idx = (engine.architronIdx + off) % totalPolytrons;
            if (idx < 0) idx += totalPolytrons;
            var candidate = engine.polytrons[idx];
            if (candidate == null) continue;
            if (candidate == arch) continue;
            if (candidate == engine.selectionManager?.PaletteSelector || candidate == engine.selectionManager?.OperatorsSelector) continue;
            if (orderedCandidates.Contains(candidate)) continue;
            orderedCandidates.Add(candidate);
        }

        var onMutatron = orderedCandidates
            .Where(c => c.boundSink != null && engine.IsMutatronCell(c.boundSink.hexCoord))
            .ToList();
        var atHome = orderedCandidates
            .Where(c => c.boundSink == null || engine.IsHome(c.boundSink.hexCoord))
            .ToList();

        List<Polytron> pickPool = new List<Polytron>();
        pickPool.AddRange(onMutatron);
        pickPool.AddRange(atHome);

        Debug.Log($"[SetupGeneticFriends] needed={needed} orderedCandidates={orderedCandidates.Count} onMutatron={onMutatron.Count} atHome={atHome.Count} pickPool={pickPool.Count}");
        Debug.Log("[SetupGeneticFriends] ordered: " + string.Join(",", orderedCandidates.Select(c => c?.sealNumber.ToString() ?? "null")));
        Debug.Log("[SetupGeneticFriends] onMutatron: " + string.Join(",", onMutatron.Select(c => c?.sealNumber.ToString() ?? "null")));
        Debug.Log("[SetupGeneticFriends] atHome: " + string.Join(",", atHome.Select(c => c?.sealNumber.ToString() ?? "null")));

        for (int i = 0; i < pickPool.Count && assigned < needed; i++)
        {
            var candidate = pickPool[i];
            if (candidate == null) continue;
            if (geneticBackups.ContainsKey(candidate)) continue;

            var backup = new PolytronStateBackup
            {
                recipe = candidate.recipe,
                boundSink = candidate.boundSink,
                position = candidate.transform.position,
                localScale = candidate.transform.localScale
            };

            geneticBackups[candidate] = backup;
            geneticFriends.Add(candidate);

            Debug.Log($"[SetupGeneticFriends] PICKED friend #{assigned}: polytron_id={candidate.sealNumber} (seal:{candidate.sealName}) boundSink={backup.boundSink?.name} position={candidate.transform.position}");

            // Unbind the friend from its original sink so it can freely orbit the Architron
            engine.UnbindPolytron(candidate);
            candidate.reservedForGenetics = true;
            engine.NotifyPolytronStateChanged(candidate);

            Debug.Log($"[SetupGeneticFriends] UNBOUND and RESERVED friend #{assigned}: polytron_id={candidate.sealNumber} reservedForGenetics={candidate.reservedForGenetics}");

            candidate.recipe = crossoverRecipes[assigned];
            candidate.RebuildMesh();

            var wa = candidate.GetComponent<WaveAnimation>();
            if (wa != null) wa.Pause(true);
            candidate.interactive = false;

            candidate.transform.localScale = backup.localScale * 0.3f;

            float angle = ((float)assigned / Mathf.Max(1, needed)) * Mathf.PI * 2f;
            Vector3 rel = Vector3.up * h + new Vector3(Mathf.Cos(angle) * horizRadius, 0f, Mathf.Sin(angle) * horizRadius);
            geneticRelativeOffsets[candidate] = rel;

            engine.NotifyPolytronStateChanged(candidate);

            assigned++;
        }

        GeneticModeActive = geneticFriends.Count > 0;

        // If two parents are selected, override the Architron's operators
        // sequence temporarily to the next missing polytronic number for the
        // Architron (do not record this as an emanation).
        if (engine.selectionManager != null && engine.selectionManager.PaletteSelector != null && engine.selectionManager.OperatorsSelector != null)
        {
            try
            {
                int nextSeed = arch.MindeleevTable.NextMissingPolytronicNumber();
                string ops = PolyhedronRecipeKabbalah.IntToOperatorsSequence(nextSeed);
                string radix = engine.GetRadixRecipe(arch); // paletteIdx + basePoly
                string tempRecipe = ops + radix;
                Debug.Log($"[GeneticsManager] Applying temporary Architron recipe for next missing seed={nextSeed}: {tempRecipe}");
                engine.ApplyRecipeToArchitron(tempRecipe);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GeneticsManager] Failed to apply temporary Architron recipe: {ex}");
                engine.ApplyRecipeToArchitron(arch.recipe);
            }
        }
        else
        {
            // no selectors; keep arch default
            engine.ApplyRecipeToArchitron(arch.recipe);
        }

        engine.NotifyPolytronStateChanged(arch);

        Debug.Log($"[MutatronEngine] SetupGeneticFriends: assigned {geneticFriends.Count} friends for {needed} recipes");
    }

    internal void ClearGeneticFriends()
    {
        if (!GeneticModeActive && geneticFriends.Count == 0 && geneticBackups.Count == 0) return;

        Debug.Log("[MutatronEngine] ClearGeneticFriends - begin return phase");

        var friendsToKeepForPhysicsReturn = new List<Polytron>();
        foreach (var friend in geneticFriends)
        {
            if (friend == null) continue;
            if (!geneticBackups.TryGetValue(friend, out var backup)) continue;

            friend.recipe = backup.recipe;
            friend.RebuildMesh();

            var wa = friend.GetComponent<WaveAnimation>();
            if (wa != null) wa.Pause(false);
            friend.transform.localScale = backup.localScale;

            if (backup.boundSink != null && backup.boundSink.boundPolytron == null)
            {
                Debug.Log($"[ClearGeneticFriends] immediate rebind available for polytron_id={friend.sealNumber} to sink={backup.boundSink.name}");
                // Ensure friend is unbound before rebinding (should already be unbound, but be safe)
                if (friend.boundSink != null)
                {
                    engine.UnbindPolytron(friend);
                }
                engine.BindPolytronToSink(friend, backup.boundSink);
                friend.reservedForGenetics = false;
                friend.interactive = true;
                geneticBackups.Remove(friend);
                var outline = friend.GetComponent<PointerOutlineStateController>();
                outline?.SetState(0);
                engine.NotifyPolytronStateChanged(friend);
            }
            else
            {
                friend.interactive = false;
                geneticReturnTargets[friend] = backup.position;
                geneticReturning.Add(friend);
                geneticRelativeOffsets.Remove(friend);
                // engine.UnbindPolytron(friend);
                var outline = friend.GetComponent<PointerOutlineStateController>();
                outline?.SetState(0);
                friendsToKeepForPhysicsReturn.Add(friend);
            }
        }

        geneticFriends = friendsToKeepForPhysicsReturn;

        engine.NotifyPolytronStateChanged(engine.polytrons[engine.architronIdx]);

        GeneticModeActive = geneticReturning.Count > 0 || geneticFriends.Count > 0;
        // If genetic mode fully ended, restore Architron original recipe if available
        if (!GeneticModeActive)
        {
            try
            {
                var arch = engine.polytrons[engine.architronIdx];
                if (engine.selectionManager != null && engine.selectionManager.ArchitronSavedRecipeForSelection != null)
                {
                    engine.ApplyRecipeToArchitron(engine.selectionManager.ArchitronSavedRecipeForSelection);
                }
                else if (arch != null)
                {
                    engine.ApplyRecipeToArchitron(arch.recipe);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GeneticsManager] Failed to restore Architron recipe on genetic end: {ex}");
            }
        }
    }

    internal void AbortPendingGeneticReturnImmediate()
    {
        if (geneticReturning.Count == 0) return;

        Debug.Log("[MutatronEngine] AbortPendingGeneticReturnImmediate - force restore");

        foreach (var friend in geneticReturning.ToList())
        {
            if (friend == null) continue;
            if (!geneticBackups.TryGetValue(friend, out var backup)) continue;

            if (backup.boundSink != null)
            {
                // Ensure friend is unbound before rebinding
                if (friend.boundSink != null)
                {
                    engine.UnbindPolytron(friend);
                }
                engine.BindPolytronToSink(friend, backup.boundSink);
                friend.reservedForGenetics = false;
            }
            friend.RebuildMesh();
            friend.transform.localScale = backup.localScale;
            var wa = friend.GetComponent<WaveAnimation>();
            if (wa != null) wa.Pause(false);
            friend.interactive = true;
        }

        geneticRelativeOffsets.Clear();
        geneticReturnTargets.Clear();
        geneticReturning.Clear();
        GeneticModeActive = false;
    }

    internal void AttractGeneticFriends()
    {
        if (geneticFriends.Count > 0)
        {
            var arch = engine.polytrons[engine.architronIdx];
            if (arch != null)
            {
                float R = geneticFriendsRadius;
                float h = geneticFriendsHeight;
                float horizRadius = 0f;
                if (R > Mathf.Abs(h)) horizRadius = Mathf.Sqrt(R * R - h * h);
                Vector3 crownCenter = arch.transform.position + Vector3.up * h;

                int N = geneticFriends.Count;
                float targetSeparation = (N > 0 && horizRadius > 0f) ? (2f * Mathf.PI * horizRadius / N) : 1.0f;

                float kToCircle = geneticAttractionStrength * 0.75f;
                float kBetween = geneticAttractionStrength * 0.5f;
                float kHeight = geneticAttractionStrength * 0.5f;

                var friendsList = geneticFriends.Where(f => f != null && !geneticReturning.Contains(f)).ToList();
                for (int i = 0; i < friendsList.Count; i++)
                {
                    var friend = friendsList[i];
                    if (friend == null) continue;
                    var rb = friend.GetComponent<Rigidbody>();
                    if (rb == null) continue;

                    float angle = ((float)i / Mathf.Max(1, friendsList.Count)) * Mathf.PI * 2f;
                    Vector3 desiredOnCircle = crownCenter + new Vector3(Mathf.Cos(angle) * horizRadius, 0f, Mathf.Sin(angle) * horizRadius);
                    desiredOnCircle.y = crownCenter.y;

                    Vector3 toCircle = desiredOnCircle - friend.transform.position;
                    Vector3 fCircle = toCircle * kToCircle;

                    Vector3 heightDelta = new Vector3(0f, crownCenter.y - friend.transform.position.y, 0f);
                    Vector3 fHeight = heightDelta * kHeight;

                    Vector3 fBetweenTotal = Vector3.zero;
                    for (int j = 0; j < friendsList.Count; j++)
                    {
                        if (j == i) continue;
                        var other = friendsList[j];
                        if (other == null) continue;
                        Vector3 d = friend.transform.position - other.transform.position;
                        float dist = d.magnitude;
                        if (dist < 0.001f) continue;
                        Vector3 dir = d / dist;
                        float displacement = dist - targetSeparation;
                        Vector3 fj = -dir * (displacement * kBetween * 0.5f);
                        fBetweenTotal += fj;
                    }

                    Vector3 totalForce = fCircle + fBetweenTotal + fHeight;
                    float maxForce = 200f;
                    if (totalForce.magnitude > maxForce) totalForce = totalForce.normalized * maxForce;
                    rb.AddForce(totalForce);
                }
            }
        }

        if (geneticReturning.Count > 0)
        {
            foreach (var friend in geneticReturning.ToList())
            {
                if (friend == null)
                {
                    geneticReturning.Remove(friend);
                    continue;
                }
                if (!geneticReturnTargets.TryGetValue(friend, out var returnTarget)) continue;

                var rb = friend.GetComponent<Rigidbody>();
                if (rb == null) continue;

                Vector3 toTarget = returnTarget - friend.transform.position;
                Vector3 force = toTarget * geneticAttractionStrength;
                rb.AddForce(force);

                if (toTarget.magnitude < 0.25f)
                {
                    if (geneticBackups.TryGetValue(friend, out var backup))
                    {
                        if (backup.boundSink != null)
                        {
                            // Ensure friend is unbound before rebinding
                            if (friend.boundSink != null)
                            {
                                engine.UnbindPolytron(friend);
                            }
                            engine.BindPolytronToSink(friend, backup.boundSink);
                            friend.reservedForGenetics = false;
                        }
                        geneticBackups.Remove(friend);
                        friend.transform.localScale = backup.localScale;
                        var wa2 = friend.GetComponent<WaveAnimation>();
                        if (wa2 != null) wa2.Pause(false);
                        friend.interactive = true;
                    }

                    geneticReturnTargets.Remove(friend);
                    geneticReturning.Remove(friend);
                    geneticFriends.Remove(friend);
                    geneticRelativeOffsets.Remove(friend);
                }
            }

            if (geneticReturning.Count == 0)
            {
                GeneticModeActive = geneticRelativeOffsets.Count > 0;
                if (!GeneticModeActive)
                {
                    geneticBackups.Clear();
                    geneticFriends.Clear();
                    geneticReturnTargets.Clear();
                    geneticRelativeOffsets.Clear();
                }
            }
        }
    }

    internal bool IsReturning(Polytron p)
    {
        return geneticReturning != null && geneticReturning.Contains(p);
    }

    internal bool IsGeneticFriend(Polytron p)
    {
        return geneticFriends != null && geneticFriends.Contains(p);
    }
}
