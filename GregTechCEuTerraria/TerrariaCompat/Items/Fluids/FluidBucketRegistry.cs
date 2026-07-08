#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Fluids;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Fluids;

public static class FluidBucketRegistry
{
	private static readonly Dictionary<string, int> _byFluidId = new();

	public static void Register(Mod mod)
	{
		_byFluidId.Clear();

		FluidBucketItem.GimmickHeadSlot = EquipLoader.AddEquipTexture(
			mod, "Terraria/Images/Armor_Head_13", EquipType.Head, name: "fluid_bucket_head");

		foreach (var fluid in FluidRegistry.All)
		{
			if (!fluid.HasBucket) continue;
			var item = new FluidBucketItem(fluid);
			mod.AddContent(item);
			_byFluidId[fluid.Id] = item.Type;
		}
		mod.Logger.Info($"Registered {_byFluidId.Count} fluid buckets.");
	}

	public static int? Get(string fluidId) =>
		_byFluidId.TryGetValue(fluidId, out var type) ? type : null;
}
