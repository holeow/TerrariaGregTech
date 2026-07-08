#nullable enable
using GregTechCEuTerraria.Api;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;

public sealed class MeTerminalSyncSystem : ModSystem
{
	public override void PostUpdateWorld()
	{
		if (Main.GameUpdateCount % (uint)TickScale.FromMcTicks(1) != 0) return;

		foreach (var te in TileEntity.ByID.Values)
			if (te is MeTerminalMachine term)
				term.DriveSync();
	}
}
