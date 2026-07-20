#nullable enable
using System.Collections.Generic;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public static class CraftingPatternTimes
{
	private const int Sec = 20;
	public const int DefaultTicks = 1 * Sec;

	private static readonly Dictionary<int, int> _overrides = new()
	{
		[TileID.Furnaces]        = 3 * Sec,
		[TileID.Hellforge]       = 2 * Sec,
		[TileID.AdamantiteForge] = 1 * Sec,
	};

	public static int TicksFor(IReadOnlyList<int> tileIds)
	{
		int ticks = DefaultTicks;
		foreach (int id in tileIds)
			if (_overrides.TryGetValue(id, out var t) && t > ticks) ticks = t;
		return ticks;
	}
}
