#nullable enable
using System.Collections.Generic;

namespace GregTechCEuTerraria.Api.Capability;

// DEVIATION: 2D adaptation of net.minecraft.core.Direction (6-way → 4-way + None).
// The capability-access `side` primitive for GetItemHandlerCap / IFluidHandler
// resolution and pipe + energy-net walks. Lives in Api.Capability (not
// TerrariaCompat) so the locked Api zone stays self-contained.
//
// Tile-Y grows downward: Up = -Y, Down = +Y.
public enum IODirection : byte
{
	None = 0,
	Up,
	Down,
	Left,
	Right,
}

public static class IODirectionExtensions
{
	// Forge Direction.getNormal() analogue - the (dx, dy) tile-delta for a side.
	public static (int dx, int dy) Offset(this IODirection d) => d switch
	{
		IODirection.Up    => (0, -1),
		IODirection.Down  => (0,  1),
		IODirection.Left  => (-1, 0),
		IODirection.Right => (1,  0),
		_                  => (0,  0),
	};

	// Forge Direction.getOpposite() analogue. Critical at the cable<->endpoint
	// boundary so side-filtering doesn't match the wrong cable.
	public static IODirection Opposite(this IODirection d) => d switch
	{
		IODirection.Up    => IODirection.Down,
		IODirection.Down  => IODirection.Up,
		IODirection.Left  => IODirection.Right,
		IODirection.Right => IODirection.Left,
		_                  => IODirection.None,
	};

	// Canonical cardinal-side iteration order + each side's normal (Forge
	// Direction.Plane.HORIZONTAL + getNormal(), projected to 2D).
	public static readonly IReadOnlyList<(IODirection side, int dx, int dy)> Cardinal4 =
		new (IODirection, int, int)[]
		{
			(IODirection.Up,    0, -1),
			(IODirection.Down,  0,  1),
			(IODirection.Left, -1,  0),
			(IODirection.Right, 1,  0),
		};
}
