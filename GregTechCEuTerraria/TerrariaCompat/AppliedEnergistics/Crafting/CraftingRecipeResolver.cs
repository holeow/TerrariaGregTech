#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public static class CraftingRecipeResolver
{
	public static Terraria.Recipe? Find(MePattern pattern)
	{
		if (pattern.Type != MePatternType.Crafting) return null;
		if (pattern.PrimaryOutput is not AEItemKey outKey) return null;
		int outType = outKey.GetItem();
		long outAmt = pattern.PrimaryOutputAmount;
		int station = pattern.StationTile;

		foreach (var r in Main.recipe)
		{
			if (r is null || r.createItem is null || r.createItem.IsAir) continue;
			if (r.createItem.type != outType) continue;
			if (r.createItem.stack != outAmt) continue;
			if (!StationMatches(r, station)) continue;
			if (InputsMatch(r, pattern.Inputs)) return r;
		}
		return null;
	}

	public static int ReDeriveStationTile(
		IReadOnlyList<(AEKey what, long amount)> outputs,
		IReadOnlyList<(AEKey what, long amount)> inputs)
	{
		if (outputs.Count == 0 || outputs[0].what is not AEItemKey outKey) return -1;
		int outType = outKey.GetItem();
		long outAmt = outputs[0].amount;

		foreach (var r in Main.recipe)
		{
			if (r is null || r.createItem is null || r.createItem.IsAir) continue;
			if (r.createItem.type != outType || r.createItem.stack != outAmt) continue;
			if (!InputsMatch(r, inputs)) continue;

			int firstReq = -1, modReq = -1;
			foreach (var t in r.requiredTile)
			{
				if (t < 0) continue;
				if (firstReq < 0) firstReq = t;
				if (t >= Terraria.ID.TileID.Count && modReq < 0) modReq = t;
			}
			int pick = modReq >= 0 ? modReq : firstReq;
			if (pick >= 0) return pick;
		}
		return -1;
	}

	public static IEnumerable<Terraria.Recipe> ForOutput(int outType)
	{
		foreach (var r in Main.recipe)
		{
			if (r is null || r.createItem is null || r.createItem.IsAir) continue;
			if (r.createItem.type == outType) yield return r;
		}
	}

	private static bool StationMatches(Terraria.Recipe r, int station)
	{
		if (station < 0)
		{
			foreach (var t in r.requiredTile)
				if (t >= 0) return false;
			return true;
		}
		foreach (var t in r.requiredTile)
			if (t == station) return true;
		return false;
	}

	public static int StationTileOf(GTRecipe gt)
	{
		int[] tiles = gt.Data.GetIntArray("stationTiles");
		if (tiles.Length > 0) return tiles[0];
		int native = gt.Data.GetInt("nativeTile");
		if (native > 0) return native;
		string station = gt.RecipeType.RegistryName ?? "";
		if (VanillaCraftingBridge.IsHandStation(station)) return -1;
		return VanillaCraftingBridge.TryGetStationTile(station, out int tile) ? tile : -1;
	}

	private static AEItemKey? KeyOf(Ingredient ing)
	{
		var items = ing.GetItems();
		return items.Count > 0 && !items[0].IsAir ? AEItemKey.Of(items[0]) : null;
	}

	private static readonly Dictionary<GTRecipe, (Terraria.Recipe? rec, int index)> _resolved = new();

	public static void ClearResolvedCache() => _resolved.Clear();

	public static Terraria.Recipe? FindForGtRecipe(GTRecipe gt, out int index)
	{
		if (_resolved.TryGetValue(gt, out var hit)) { index = hit.index; return hit.rec; }
		var found = FindForGtRecipeUncached(gt, out index);
		_resolved[gt] = (found, index);
		return found;
	}

	private static Terraria.Recipe? FindForGtRecipeUncached(GTRecipe gt, out int index)
	{
		index = -1;

		int outType = 0;
		long outAmt = 0;
		foreach (var (ing, count) in gt.GetItemOutputs())
			if (KeyOf(ing) is { } k) { outType = k.GetItem(); outAmt = count; break; }
		if (outType <= 0) return null;

		var inputs = new List<(AEKey what, long amount)>();
		foreach (var (ing, count) in gt.GetItemInputs())
		{
			if (KeyOf(ing) is not { } k) return null;
			inputs.Add((k, count));
		}
		if (inputs.Count == 0) return null;

		int station = StationTileOf(gt);

		for (int i = 0; i < Terraria.Recipe.numRecipes; i++)
		{
			var r = Main.recipe[i];
			if (r is null || r.createItem is null || r.createItem.IsAir) continue;
			if (r.createItem.type != outType) continue;
			if (r.createItem.stack != outAmt) continue;
			if (!StationMatches(r, station)) continue;
			if (!InputsMatch(r, inputs)) continue;
			index = i;
			return r;
		}
		return null;
	}

	private static bool InputsMatch(Terraria.Recipe r, IReadOnlyList<(AEKey what, long amount)> inputs)
	{
		var reqs = new List<Item>();
		foreach (var it in r.requiredItem)
			if (it != null && !it.IsAir) reqs.Add(it);

		if (reqs.Count != inputs.Count) return false;

		var used = new bool[inputs.Count];
		foreach (var req in reqs)
		{
			var accepted = AcceptedTypes(r, req.type);
			bool found = false;
			for (int i = 0; i < inputs.Count; i++)
			{
				if (used[i]) continue;
				if (inputs[i].what is not AEItemKey ik) continue;
				if (inputs[i].amount != req.stack) continue;
				if (!accepted.Contains(ik.GetItem())) continue;
				used[i] = true;
				found = true;
				break;
			}
			if (!found) return false;
		}
		return true;
	}

	private static HashSet<int> AcceptedTypes(Terraria.Recipe r, int reqType)
	{
		var set = new HashSet<int> { reqType };
		foreach (var gid in r.acceptedGroups)
		{
			if (RecipeGroup.recipeGroups.TryGetValue(gid, out var g) && g.ValidItems.Contains(reqType))
				set.UnionWith(g.ValidItems);
		}
		return set;
	}
}
