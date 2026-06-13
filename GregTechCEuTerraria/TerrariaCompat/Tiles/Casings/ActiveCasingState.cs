#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.Casings;

public sealed class ActiveCasingState : ModSystem
{
	private static HashSet<long> _activeCells = new();
	private int _tick;

	public override void OnWorldUnload()
	{
		_activeCells = new HashSet<long>();
		_tick = 0;
	}

	private static long Pack(int x, int y) => ((long)y << 32) | (uint)x;

	public static bool IsActive(int x, int y) => _activeCells.Contains(Pack(x, y));

	public override void PostUpdateEverything()
	{
		if (Main.dedServ) return;
		if (++_tick % 10 != 0) return;
		Rebuild();
	}

	private static void Rebuild()
	{
		bool canCompute = Main.netMode != NetmodeID.MultiplayerClient;
		var next = new HashSet<long>();
		foreach (var te in TileEntity.ByID.Values)
		{
			if (te is not MultiblockControllerMachine c || !c.ShouldGlow) continue;
			if (canCompute) c.RefreshActiveCells();
			foreach (var p in c.ActiveCells)
				next.Add(Pack(p.X, p.Y));
		}
		_activeCells = next;
	}
}
