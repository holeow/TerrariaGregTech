#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Category;
using GregTechCEuTerraria.Api.Recipe.Chance.Logic;
using GregTechCEuTerraria.Api.Recipe.Content;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.Common.Recipe;
using GregTechCEuTerraria.Common.Recipe.Condition;
using GregTechCEuTerraria.TerrariaCompat.BossDrops.MultiblockBag;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock;

public static class MultiblockStructureRecipeSynth
{
	public static void Register(Mod mod)
	{
		var list = new List<GTRecipe>();
		foreach (var def in MachineRegistry.All)
		{
			if (def.PatternFactory is null) continue;
			var recipe = BuildRecipe(mod, def);
			if (recipe is null) continue;
			list.Add(recipe);
		}

		if (list.Count > 0)
		{
			RecipeRegistry.AppendAll(new Dictionary<string, List<GTRecipe>>
			{
				[GTRecipeTypes.MULTIBLOCK_STRUCTURE.RegistryName] = list,
			});
		}
		mod.Logger.Info(
			$"MultiblockStructureRecipeSynth: synthesized {list.Count} multiblock structure browser recipes.");
	}

	private static GTRecipe? BuildRecipe(Mod mod, MachineDefinition def)
	{
		var components = MultiblockBagContents.Resolve(mod, def);
		if (components is null || components.Count == 0) return null;

		var tier = def.Tiers.Length > 0 ? def.Tiers[0] : VoltageTier.LV;
		string controllerName = def.Tiered ? $"{VoltageTiers.Id(tier)}_{def.Id}" : def.Id;
		if (!mod.TryFind<ModItem>(controllerName, out var ctrlItem)) return null;
		int controllerItemType = ctrlItem.Type;

		int maxChance = ChanceLogic.GetMaxChancedValue();

		var inList = new List<Content>(components.Count);
		foreach (var drop in components)
		{
			Ingredient ing = drop.Count > 1
				? new SizedIngredient(new ItemStackIngredient(drop.ItemType), drop.Count)
				: new ItemStackIngredient(drop.ItemType);
			inList.Add(new Content(ing, maxChance, maxChance, 0));
		}
		var inputs = new Dictionary<object, List<Content>>
		{
			[ItemRecipeCapability.CAP] = inList,
		};

		var outputs = new Dictionary<object, List<Content>>
		{
			[ItemRecipeCapability.CAP] = new List<Content>
			{
				new(new ItemStackIngredient(controllerItemType), maxChance, maxChance, 0),
			},
		};

		return new GTRecipe(
			recipeType:              GTRecipeTypes.MULTIBLOCK_STRUCTURE,
			id:                      $"multiblock_structure/{def.Id}",
			inputs:                  inputs,
			outputs:                 outputs,
			tickInputs:              new Dictionary<object, List<Content>>(),
			tickOutputs:             new Dictionary<object, List<Content>>(),
			inputChanceLogics:       new Dictionary<object, ChanceLogic>(),
			outputChanceLogics:      new Dictionary<object, ChanceLogic>(),
			tickInputChanceLogics:   new Dictionary<object, ChanceLogic>(),
			tickOutputChanceLogics:  new Dictionary<object, ChanceLogic>(),
			conditions:              new List<RecipeCondition>(),
			ingredientActions:       System.Array.Empty<object>(),
			data:                    new TagCompound(),
			duration:                0,
			recipeCategory:          GTRecipeCategory.DEFAULT,
			groupColor:              -1);
	}
}
