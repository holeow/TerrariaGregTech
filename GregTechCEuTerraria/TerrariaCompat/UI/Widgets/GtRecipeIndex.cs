#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public static class GtRecipeIndex
{
	private static Dictionary<int, List<GTRecipe>>? _byOutput;
	private static List<(int type, AEItemKey key)>? _outputKeys;

	public static IReadOnlyDictionary<int, List<GTRecipe>> ByOutput()
	{
		Build();
		return _byOutput!;
	}

	public static IReadOnlyList<(int type, AEItemKey key)> OutputKeys()
	{
		Build();
		return _outputKeys!;
	}

	public static bool TryResolveVanilla(GTRecipe g, out Terraria.Recipe rec, out int index)
	{
		rec = null!;
		index = -1;
		if (!VanillaCraftingBridge.GTToVanilla.TryGetValue(g, out var r) || r is null || r.createItem.IsAir) return false;
		rec = r;
		for (int i = 0; i < Terraria.Recipe.numRecipes; i++)
			if (ReferenceEquals(Main.recipe[i], r)) { index = i; return true; }
		return false;
	}

	private static void Build()
	{
		if (_byOutput != null) return;

		var d = new Dictionary<int, List<GTRecipe>>();
		foreach (var kv in RecipeRegistry.ByStation)
			foreach (var r in kv.Value)
				foreach (int t in RecipeRowRenderer.OutputItemTypesInRecipe(r))
				{
					if (t <= 0) continue;
					if (!d.TryGetValue(t, out var l)) d[t] = l = new List<GTRecipe>();
					l.Add(r);
				}
		foreach (var l in d.Values) OrderVanillaFirst(l);
		_byOutput = d;

		var keys = new List<(int type, AEItemKey key)>();
		var probe = new Item();
		foreach (int t in d.Keys)
		{
			probe.SetDefaults(t);
			var key = AEItemKey.Of(probe);
			if (key != null) keys.Add((t, key));
		}
		_outputKeys = keys;
	}

	private static void OrderVanillaFirst(List<GTRecipe> list)
	{
		int w = 0;
		var gt = new List<GTRecipe>();
		for (int i = 0; i < list.Count; i++)
		{
			var r = list[i];
			if (VanillaCraftingBridge.GTToVanilla.ContainsKey(r)) list[w++] = r;
			else gt.Add(r);
		}
		for (int i = 0; i < gt.Count; i++) list[w++] = gt[i];
	}
}
