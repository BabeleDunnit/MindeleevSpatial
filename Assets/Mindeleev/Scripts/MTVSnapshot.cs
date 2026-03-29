using System.Collections.Generic;
using UnityEngine;

// Lightweight snapshot DTO used to save/restore Mutatron runtime state
public class MTVSnapshot
{
    // polytron sealNumber -> HexCoord? (null means unbound / home)
    public Dictionary<int, HexCoord?> polytronBoundCoords = new Dictionary<int, HexCoord?>();

    // per-polytron home cooldowns
    public Dictionary<int, int> homeCooldowns = new Dictionary<int, int>();

    // architron runtime flags
    public bool architronFollowingAvatar = false;

    // architron index
    public int architronIdx = -1;

    // saved tile state for Mutatron cells (coord -> recipe)
    public Dictionary<HexCoord, string> tileRecipes = new Dictionary<HexCoord, string>();

    // saved polytronic numbers for Mutatron cells (coord -> polytronicNumber)
    public Dictionary<HexCoord, int> polytronicNumbers = new Dictionary<HexCoord, int>();

    // MTV-specific: saved polytron emanations before visualization (sealNumber -> recipe)
    public Dictionary<int, string> polytronEmanations = new Dictionary<int, string>();

    // MTV-specific: Architron's captured emanations for MindeleevTable viz
    // Key format: "{palette}_{polytronicNumber}" (e.g., "03_2"), Value: recipe
    public Dictionary<string, string> architronEmanationsMap = new Dictionary<string, string>();

    // MTV-specific: saved polytron local scales before MTV transformation (sealNumber -> scale)
    public Dictionary<int, Vector3> polytronScales = new Dictionary<int, Vector3>();
}
