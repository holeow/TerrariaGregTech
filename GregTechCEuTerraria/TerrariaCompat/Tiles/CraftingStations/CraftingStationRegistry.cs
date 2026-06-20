#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.Api.Tool;
using GregTechCEuTerraria.TerrariaCompat.Items.Tools;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.CraftingStations;

public static class CraftingStationRegistry
{
	public sealed record StationDef(string Key, string Tool, string DisplayName, string GtTag, string ForgeTag);

	private static readonly StationDef[] _defs =
	{
		new("manual_hammer",      "hammer",      "Manual Hammer Crafting Station",      "gtceu:tools/crafting_hammers",       "forge:tools/hammers"),
		new("manual_mallet",      "mallet",      "Manual Mallet Crafting Station",      "gtceu:tools/crafting_mallets",       "forge:tools/mallets"),
		new("manual_knife",       "knife",       "Manual Knife Crafting Station",       "gtceu:tools/crafting_knives",        "forge:tools/knives"),
		new("manual_file",        "file",        "Manual File Crafting Station",        "gtceu:tools/crafting_files",         "forge:tools/files"),
		new("manual_saw",         "saw",         "Manual Saw Crafting Station",         "gtceu:tools/crafting_saws",          "forge:tools/saws"),
		new("manual_wrench",      "wrench",      "Manual Wrench Crafting Station",      "gtceu:tools/crafting_wrenches",      "forge:tools/wrenches"),
		new("manual_screwdriver", "screwdriver", "Manual Screwdriver Crafting Station", "gtceu:tools/crafting_screwdrivers",  "forge:tools/screwdrivers"),
		new("manual_wire_cutter", "wire_cutter", "Manual Wire Cutter Crafting Station", "gtceu:tools/crafting_wire_cutters",  "forge:tools/wire_cutters"),
		new("manual_mortar",      "mortar",      "Manual Mortar Crafting Station",      "gtceu:tools/crafting_mortars",       "forge:tools/mortars"),
		new("manual_crowbar",     "crowbar",     "Manual Crowbar Crafting Station",     "gtceu:tools/crafting_crowbars",      "forge:tools/crowbars"),
	};

	public static IReadOnlyList<StationDef> Defs => _defs;

	public const string UltimateKey = "ultimate_manual";
	public const string UltimateDisplayName = "Ultimate Manual Crafting Station";
	public const string UltimateOverlayId = "gtceu:nether_star";

	private static string[] AllStationKeys()
	{
		var keys = new string[_defs.Length];
		for (int i = 0; i < _defs.Length; i++) keys[i] = _defs[i].Key;
		return keys;
	}

	public static int UltimateTileType => TryGetTile(UltimateKey, out int t) ? t : 0;

	private static readonly Dictionary<string, string> _tagToStation = new(StringComparer.Ordinal);

	private static readonly Dictionary<string, int> _keyOrder = new(StringComparer.Ordinal);

	private static Mod? _mod;
	private static readonly Dictionary<string, int> _tileCache = new(StringComparer.Ordinal);

	static CraftingStationRegistry()
	{
		for (int i = 0; i < _defs.Length; i++)
		{
			var d = _defs[i];
			_tagToStation[d.GtTag] = d.Key;
			_tagToStation[d.ForgeTag] = d.Key;
			_keyOrder[d.Key] = i;
		}
	}

	public static bool TryStationForTag(string tag, out string stationKey) =>
		_tagToStation.TryGetValue(tag, out stationKey!);

	public static int OrderOf(string key) => _keyOrder.TryGetValue(key, out int o) ? o : int.MaxValue;

	public static void RegisterAll(Mod mod)
	{
		_mod = mod;
		_tileCache.Clear();
		foreach (var d in _defs)
		{
			mod.AddContent(new CraftingStationTile(d.Key, d.DisplayName));
			mod.AddContent(new CraftingStationItem(d.Key, d.DisplayName, d.GtTag));
		}
		mod.AddContent(new CraftingStationTile(UltimateKey, UltimateDisplayName, AllStationKeys()));
		mod.AddContent(new CraftingStationItem(UltimateKey, UltimateDisplayName, null));
	}

	public static IReadOnlyList<string> StationKeysFor(GTRecipe recipe)
	{
		if (!recipe.Data.ContainsKey("GT.CraftStations")) return Array.Empty<string>();
		var list = recipe.Data.GetList<string>("GT.CraftStations");
		if (list.Count == 0) return Array.Empty<string>();
		return list as IReadOnlyList<string> ?? new List<string>(list);
	}

	public static bool TryGetTile(string stationKey, out int tileType)
	{
		if (_tileCache.TryGetValue(stationKey, out tileType)) return tileType > 0;
		tileType = 0;
		if (_mod != null && _mod.TryFind<ModTile>(stationKey, out var tile)) tileType = tile.Type;
		_tileCache[stationKey] = tileType;
		return tileType > 0;
	}

	private static readonly Dictionary<string, int> _overlayTool = new(StringComparer.Ordinal);
	public static bool TryGetOverlayTool(string stationKey, out int itemType)
	{
		if (_overlayTool.TryGetValue(stationKey, out itemType)) return itemType > 0;
		itemType = ResolveOverlayTool(stationKey);
		_overlayTool[stationKey] = itemType;
		return itemType > 0;
	}

	private static int ResolveOverlayTool(string stationKey)
	{
		if (stationKey == UltimateKey)
			return IIngredientResolver.Default?.ResolveItemType(UltimateOverlayId) ?? 0;

		StationDef? def = null;
		foreach (var d in _defs) if (d.Key == stationKey) { def = d; break; }
		if (def is null) return 0;

		var toolType = GTToolType.Get(def.Tool);
		if (toolType != null && ToolItemLoader.TryGet("gtceu:" + toolType.ResolveId("iron"), out int ironType))
			return ironType;
		if (ToolItemLoader.CraftingTagItems.TryGetValue(def.GtTag, out var list) && list.Count > 0)
			return list[0];
		return 0;
	}

	private static HashSet<int>? _tileTypes;
	public static bool IsStationTile(int tileType)
	{
		if (_tileTypes is null)
		{
			_tileTypes = new HashSet<int>();
			foreach (var d in _defs)
				if (TryGetTile(d.Key, out int t)) _tileTypes.Add(t);
		}
		return _tileTypes.Contains(tileType);
	}

	public static bool TryStationForIngredient(Ingredient ing, out string stationKey)
	{
		switch (ing)
		{
			case SizedIngredient sized:       return TryStationForIngredient(sized.Inner, out stationKey);
			case IntProviderIngredient ipi:   return TryStationForIngredient(ipi.Inner, out stationKey);
			case TagIngredient tag:           return TryStationForTag(tag.TagName, out stationKey);
		}
		stationKey = "";
		return false;
	}
}
