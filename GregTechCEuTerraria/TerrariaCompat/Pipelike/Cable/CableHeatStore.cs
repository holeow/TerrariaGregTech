#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.TerrariaCompat.Items.Cables;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Cable;

public static class CableHeatStore
{
	public const int DefaultTemp = 293;
	public const int MeltTemp    = 3000;
	private const int InsulationMeltTemp = 1500;

	private sealed class Cell
	{
		public int Temperature = DefaultTemp;
		public int HeatQueue;
	}

	private static readonly Dictionary<(int x, int y), Cell> _cells = new();
	private static readonly Dictionary<(int x, int y), long> _ampsThisTick = new();

	public static void Clear()
	{
		_cells.Clear();
		_ampsThisTick.Clear();
	}

	private static Cell GetOrCreate((int x, int y) pos)
	{
		if (!_cells.TryGetValue(pos, out var c))
		{
			c = new Cell();
			_cells[pos] = c;
		}
		return c;
	}

	public static void ApplyHeat((int x, int y) pos, int amount)
	{
		if (amount <= 0) return;
		GetOrCreate(pos).HeatQueue += amount;
	}

	public static void IncrementAmperage((int x, int y) pos, long amps, long maxAmperage)
	{
		_ampsThisTick.TryGetValue(pos, out long used);
		used += amps;
		_ampsThisTick[pos] = used;
		long dif = used - maxAmperage;
		if (dif > 0) ApplyHeat(pos, (int)Math.Min(int.MaxValue, dif * 40));
	}

	public static void OverVoltage((int x, int y) pos, long voltage, long cableMaxVoltage)
	{
		int tierDiff = (int)VoltageTiers.MaxTierForVoltage(voltage)
		             - (int)VoltageTiers.MaxTierForVoltage(cableMaxVoltage);
		if (tierDiff < 1) tierDiff = 1;
		int heat = (int)(Math.Log(tierDiff) * 45 + 36.5);
		ApplyHeat(pos, heat);
	}

	public static void BeginTick() => _ampsThisTick.Clear();

	private static readonly List<(int x, int y)> _scratch = new();
	public static void UpdateTick()
	{
		if (_cells.Count == 0) return;
		_scratch.Clear();
		foreach (var kv in _cells) _scratch.Add(kv.Key);

		foreach (var pos in _scratch)
		{
			if (!_cells.TryGetValue(pos, out var c)) continue;

			if (c.HeatQueue > 0)
				c.Temperature += c.HeatQueue;

			if (c.Temperature >= MeltTemp)
			{
				MeltCable(pos);
				_cells.Remove(pos);
				continue;
			}

			if (c.Temperature <= DefaultTemp)
			{
				_cells.Remove(pos);
				continue;
			}

			var cell = CableLayerSystem.Cables.CellAt(pos.x, pos.y);
			if (cell is null)
			{
				_cells.Remove(pos);
				continue;
			}

			if (cell.Value.Insulated && c.Temperature >= InsulationMeltTemp && Main.rand.NextFloat() < 0.1f)
			{
				Uninsulate(pos, cell.Value, c);
				continue;
			}

			if (c.HeatQueue == 0)
				c.Temperature -= (int)Math.Pow(c.Temperature - DefaultTemp, 0.35);
			else
				c.HeatQueue = 0;
		}
	}

	private static void Uninsulate((int x, int y) pos, CableCell insulated, Cell heat)
	{
		var bare = WireItemRegistry.BuildCell(insulated.MaterialId, insulated.WireSize, insulated: false);
		if (bare is null)
		{
			heat.HeatQueue = 0;
			return;
		}
		CableLayerSystem.Cables.Set(pos.x, pos.y, bare.Value);
		TerrariaCompat.Net.CablePackets.SendSet(pos.x, pos.y, bare.Value);
		heat.HeatQueue = 0;
	}

	public static void MeltCable((int x, int y) pos)
	{
		var cell = CableLayerSystem.Cables.CellAt(pos.x, pos.y);

		TerrariaCompat.Net.BlockExplosionEffectPacket.PlayLocal(pos.x, pos.y, 1, 1);
		if (Main.netMode == NetmodeID.Server)
			TerrariaCompat.Net.BlockExplosionEffectPacket.Send(pos.x, pos.y, 1, 1);

		if (cell is { } c && Main.netMode != NetmodeID.MultiplayerClient)
		{
			int? itemType = WireItemRegistry.Get(c.MaterialId, c.WireSize, c.Insulated);
			if (itemType is not null)
			{
				int worldX = pos.x * 16;
				int worldY = pos.y * 16;
				Item.NewItem(new EntitySource_TileBreak(pos.x, pos.y),
					worldX, worldY, 16, 16, itemType.Value);
			}
		}

		CableLayerSystem.Cables.Remove(pos.x, pos.y);
		TerrariaCompat.Net.CablePackets.SendRemoveBroadcast(pos.x, pos.y);
	}

	public static void Save(TagCompound tag)
	{
		var xs = new List<int>();
		var ys = new List<int>();
		var temps = new List<int>();
		foreach (var kv in _cells)
		{
			if (kv.Value.Temperature <= DefaultTemp) continue;
			xs.Add(kv.Key.x);
			ys.Add(kv.Key.y);
			temps.Add(kv.Value.Temperature);
		}
		if (xs.Count == 0) return;
		tag["cableHeat.xs"] = xs;
		tag["cableHeat.ys"] = ys;
		tag["cableHeat.temps"] = temps;
	}

	public static void Load(TagCompound tag)
	{
		_cells.Clear();
		_ampsThisTick.Clear();
		if (!tag.ContainsKey("cableHeat.xs")) return;
		var xs = tag.GetList<int>("cableHeat.xs");
		var ys = tag.GetList<int>("cableHeat.ys");
		var temps = tag.GetList<int>("cableHeat.temps");
		int n = Math.Min(xs.Count, Math.Min(ys.Count, temps.Count));
		for (int i = 0; i < n; i++)
			_cells[(xs[i], ys[i])] = new Cell { Temperature = temps[i] };
	}
}
