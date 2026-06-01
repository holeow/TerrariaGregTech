#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using ContentEntry = GregTechCEuTerraria.Api.Recipe.Content.Content;

namespace GregTechCEuTerraria.TerrariaCompat.Recipes;

// Consumer-facing projection over GTRecipe's capability-keyed content map: lets
// an automation layer (recipe browser, future AE2 pattern encoding) enumerate
// item / fluid ingredients without touching the capability tokens / Content
// wrapper / SizedIngredient. Extension methods (not members) because GTRecipe is
// locked Api/ mirroring upstream 1:1. Pure projection, no behavior.
//
// Item entries are UNWRAPPED to (inner matcher, count); fluid entries carry the
// amount on the FluidIngredient itself.
public static class GTRecipeContents
{
	public static IEnumerable<(Ingredient ingredient, int count)> GetItemInputs(this GTRecipe recipe) =>
		ItemEntries(recipe.GetInputContents(ItemRecipeCapability.CAP));

	public static IEnumerable<(Ingredient ingredient, int count)> GetItemOutputs(this GTRecipe recipe) =>
		ItemEntries(recipe.GetOutputContents(ItemRecipeCapability.CAP));

	public static IEnumerable<(FluidIngredient ingredient, int amount)> GetFluidInputs(this GTRecipe recipe) =>
		FluidEntries(recipe.GetInputContents(FluidRecipeCapability.CAP));

	public static IEnumerable<(FluidIngredient ingredient, int amount)> GetFluidOutputs(this GTRecipe recipe) =>
		FluidEntries(recipe.GetOutputContents(FluidRecipeCapability.CAP));

	private static IEnumerable<(Ingredient, int)> ItemEntries(IReadOnlyList<ContentEntry> contents)
	{
		foreach (var c in contents)
		{
			if (c.Payload is not Ingredient ing) continue;
			if (ing is SizedIngredient s)
				yield return (s.Inner, s.Amount);
			else
				yield return (ing, 1);
		}
	}

	private static IEnumerable<(FluidIngredient, int)> FluidEntries(IReadOnlyList<ContentEntry> contents)
	{
		foreach (var c in contents)
			if (c.Payload is FluidIngredient fi)
				yield return (fi, fi.Amount);
	}
}
