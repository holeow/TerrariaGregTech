#nullable enable
using System.Collections.Generic;
using System.IO;
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.Profiler;

public static class MachineMemoryProbe
{
	private const int SamplesPerGroup = 6;
	private const int FluidSamples    = 32;

	private static readonly Dictionary<string, int>  _counts       = new();
	private static readonly Dictionary<string, long> _bytes        = new();
	private static readonly Dictionary<string, int>  _sampled      = new();
	private static readonly Dictionary<string, long> _sampledBytes = new();

	public static void Sample()
	{
		SampleMachines();
		SampleSubsystems();
	}

	private static void SampleMachines()
	{
		_counts.Clear();
		_bytes.Clear();
		_sampled.Clear();
		_sampledBytes.Clear();

		int totalCount = 0;
		int coverCount = 0;

		foreach (var kv in TileEntity.ByID)
		{
			if (kv.Value is not MetaMachine m) continue;
			string id = m.Definition?.Id ?? m.GetType().Name;

			_counts.TryGetValue(id, out var c);
			_counts[id] = c + 1;
			totalCount++;

			_sampled.TryGetValue(id, out var s);
			if (s < SamplesPerGroup)
			{
				_sampled[id] = s + 1;
				_sampledBytes.TryGetValue(id, out var sb);
				_sampledBytes[id] = sb + SerializedBytes(m.SaveData);
			}

			for (int s2 = 0; s2 < CoverSides.Count; s2++)
				if (m.GetCoverAtSide((CoverSide)s2) != null) coverCount++;
		}

		long totalBytes = 0;
		foreach (var kv in _counts)
		{
			long est = _sampled.TryGetValue(kv.Key, out var s) && s > 0
				? _sampledBytes[kv.Key] / s * kv.Value
				: 0;
			_bytes[kv.Key] = est;
			totalBytes += est;
		}

		foreach (var kv in _counts)
			Profiler.Gauge("mem.machine_count", kv.Key, kv.Value);
		foreach (var kv in _bytes)
			Profiler.Gauge("mem.machine_state_kb", kv.Key, kv.Value >> 10);

		Profiler.Gauge("mem.machine_count", "TOTAL", totalCount);
		Profiler.Gauge("mem.machine_state_kb", "TOTAL", totalBytes >> 10);
		Profiler.Gauge("mem.subsystem", "machine_covers", coverCount);

		_machineStateBytes = totalBytes;
	}

	private static long _machineStateBytes;

	private static void SampleSubsystems()
	{
		Profiler.Gauge("mem.subsystem", "cable_cells",
			Pipelike.Cable.CableLayerSystem.Cables.Count);

		Profiler.Gauge("mem.subsystem", "item_pipe_cells",  Pipelike.ItemPipe.ItemPipeLayerSystem.Pipes.Count);
		Profiler.Gauge("mem.subsystem", "item_pipe_sides",  Pipelike.ItemPipe.ItemPipeLayerSystem.AllSides.Count);
		Profiler.Gauge("mem.subsystem", "item_pipe_nets",   Pipelike.ItemPipe.ItemPipeNetSystem.Level.AllPipeNets.Count);

		long fluidStateBytes = 0;
		int fluidStateCount = Pipelike.Fluid.FluidPipeLayerSystem.AllStates.Count;
		if (fluidStateCount > 0)
		{
			long sum = 0;
			int n = 0;
			foreach (var kv in Pipelike.Fluid.FluidPipeLayerSystem.AllStates)
			{
				sum += SerializedBytes(t => CopyInto(kv.Value.SaveTo(), t));
				if (++n >= FluidSamples) break;
			}
			fluidStateBytes = sum / n * fluidStateCount;
		}
		Profiler.Gauge("mem.subsystem", "fluid_pipe_cells",   Pipelike.Fluid.FluidPipeLayerSystem.Pipes.Count);
		Profiler.Gauge("mem.subsystem", "fluid_pipe_sides",   Pipelike.Fluid.FluidPipeLayerSystem.AllSides.Count);
		Profiler.Gauge("mem.subsystem", "fluid_pipe_states",  Pipelike.Fluid.FluidPipeLayerSystem.AllStates.Count);
		Profiler.Gauge("mem.subsystem", "fluid_pipe_nets",    Pipelike.Fluid.FluidPipeNetSystem.Level.AllPipeNets.Count);
		Profiler.Gauge("mem.subsystem", "fluid_pipe_state_kb", fluidStateBytes >> 10);

		Profiler.Gauge("mem.subsystem", "laser_pipe_cells",   Pipelike.Laser.LaserPipeLayerSystem.Pipes.Count);
		Profiler.Gauge("mem.subsystem", "laser_pipe_nets",    Pipelike.Laser.LaserPipeNetSystem.Level.AllPipeNets.Count);
		Profiler.Gauge("mem.subsystem", "optical_pipe_cells", Pipelike.Optical.OpticalPipeLayerSystem.Pipes.Count);
		Profiler.Gauge("mem.subsystem", "optical_pipe_nets",  Pipelike.Optical.OpticalPipeNetSystem.Level.AllPipeNets.Count);

		Profiler.Gauge("mem.subsystem", "recipes_total",     Recipes.RecipeRegistry.Count);
		Profiler.Gauge("mem.subsystem", "profiler_counters", Profiler.All.Count);

		Profiler.Gauge("mem.total", "world_state_kb", (_machineStateBytes + fluidStateBytes) >> 10);
	}

	private static readonly MemoryStream _scratch = new();

	private static long SerializedBytes(System.Action<TagCompound> write)
	{
		try
		{
			var tag = new TagCompound();
			write(tag);
			_scratch.SetLength(0);
			TagIO.ToStream(tag, _scratch, compress: false);
			return _scratch.Length;
		}
		catch { return 0; }
	}

	private static void CopyInto(TagCompound src, TagCompound dst)
	{
		foreach (var kv in src) dst[kv.Key] = kv.Value;
	}
}
