#nullable enable
using GregTechCEuTerraria.Api.Capability;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace GregTechCEuTerraria.TerrariaCompat.Machine;

public static class MachineCellResolver
{
	public static bool TryFindMachineEntityCovering(int i, int j, out MetaMachine machine)
	{
		machine = null!;
		const int max = 4;
		for (int dy = 0; dy < max; dy++)
			for (int dx = 0; dx < max; dx++)
			{
				if (!TileEntity.ByPosition.TryGetValue(new Point16(i - dx, j - dy), out var te)
				    || te is not MetaMachine m) continue;
				var (w, h) = m.Size;
				if (dx < w && dy < h) { machine = m; return true; }
			}
		return false;
	}

	public static bool TryFindMachineAt(int i, int j, out MetaMachine machine)
	{
		machine = null!;
		if (i < 0 || i >= Main.maxTilesX || j < 0 || j >= Main.maxTilesY) return false;
		Tile tile = Main.tile[i, j];
		if (!tile.HasTile) return false;

		int dx = (tile.TileFrameX / 18) % 8;
		int dy = (tile.TileFrameY / 18) % 8;
		int originX = i - dx;
		int originY = j - dy;

		if (originX < 0 || originX >= Main.maxTilesX || originY < 0 || originY >= Main.maxTilesY)
			return false;
		Tile origin = Main.tile[originX, originY];
		if (!origin.HasTile || origin.TileType != tile.TileType)
			return false;

		if (!TileEntity.ByPosition.TryGetValue(new Point16(originX, originY), out var te))
			return false;
		if (te is MetaMachine m)
		{
			machine = m;
			return true;
		}
		return false;
	}

	public static IEnumerable<(IODirection side, MetaMachine neighbor)> PerimeterNeighbors(MetaMachine machine)
	{
		var own = new HashSet<(int, int)>(machine.Cells());
		var seen = new HashSet<MetaMachine>();
		foreach (var (cx, cy) in machine.Cells())
		{
			foreach (var (side, dx, dy) in IODirectionExtensions.Cardinal4)
			{
				int nx = cx + dx, ny = cy + dy;
				if (own.Contains((nx, ny))) continue;
				if (!TryFindMachineAt(nx, ny, out var neighbor)) continue;
				if (ReferenceEquals(neighbor, machine)) continue;
				if (seen.Add(neighbor))
					yield return (side, neighbor);
			}
		}
	}

	public static bool TryFindAt<T>(int i, int j, out T machine) where T : MetaMachine
	{
		if (TryFindMachineAt(i, j, out var any) && any is T t)
		{
			machine = t;
			return true;
		}
		machine = null!;
		return false;
	}
}
