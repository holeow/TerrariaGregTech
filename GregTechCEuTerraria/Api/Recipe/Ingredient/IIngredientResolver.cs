#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Fluids;

namespace GregTechCEuTerraria.Api.Recipe.Ingredient;

public interface IIngredientResolver
{
	static IIngredientResolver? Default { get; set; }

	int ResolveItemType(string upstreamId);

	string StableItemId(int itemType);

	IReadOnlyList<int> ResolveItemTag(string tagName);

	FluidType? ResolveFluidType(string upstreamId);

	IReadOnlyList<FluidType> ResolveFluidTag(string tagName);
}
