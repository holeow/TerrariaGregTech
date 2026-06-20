#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Api.Machine.Feature;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GtIngredient = GregTechCEuTerraria.Api.Recipe.Ingredient.Ingredient;

namespace GregTechCEuTerraria.Api.Recipe.Lookup;

public static class RecipeLookupCompiler
{
	public static List<List<AbstractMapIngredient>>? TryCompileRecipe(GTRecipe recipe)
	{
		var slots = new List<List<AbstractMapIngredient>>();
		if (!AddItemSlots(recipe.GetInputContents(ItemRecipeCapability.CAP), slots))       return null;
		if (!AddItemSlots(recipe.GetTickInputContents(ItemRecipeCapability.CAP), slots))   return null;
		if (!AddFluidSlots(recipe.GetInputContents(FluidRecipeCapability.CAP), slots))     return null;
		if (!AddFluidSlots(recipe.GetTickInputContents(FluidRecipeCapability.CAP), slots)) return null;
		return slots;
	}

	private static bool AddItemSlots(
		IReadOnlyList<Content.Content> contents, List<List<AbstractMapIngredient>> slots)
	{
		foreach (var c in contents)
		{
			if (c.Payload is not GtIngredient ing) return false;
			var keys = DecomposeItem(ing);
			if (keys == null) return false;
			slots.Add(keys);
		}
		return true;
	}

	private static bool AddFluidSlots(
		IReadOnlyList<Content.Content> contents, List<List<AbstractMapIngredient>> slots)
	{
		foreach (var c in contents)
		{
			if (c.Payload is not FluidIngredient fi) return false;
			var keys = DecomposeFluid(fi);
			if (keys == null) return false;
			slots.Add(keys);
		}
		return true;
	}

	private static List<AbstractMapIngredient>? DecomposeItem(GtIngredient ing)
	{
		while (true)
		{
			if (ing is SizedIngredient s)       { ing = s.Inner; continue; }
			if (ing is IntProviderIngredient p) { ing = p.Inner; continue; }
			break;
		}
		switch (ing)
		{
			case IntCircuitIngredient circuit:
				return new List<AbstractMapIngredient> { new CircuitMapIngredient(circuit.Configuration) };
			case ItemStackIngredient item:
				return item.ItemType != 0
					? new List<AbstractMapIngredient> { new ItemMapIngredient(item.ItemType) }
					: null;
			case TagIngredient tag:
			{
				var keys = new List<AbstractMapIngredient>(tag.ResolvedTypes.Count);
				foreach (int type in tag.ResolvedTypes)
					if (type != 0) keys.Add(new ItemMapIngredient(type));
				return keys.Count > 0 ? keys : null;
			}
			default:
				return null;
		}
	}

	private static List<AbstractMapIngredient>? DecomposeFluid(FluidIngredient fi)
	{
		var fluids = fi.GetFluids();
		if (fluids.Count == 0) return null;
		var keys = new List<AbstractMapIngredient>(fluids.Count);
		foreach (var f in fluids) keys.Add(new FluidMapIngredient(f.Id));
		return keys;
	}

	public static List<List<AbstractMapIngredient>> CompileQuery(IRecipeLogicMachine holder)
	{
		var query = new List<List<AbstractMapIngredient>>();

		var seenItems = new HashSet<int>();
		var seenCircuits = new HashSet<int>();
		foreach (var item in holder.LookupInputItems)
		{
			if (item is null || item.IsAir) continue;
			if (item.ModItem is TerrariaCompat.Items.IntCircuitItem circuit)
			{
				if (seenCircuits.Add(circuit.Configuration))
					query.Add(new List<AbstractMapIngredient> { new CircuitMapIngredient(circuit.Configuration) });
				continue;
			}
			if (seenItems.Add(item.type))
				query.Add(new List<AbstractMapIngredient> { new ItemMapIngredient(item.type) });
		}

		var seenFluids = new HashSet<string>();
		foreach (var fluid in holder.LookupInputFluids)
		{
			if (fluid.IsEmpty || fluid.Type is null) continue;
			if (seenFluids.Add(fluid.Type.Id))
				query.Add(new List<AbstractMapIngredient> { new FluidMapIngredient(fluid.Type.Id) });
		}

		return query;
	}
}
