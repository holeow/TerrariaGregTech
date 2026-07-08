#nullable enable
using System.Linq;
using GregTechCEuTerraria.Api.Data.Chemical.Material;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Api.Fluids.Store;
using GregTechCEuTerraria.Common.Materials;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Loaders;

internal static class FluidLoader
{
	public static void RegisterAll(Mod mod)
	{
		foreach (var material in MaterialRegistry.All.Values)
			MaybeEnqueueAlloyMolten(material);

		int registered = 0;
		foreach (var material in MaterialRegistry.All.Values)
		{
			if (material.FluidProperty is not { } prop) continue;
			prop.RegisterFluids(material);
			registered += prop.Fluids.Count();
		}

		mod.Logger.Info($"Registered {registered} fluids across {MaterialRegistry.All.Count} materials.");
	}

	private static void MaybeEnqueueAlloyMolten(Material material)
	{
		if (material.Components.Count < 2) return;
		if (material.BlastTemperatureK is null) return;
		if (material.FluidProperty is not { } prop) return;
		if (prop.GetQueuedBuilder(FluidStorageKey.MOLTEN) is not null) return;

		int fluidOnly = 0;
		foreach (var comp in material.Components)
		{
			if (IsComponentFluidOnly(comp.MaterialId) && ++fluidOnly > 2)
				return;
		}

		prop.EnqueueRegistration(FluidStorageKey.MOLTEN,
			new FluidBuilder().State(FluidState.LIQUID));
	}

	private static bool IsComponentFluidOnly(string componentId) =>
		MaterialRegistry.All.TryGetValue(componentId, out var cm)
		&& !cm.Forms.Contains("DUST")
		&& cm.FluidProperty is not null;
}
