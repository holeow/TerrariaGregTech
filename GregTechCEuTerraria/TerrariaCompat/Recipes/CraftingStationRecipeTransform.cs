#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.TerrariaCompat.Tiles.CraftingStations;

namespace GregTechCEuTerraria.TerrariaCompat.Recipes;

public static class CraftingStationRecipeTransform
{
	public static int Apply(IDictionary<string, List<GTRecipe>> byStation)
	{
		int affected = 0;
		foreach (var list in byStation.Values)
			foreach (var recipe in list)
				if (TransformOne(recipe))
					affected++;
		return affected;
	}

	private static bool TransformOne(GTRecipe recipe)
	{
		if (!recipe.Inputs.TryGetValue(ItemRecipeCapability.CAP, out var items) || items.Count == 0)
			return false;

		HashSet<string>? keys = null;
		for (int i = items.Count - 1; i >= 0; i--)
		{
			var ing = (Ingredient)items[i].Payload;
			if (!CraftingStationRegistry.TryStationForIngredient(ing, out string stationKey))
				continue;
			(keys ??= new HashSet<string>()).Add(stationKey);
			items.RemoveAt(i);
		}

		if (keys is null) return false;

		var ordered = new List<string>(keys);
		ordered.Sort((a, b) => CraftingStationRegistry.OrderOf(a).CompareTo(CraftingStationRegistry.OrderOf(b)));
		recipe.Data["GT.CraftStations"] = ordered;
		return true;
	}
}
