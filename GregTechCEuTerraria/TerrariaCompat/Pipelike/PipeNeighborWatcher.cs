#nullable enable
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Fluid;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.ItemPipe;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Laser;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike;

public sealed class PipeNeighborWatcher : GlobalTile
{
	public override void PlaceInWorld(int i, int j, int type, Item item) => NotifyAround(i, j);

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (fail || effectOnly) return;
		NotifyAround(i, j);
	}

	public static void NotifyAround(int x, int y)
	{
		PipeRenderer.InvalidateGeomAround(x, y);

		if (Main.netMode == NetmodeID.MultiplayerClient) return;
		for (int dy = -2; dy <= 2; dy++)
		for (int dx = -2; dx <= 2; dx++)
		{
			if (dx == 0 && dy == 0) continue;
			PingPipe(x + dx, y + dy);
		}
	}

	private static void PingPipe(int x, int y)
	{
		if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return;
		if (ItemPipeLayerSystem.GetSides(x, y) is { } itemPcv)
			((ICoverable)itemPcv).OnCoversNeighborChanged();
		if (FluidPipeLayerSystem.GetSides(x, y) is { } fluidPcv)
			((ICoverable)fluidPcv).OnCoversNeighborChanged();
		if (LaserPipeLayerSystem.Pipes.Has(x, y))
			LaserPipeNetSystem.Level.GetNetFromPos((x, y))?.OnNeighbourUpdate((x, y));
		if (Optical.OpticalPipeLayerSystem.Pipes.Has(x, y))
			Optical.OpticalPipeNetSystem.Level.GetNetFromPos((x, y))?.OnNeighbourUpdate((x, y));
	}
}
