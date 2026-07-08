#nullable enable
using GregTechCEuTerraria.Api.Pipenet;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.LongDistance;

public sealed class LongDistancePipeNetSystem : ModSystem
{
	private static LevelLongDistancePipeNet? _level;

	public static LevelLongDistancePipeNet Level => _level ??= new LevelLongDistancePipeNet();

	public override void ClearWorld()
	{
		_level = new LevelLongDistancePipeNet();
		LongDistanceEndpointRegistry.Clear();
	}

	public override void PostUpdateEverything() => MaybeRebuild();

	public override void PostUpdateWorld()
	{
		using (Profiler.Profiler.Time("tick", "long_distance_pipe_net"))
		{
			MaybeRebuild();
			Profiler.Profiler.Gauge("counts", "ld_pipes", LongDistancePipeLayerSystem.Pipes.Count);
			Profiler.Profiler.Gauge("counts", "ld_pipe_nets", Level.AllPipeNets.Count);
		}
	}

	public static void MaybeRebuild()
	{
		if (!LongDistancePipeLayerSystem.Pipes.IsDirty) return;

		_level = new LevelLongDistancePipeNet();
		foreach (var kv in LongDistancePipeLayerSystem.Pipes.All)
			OnPipeAdded(kv.Key.x, kv.Key.y);
		LongDistancePipeLayerSystem.Pipes.ClearDirty();

		LongDistanceEndpointRegistry.InvalidateAll();
	}

	public static void OnPipeAdded(int x, int y)
	{
		if (Level.GetNetFromPos((x, y)) is not null) return;

		var cell = LongDistancePipeLayerSystem.Pipes.CellAt(x, y);
		int mark = (cell?.Type ?? LongDistancePipeType.Item).NodeMark();
		Level.AddNode((x, y), LongDistancePipeProperties.INSTANCE,
			mark: mark,
			openConnections: Node<LongDistancePipeProperties>.ALL_OPENED,
			isActive: false);
	}

	public static void OnPipeRemoved(int x, int y)
	{
		Level.RemoveNode((x, y));
		LongDistanceEndpointRegistry.InvalidateAll();
	}
}
