#nullable enable
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.CraftingStations;

public static class CraftingStationAnim
{
	private const int Frames        = 8;
	private const int TicksPerFrame = 5;
	private const int Cycles        = 2;
	private const int DurationTicks = Frames * TicksPerFrame * Cycles;

	private static readonly Dictionary<Point16, uint> _until = new();

	public static void Trigger(Point16 origin) => _until[origin] = Main.GameUpdateCount + DurationTicks;

	public static int FrameFor(Point16 origin)
	{
		if (!_until.TryGetValue(origin, out uint until)) return 0;
		if ((int)(until - Main.GameUpdateCount) <= 0) { _until.Remove(origin); return 0; }
		return (int)(Main.GameUpdateCount / TicksPerFrame) % Frames;
	}

	public static void Clear() => _until.Clear();
}

public sealed class CraftingStationCraftListener : GlobalItem
{
	public override void OnCreated(Item item, ItemCreationContext context)
	{
		if (Main.dedServ || context is not RecipeItemCreationContext rc) return;
		var req = rc.Recipe.requiredTile;
		if (req is null || req.Count == 0) return;
		foreach (int tileType in req)
		{
			if (!CraftingStationRegistry.IsStationTile(tileType)) continue;
			if (TryFindNearestStation(tileType, out Point16 origin))
				CraftingStationAnim.Trigger(origin);
		}
	}

	private static bool TryFindNearestStation(int tileType, out Point16 origin)
	{
		origin = default;
		const int R = 30;
		var player = Main.LocalPlayer;
		int px = (int)(player.Center.X / 16f);
		int py = (int)(player.Center.Y / 16f);
		int x0 = System.Math.Max(0, px - R), x1 = System.Math.Min(Main.maxTilesX - 1, px + R);
		int y0 = System.Math.Max(0, py - R), y1 = System.Math.Min(Main.maxTilesY - 1, py + R);

		bool found = false;
		float best = float.MaxValue;
		for (int i = x0; i <= x1; i++)
		for (int j = y0; j <= y1; j++)
		{
			Tile t = Main.tile[i, j];
			if (!t.HasTile || !AppliedEnergistics.Crafting.RecipeNetworkCrafting.TileSatisfies(t.TileType, tileType)) continue;
			int ox = i - t.TileFrameX / 18, oy = j - t.TileFrameY / 18;
			float dx = ox + 1.5f - px, dy = oy + 1.5f - py;
			float d = dx * dx + dy * dy;
			if (d < best) { best = d; origin = new Point16(ox, oy); found = true; }
		}
		return found;
	}

}

public sealed class CraftingStationAnimSystem : ModSystem
{
	public override void OnWorldUnload() => CraftingStationAnim.Clear();
}
