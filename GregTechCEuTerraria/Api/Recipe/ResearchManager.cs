#nullable enable
using System.Collections.Generic;
using Terraria;

namespace GregTechCEuTerraria.Api.Recipe;

// Port of com.gregtechceu.gtceu.utils.ResearchManager
//
// adaptations:
//  item NBT -> `ResearchDataGlobalItem` per-stack blob
//  `isStackDataItem` IComponentItem/IDataItem behaviour lookup -> a static type check
//  `DataStickCopyScannerLogic` custom-recipe + default-research-recipe builders are not ported
public static class ResearchManager
{
	public readonly struct ResearchItem
	{
		public readonly GTRecipeType RecipeType;
		public readonly string       ResearchId;
		public ResearchItem(GTRecipeType recipeType, string researchId)
		{
			RecipeType = recipeType;
			ResearchId = researchId;
		}
	}

	private static bool RequireDataBank(int type)
	{
		var mod = Terraria.ModLoader.ModContent.GetInstance<GregTechCEuTerraria>();
		return mod.TryFind<Terraria.ModLoader.ModItem>("data_module", out var mi) && mi.Type == type;
	}

	public static bool IsStackDataItem(Item stack, bool isDataBank)
	{
		if (stack is null || stack.IsAir) return false;
		if (!TerrariaCompat.Items.ResearchDataGlobalItem.IsDataItemType(stack.type)) return false;
		return !RequireDataBank(stack.type) || isDataBank;
	}

	public static ResearchItem? ReadResearchId(Item stack)
	{
		if (stack is null || stack.IsAir) return null;
		if (!TerrariaCompat.Items.ResearchDataGlobalItem.IsDataItemType(stack.type)) return null;
		if (!stack.TryGetGlobalItem<TerrariaCompat.Items.ResearchDataGlobalItem>(out var blob) || !blob.HasResearch) return null;
		var type = GTRecipeType.Get(StripNs(blob.ResearchType ?? ""));
		if (type is null) return null;
		return new ResearchItem(type, blob.ResearchId!);
	}

	public static void WriteResearchToStack(Item stack, string researchId, GTRecipeType recipeType)
	{
		if (stack is null || stack.IsAir) return;
		var blob = stack.GetGlobalItem<TerrariaCompat.Items.ResearchDataGlobalItem>();
		blob.ResearchId   = researchId;
		blob.ResearchType = recipeType.RegistryName;
	}

	public static bool HasResearchTag(Item stack)
	{
		if (stack is null || stack.IsAir) return false;
		if (!TerrariaCompat.Items.ResearchDataGlobalItem.IsDataItemType(stack.type)) return false;
		return stack.TryGetGlobalItem<TerrariaCompat.Items.ResearchDataGlobalItem>(out var blob) && blob.HasResearch;
	}

	public static IReadOnlyCollection<GTRecipe> GetRecipesFor(GTRecipeType recipeType, string researchId) =>
		recipeType.GetDataStickEntry(researchId) ?? System.Array.Empty<GTRecipe>();

	private static string StripNs(string id)
	{
		int i = id.IndexOf(':');
		return i >= 0 ? id[(i + 1)..] : id;
	}
}
