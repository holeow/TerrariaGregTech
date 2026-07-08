#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
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
