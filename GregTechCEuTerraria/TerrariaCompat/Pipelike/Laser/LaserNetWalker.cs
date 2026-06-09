#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Pipenet;
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Laser;

// Verbatim port of com.gregtechceu.gtceu.common.pipelike.laser.LaserNetWalker.
//
// Walks a laser pipe net along ONE axis, stops at the FIRST ILaserContainer it finds adjacent to a pipe
//
// adaptations:
//   - `Direction.Axis` (X/Y/Z) -> 2D enum-equivalent: horizontal axis
//     (Left/Right) vs vertical axis (Up/Down).
//   - `LaserPipeBlockEntity` base class -> `LaserPipeCell` struct payload.
//   - `LaserPipeNet` -> `LaserPipeNet` (our port).
//   - `GTCapability.CAPABILITY_LASER` neighbour lookup -> `WorldCapability.
//     Get<ILaserContainer>(x, y)` (our 2D capability resolver). Side gating
//     is on the hatch's `SideInputCondition`/`SideOutputCondition` lambda,
//     so the walker doesn't pass a side here - the hatch self-gates when
//     energy is pushed.
public sealed class LaserNetWalker : PipeNetWalker<LaserPipeCell, LaserPipeProperties, LaserPipeNet>
{
	public static readonly LaserRoutePath FAILED_MARKER = new((0, 0), IODirection.None, 0);

	public LaserRoutePath? RoutePath { get; private set; }

	private (int x, int y) _sourcePipe;
	private IODirection    _facingToHandler;
	private Axis           _axis;

	private enum Axis { Horizontal, Vertical }

	private LaserNetWalker(LaserPipeNet net, (int x, int y) sourcePipe, int distance)
		: base(net, sourcePipe, distance) { }

	public static LaserRoutePath? CreateNetData(LaserPipeNet world, (int x, int y) sourcePipe, IODirection faceToSourceHandler)
	{
		try
		{
			var walker = new LaserNetWalker(world, sourcePipe, 1);
			walker._sourcePipe      = sourcePipe;
			walker._facingToHandler = faceToSourceHandler;
			walker._axis            = AxisOf(faceToSourceHandler);
			walker.TraversePipeNet();
			return walker.RoutePath;
		}
		catch
		{
			return FAILED_MARKER;
		}
	}

	private static Axis AxisOf(IODirection dir) => dir switch
	{
		IODirection.Left or IODirection.Right => Axis.Horizontal,
		IODirection.Up   or IODirection.Down  => Axis.Vertical,
		_                                     => Axis.Horizontal,
	};

	private static readonly IReadOnlyList<(IODirection side, int dx, int dy)> HorizontalSides =
		new (IODirection, int, int)[] { (IODirection.Left, -1, 0), (IODirection.Right, 1, 0) };
	private static readonly IReadOnlyList<(IODirection side, int dx, int dy)> VerticalSides =
		new (IODirection, int, int)[] { (IODirection.Up, 0, -1), (IODirection.Down, 0, 1) };

	protected override IReadOnlyList<(IODirection side, int dx, int dy)> GetSurroundingPipeSides() =>
		_axis switch { Axis.Horizontal => HorizontalSides, _ => VerticalSides };

	protected override bool IsValidPipe(LaserPipeCell currentPipe, LaserPipeCell otherPipe,
		(int x, int y) currentPos, IODirection side)
	{
		int bitHere  = LaserConn.Bit(side);
		int bitThere = LaserConn.Bit(side.Opposite());
		return (currentPipe.Open & bitHere) != 0 && (otherPipe.Open & bitThere) != 0;
	}

	protected override PipeNetWalker<LaserPipeCell, LaserPipeProperties, LaserPipeNet> CreateSubWalker(
		LaserPipeNet pipeNet, IODirection facingToNextPos, (int x, int y) nextPos, int walkedBlocks)
	{
		var walker = new LaserNetWalker(pipeNet, nextPos, walkedBlocks);
		walker._sourcePipe      = _sourcePipe;
		walker._facingToHandler = _facingToHandler;
		walker._axis            = _axis;
		return walker;
	}

	protected override bool TryGetCellAt((int x, int y) pos, out LaserPipeCell cell)
	{
		var c = LaserPipeLayerSystem.Pipes.CellAt(pos.x, pos.y);
		if (c is null) { cell = default; return false; }
		cell = c.Value;
		return true;
	}

	protected override void CheckPipe(LaserPipeCell pipeTile, (int x, int y) pos) { }

	protected override void CheckNeighbour(
		LaserPipeCell pipeNode, (int x, int y) pipePos, IODirection faceToNeighbour, object? neighbourTile)
	{
		if (pipePos.x == _sourcePipe.x && pipePos.y == _sourcePipe.y && faceToNeighbour == _facingToHandler)
			return;

		var root = (LaserNetWalker)Root;
		if (root.RoutePath != null) return;

		var (dx, dy) = faceToNeighbour.Offset();
		int hx = pipePos.x + dx;
		int hy = pipePos.y + dy;
		var handler = WorldCapability.Get<ILaserContainer>(hx, hy);
		if (handler != null)
		{
			root.RoutePath = new LaserRoutePath(pipePos, faceToNeighbour, WalkedBlocks);
			Stop();
		}
	}
}
