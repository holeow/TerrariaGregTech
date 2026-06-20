#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Category;
using GregTechCEuTerraria.Api.Recipe.Chance.Logic;
using GregTechCEuTerraria.Api.Recipe.Content;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.Api.Tool;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using GregTechCEuTerraria.TerrariaCompat.Recipe.Condition;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools;

public static class ToolWorldEffectRecipeSynth
{
	private const string Station = "cutting_trees";
	private const string MortarStation = "grinding_with_mortar";

	public static void Register(Mod mod)
	{
		var resolver = IngredientResolverImpl.Instance;

		var sawType = GTToolType.Get("saw");
		if (sawType != null && ToolItemLoader.TryGet("gtceu:" + sawType.ResolveId("iron"), out int ironSaw))
			StationIcon.RegisterExplicit(Station, ironSaw);
		StationIcon.RegisterDisplayName(Station, "Cutting Trees with a Saw");

		var list = new List<GTRecipe>();

		var rubber = BuildSawRecipe("rubber_log", "gtceu:rubber_log", "Non-palm tree", resolver);
		if (rubber is not null) list.Add(rubber);

		var resin = BuildSawRecipe("sticky_resin", "gtceu:sticky_resin", "Palm tree", resolver);
		if (resin is not null) list.Add(resin);

		if (list.Count > 0)
		{
			RecipeRegistry.AppendAll(new Dictionary<string, List<GTRecipe>> { [Station] = list });
			mod.Logger.Info($"ToolWorldEffectRecipeSynth: synthesized {list.Count} cutting_trees browser recipes.");
		}

		RegisterMortar(mod);
	}

	private static void RegisterMortar(Mod mod)
	{
		var mortarType = GTToolType.Get("mortar");
		if (mortarType != null && ToolItemLoader.TryGet("gtceu:" + mortarType.ResolveId("iron"), out int ironMortar))
			StationIcon.RegisterExplicit(MortarStation, ironMortar);
		StationIcon.RegisterDisplayName(MortarStation, "Grinding Blocks with a Mortar");

		var list = new List<GTRecipe>
		{
			BuildMortarRecipe("stone_silt", ItemID.StoneBlock, ItemID.SiltBlock),
			BuildMortarRecipe("silt_dirt", ItemID.SiltBlock, ItemID.DirtBlock),
			BuildMortarRecipe("dirt_clay", ItemID.DirtBlock, ItemID.ClayBlock),
			BuildMortarRecipe("clay_sand", ItemID.ClayBlock, ItemID.SandBlock),
		};

		RecipeRegistry.AppendAll(new Dictionary<string, List<GTRecipe>> { [MortarStation] = list });
		mod.Logger.Info($"ToolWorldEffectRecipeSynth: synthesized {list.Count} grinding_with_mortar browser recipes.");
	}

	private static GTRecipe? BuildSawRecipe(string id, string outputUpstreamId, string label,
		IngredientResolverImpl resolver)
	{
		int outputItem = resolver.ResolveItemType(outputUpstreamId);
		if (outputItem <= 0) return null;

		var outputs = new Dictionary<object, List<Content>>
		{
			[ItemRecipeCapability.CAP] = new List<Content>
			{
				new(new ItemStackIngredient(outputItem, outputUpstreamId),
					ChanceLogic.GetMaxChancedValue(), ChanceLogic.GetMaxChancedValue(), 0),
			},
		};

		return new GTRecipe(
			recipeType:              GTRecipeType.GetOrCreate(Station),
			id:                      $"{Station}/{id}",
			inputs:                  new Dictionary<object, List<Content>>(),
			outputs:                 outputs,
			tickInputs:              new Dictionary<object, List<Content>>(),
			tickOutputs:             new Dictionary<object, List<Content>>(),
			inputChanceLogics:       new Dictionary<object, ChanceLogic>(),
			outputChanceLogics:      new Dictionary<object, ChanceLogic>(),
			tickInputChanceLogics:   new Dictionary<object, ChanceLogic>(),
			tickOutputChanceLogics:  new Dictionary<object, ChanceLogic>(),
			conditions:              new List<RecipeCondition> { new TreeCutCondition(label) },
			ingredientActions:       System.Array.Empty<object>(),
			data:                    new TagCompound(),
			duration:                0,
			recipeCategory:          GTRecipeCategory.DEFAULT,
			groupColor:              -1);
	}

	private static GTRecipe BuildMortarRecipe(string id, int inputItem, int outputItem)
	{
		var inputs = new Dictionary<object, List<Content>>
		{
			[ItemRecipeCapability.CAP] = new List<Content>
			{
				new(new ItemStackIngredient(inputItem),
					ChanceLogic.GetMaxChancedValue(), ChanceLogic.GetMaxChancedValue(), 0),
			},
		};

		var outputs = new Dictionary<object, List<Content>>
		{
			[ItemRecipeCapability.CAP] = new List<Content>
			{
				new(new ItemStackIngredient(outputItem),
					ChanceLogic.GetMaxChancedValue(), ChanceLogic.GetMaxChancedValue(), 0),
			},
		};

		return new GTRecipe(
			recipeType:              GTRecipeType.GetOrCreate(MortarStation),
			id:                      $"{MortarStation}/{id}",
			inputs:                  inputs,
			outputs:                 outputs,
			tickInputs:              new Dictionary<object, List<Content>>(),
			tickOutputs:             new Dictionary<object, List<Content>>(),
			inputChanceLogics:       new Dictionary<object, ChanceLogic>(),
			outputChanceLogics:      new Dictionary<object, ChanceLogic>(),
			tickInputChanceLogics:   new Dictionary<object, ChanceLogic>(),
			tickOutputChanceLogics:  new Dictionary<object, ChanceLogic>(),
			conditions:              new List<RecipeCondition> { new MortarGrindCondition() },
			ingredientActions:       System.Array.Empty<object>(),
			data:                    new TagCompound(),
			duration:                0,
			recipeCategory:          GTRecipeCategory.DEFAULT,
			groupColor:              -1);
	}
}
