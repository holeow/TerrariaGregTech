#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Tool;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools;

// AoE mining for GT drills + mining hammer + spade
public sealed class ToolAoEGlobalTile : GlobalTile
{
	private static bool _inAoE;
	private static readonly List<(int x, int y)> _pending = new();
	private static readonly List<int> _aboveColumn = new();

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (_inAoE || fail || effectOnly) return;
		if (Main.netMode == NetmodeID.Server) return;

		var player = Main.LocalPlayer;
		if (player is null || player.itemAnimation <= 0) return;
		if (player.HeldItem?.ModItem is not ToolItem tool) return;
		if (tool.ToolType == GTToolType.SCYTHE) return;

		if (i != Player.tileTargetX || j != Player.tileTargetY) return;

		int reach = Math.Max(tool.AoeColumn, tool.AoeRow);
		if (reach <= 0) return;
		if (tool.IsElectric && tool.GetCharge() <= 0) return;

		float dx = (i + 0.5f) * 16f - player.Center.X;
		float dy = (j + 0.5f) * 16f - player.Center.Y;
		bool miningHorizontal = Math.Abs(dx) >= Math.Abs(dy);

		_aboveColumn.Clear();
		for (int k = -reach; k <= reach; k++)
		{
			if (k == 0) continue;
			int nx = miningHorizontal ? i : i + k;
			int ny = miningHorizontal ? j + k : j;
			if (!WorldGen.InWorld(nx, ny, 10)) continue;
			if (!Main.tile[nx, ny].HasTile) continue;
			if (!WorldGen.CanKillTile(nx, ny)) continue;

			// gates on the player's best pickaxe so not perfect, but theres no obvious better way
			if (!player.HasEnoughPickPowerToHurtTile(nx, ny)) continue;

			if (nx == i && ny < j) _aboveColumn.Add(ny);
			else _pending.Add((nx, ny));
		}

		if (_aboveColumn.Count == 0) return;

		_aboveColumn.Sort();
		_inAoE = true;
		foreach (int ny in _aboveColumn)
		{
			if (!Main.tile[i, ny].HasTile) continue;
			WorldGen.KillTile(i, ny);
			if (Main.netMode == NetmodeID.MultiplayerClient && !Main.tile[i, ny].HasTile)
				NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, ny);
		}
		_inAoE = false;
	}

	internal static void Reset()
	{
		_pending.Clear();
		_aboveColumn.Clear();
		_inAoE = false;
	}

	internal static void FlushPending()
	{
		if (_pending.Count == 0) return;

		_pending.Sort((a, b) => a.y.CompareTo(b.y));

		_inAoE = true;
		foreach (var (nx, ny) in _pending)
		{
			if (!WorldGen.InWorld(nx, ny, 10)) continue;
			if (!Main.tile[nx, ny].HasTile) continue;
			if (!WorldGen.CanKillTile(nx, ny)) continue;

			WorldGen.KillTile(nx, ny);
			if (Main.netMode == NetmodeID.MultiplayerClient && !Main.tile[nx, ny].HasTile)
				NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, nx, ny);
		}
		_pending.Clear();
		_inAoE = false;
	}
}

public sealed class ToolAoEFlushSystem : ModSystem
{
	public override void PostUpdateEverything() => ToolAoEGlobalTile.FlushPending();
	public override void OnWorldUnload() => ToolAoEGlobalTile.Reset();
}
