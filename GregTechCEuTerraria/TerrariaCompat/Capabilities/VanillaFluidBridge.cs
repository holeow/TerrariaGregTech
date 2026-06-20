#nullable enable
using GregTechCEuTerraria.Api.Data.Chemical.Material;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Fluids.Store;
using GregTechCEuTerraria.Api.Fluids;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Capabilities;

public static class VanillaFluidBridge
{
	public const int BucketAmount = VanillaBuckets.Amount;

	public static FluidStack StackFor(int itemType) =>
		VanillaBuckets.TryGet(itemType, out var e) && FluidRegistry.TryGet(e.FluidId, out var type)
			? new FluidStack(type, BucketAmount)
			: FluidStack.Empty;

	public static int EmptyVersion(int filledType) => VanillaBuckets.DrainedItem(filledType);

	public static int FilledVersion(int emptyType, FluidType fluid) =>
		emptyType == VanillaBuckets.EmptyBucket ? VanillaBuckets.FillEmptyBucket(fluid.Id) : 0;
}
