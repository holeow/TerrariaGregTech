#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Fluids;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.Api.Recipe.Ingredient;

// Port of com.gregtechceu.gtceu.api.recipe.ingredient.FluidContainerIngredient.
// adaptations:
//   - tryEmptyContainer "can this be emptied" dropped
public sealed class FluidContainerIngredient : Ingredient
{
	public FluidIngredient Fluid { get; }

	public FluidContainerIngredient(FluidIngredient fluid) => Fluid = fluid;

	public override bool Test(Item item)
	{
		if (item is null || item.IsAir) return false;
		var contained = GetFluidContained(item);
		return !contained.IsEmpty
		    && Fluid.TestStack(contained)
		    && contained.Amount >= Fluid.Amount;
	}

	private IReadOnlyList<Item>? _cachedItems;

	public override IReadOnlyList<Item> GetItems()
	{
		if (_cachedItems is not null) return _cachedItems;
		var items = new List<Item>();
		foreach (var fluid in Fluid.GetFluids())
		{
			int bucket = VanillaBucketFor(fluid);
			if (bucket == 0) continue;
			var stack = new Item();
			stack.SetDefaults(bucket);
			items.Add(stack);
		}
		return _cachedItems = items;
	}

	public override bool IsEmpty => Fluid.IsEmpty;

	public override string GetTypeName() => "gtceu:fluid_container";

	private static FluidStack GetFluidContained(Item item)
	{
		if (item.ModItem is IFluidHandlerItem handler)
			return handler.GetTank(0);

		if (VanillaBuckets.TryGet(item.type, out var entry)
		    && FluidRegistry.TryGet(entry.FluidId, out var type))
			return new FluidStack(type, VanillaBuckets.Amount);
		return FluidStack.Empty;
	}

	private static int VanillaBucketFor(FluidType? fluid) =>
		fluid is null ? 0 : VanillaBuckets.FillEmptyBucket(fluid.Id);
}
