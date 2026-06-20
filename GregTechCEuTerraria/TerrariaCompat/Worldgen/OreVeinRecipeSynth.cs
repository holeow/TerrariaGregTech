#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Category;
using GregTechCEuTerraria.Api.Recipe.Chance.Logic;
using GregTechCEuTerraria.Api.Recipe.Content;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.TerrariaCompat.Items;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using GregTechCEuTerraria.TerrariaCompat.Recipe.Condition;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.Worldgen;

public static class OreVeinRecipeSynth
{
	private const string Station = "ore_vein";

	private static readonly string[] OrePrefixes = { "raw_ore", "gem", "dust" };

	public static void Register(Mod mod)
	{
		StationIcon.RegisterExplicit(Station, Terraria.ID.ItemID.CopperPickaxe);

		var list = new List<GTRecipe>();
		foreach (var vein in VeinRegistry.All)
		{
			var recipe = BuildVeinRecipe(vein);
			if (recipe is not null) list.Add(recipe);
		}
		if (list.Count == 0) return;

		RecipeRegistry.AppendAll(new Dictionary<string, List<GTRecipe>> { [Station] = list });
		mod.Logger.Info($"OreVeinRecipeSynth: synthesized {list.Count} ore_vein browser recipes.");
	}

	private static GTRecipe? BuildVeinRecipe(VeinDefinition vein)
	{
		var resolved = new List<(ItemStackIngredient Ing, int Weight)>(vein.Materials.Count);
		int totalWeight = 0;
		foreach (var m in vein.Materials)
		{
			int item = ResolveOreItem(m.MaterialId, out string upstreamId);
			if (item <= 0 || m.Weight <= 0) continue;
			resolved.Add((new ItemStackIngredient(item, upstreamId), m.Weight));
			totalWeight += m.Weight;
		}
		if (resolved.Count == 0) return null;

		var outContents = new List<Content>(resolved.Count);
		foreach (var (ing, weight) in resolved)
			outContents.Add(new Content(ing, weight, totalWeight, 0));
		var outputs = new Dictionary<object, List<Content>>
		{
			[ItemRecipeCapability.CAP] = outContents,
		};

		return new GTRecipe(
			recipeType:              GTRecipeType.GetOrCreate(Station),
			id:                      $"{Station}/{vein.Id}",
			inputs:                  new Dictionary<object, List<Content>>(),
			outputs:                 outputs,
			tickInputs:              new Dictionary<object, List<Content>>(),
			tickOutputs:             new Dictionary<object, List<Content>>(),
			inputChanceLogics:       new Dictionary<object, ChanceLogic>(),
			outputChanceLogics:      new Dictionary<object, ChanceLogic>(),
			tickInputChanceLogics:   new Dictionary<object, ChanceLogic>(),
			tickOutputChanceLogics:  new Dictionary<object, ChanceLogic>(),
			conditions:              new List<RecipeCondition> { new OreVeinCondition(vein.Layer, vein.HeightMin, vein.HeightMax) },
			ingredientActions:       System.Array.Empty<object>(),
			data:                    new TagCompound(),
			duration:                0,
			recipeCategory:          GTRecipeCategory.DEFAULT,
			groupColor:              -1);
	}

	private static int ResolveOreItem(string materialId, out string upstreamId)
	{
		foreach (var prefix in OrePrefixes)
		{
			int item = MaterialItemRegistry.Get(materialId, prefix) ?? 0;
			if (item > 0) { upstreamId = $"gtceu:{prefix}_{materialId}"; return item; }
		}
		upstreamId = "";
		return 0;
	}
}
