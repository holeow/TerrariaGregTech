#nullable enable
namespace GregTechCEuTerraria.Api.Cover;

public enum CoverSide
{
	Up    = 0,
	Down  = 1,
	Left  = 2,
	Right = 3,
}

public static class CoverSides
{
	public const int Count = 4;

	public static readonly CoverSide[] All =
		{ CoverSide.Up, CoverSide.Down, CoverSide.Left, CoverSide.Right };

	public static CoverSide Opposite(CoverSide side) => side switch
	{
		CoverSide.Up    => CoverSide.Down,
		CoverSide.Down  => CoverSide.Up,
		CoverSide.Left  => CoverSide.Right,
		CoverSide.Right => CoverSide.Left,
		_               => side,
	};

	public static (int dx, int dy) Offset(CoverSide side) => side switch
	{
		CoverSide.Up    => (0, -1),
		CoverSide.Down  => (0, +1),
		CoverSide.Left  => (-1, 0),
		CoverSide.Right => (+1, 0),
		_               => (0, 0),
	};
}
