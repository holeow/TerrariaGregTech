#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Pipelike;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Pipes;

public static class SimplePipeRegistry
{
	public static void Register(Mod mod)
	{
		foreach (var s in SimpleItemPipeItem.Sizes)
			mod.AddContent(new SimpleItemPipeItem(s));
		foreach (var s in SimpleFluidPipeItem.Sizes)
			mod.AddContent(new SimpleFluidPipeItem(s));
	}
}
