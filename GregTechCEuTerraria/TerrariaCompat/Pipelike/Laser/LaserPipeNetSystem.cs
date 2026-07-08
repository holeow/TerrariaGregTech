#nullable enable
using GregTechCEuTerraria.Api.Pipenet;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Laser;

public sealed class LaserPipeNetSystem : ModSystem
{
	private static LevelLaserPipeNet? _level;

	public static LevelLaserPipeNet Level => _level ??= new LevelLaserPipeNet();

	public override void ClearWorld() => _level = new LevelLaserPipeNet();

	public override void PostUpdateEverything() => MaybeRebuild();

	public override void PostUpdateWorld()
	{
		using (Profiler.Profiler.Time("tick", "laser_pipe_net"))
		{
			MaybeRebuild();
			Profiler.Profiler.Gauge("counts", "laser_pipes", LaserPipeLayerSystem.Pipes.Count);
			Profiler.Profiler.Gauge("counts", "laser_pipe_nets", Level.AllPipeNets.Count);
		}
	}

	public static void MaybeRebuild()
	{
		if (!LaserPipeLayerSystem.Pipes.IsDirty) return;

		_level = new LevelLaserPipeNet();
		foreach (var kv in LaserPipeLayerSystem.Pipes.All)
			OnPipeAdded(kv.Key.x, kv.Key.y);
		LaserPipeLayerSystem.Pipes.ClearDirty();
	}

	public static void OnPipeAdded(int x, int y)
	{
		if (Level.GetNetFromPos((x, y)) is not null) return;

		var cell = LaserPipeLayerSystem.Pipes.CellAt(x, y);
		int open = cell?.Open ?? Node<LaserPipeProperties>.ALL_OPENED;
		Level.AddNode((x, y), LaserPipeProperties.INSTANCE,
			mark: Node<LaserPipeProperties>.DEFAULT_MARK,
			openConnections: open,
			isActive: false);
	}

	public static void OnPipeRemoved(int x, int y) => Level.RemoveNode((x, y));
}
