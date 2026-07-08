#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Machine.Feature;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.Api.Recipe.Modifier;
using GregTechCEuTerraria.TerrariaCompat.Net;
using GregTechCEuTerraria.TerrariaCompat.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Machine;

public static class GregtechToiletAura
{
	public const int RadiusTiles = 42;

	private static readonly HashSet<Point16> _toilets = new();
	private static bool _scanned;

	public static bool InRange(Point16 pos)
	{
		EnsureScanned();
		if (_toilets.Count == 0) return false;
		long rsq = (long)RadiusTiles * RadiusTiles;
		foreach (var t in _toilets)
		{
			long dx = pos.X - t.X, dy = pos.Y - t.Y;
			if (dx * dx + dy * dy <= rsq) return true;
		}
		return false;
	}

	private static void EnsureScanned()
	{
		if (_scanned) return;
		_scanned = true;
		_toilets.Clear();
		ushort type = (ushort)ModContent.TileType<GregtechToiletTile>();
		for (int x = 0; x < Main.maxTilesX; x++)
		for (int y = 0; y < Main.maxTilesY; y++)
		{
			Tile t = Main.tile[x, y];
			if (t.HasTile && t.TileType == type && t.TileFrameY == 0)
				_toilets.Add(new Point16(x, y));
		}
	}

	public static void ServerAdd(int x, int y)    => _toilets.Add(new Point16(x, y));
	public static void ServerRemove(int x, int y) => _toilets.Remove(new Point16(x, y));

	public static void Reset()
	{
		_scanned = false;
		_toilets.Clear();
	}

	public static void OnPlaced(int i, int j)
	{
		Point16 origin = OriginOf(i, j);
		if (Main.netMode == NetmodeID.MultiplayerClient)
			ToiletAuraPacket.SendChange(true, origin.X, origin.Y);
		else
			ServerAdd(origin.X, origin.Y);
	}

	public static void OnRemoved(int i, int j)
	{
		Point16 origin = OriginOf(i, j);
		if (Main.netMode == NetmodeID.MultiplayerClient)
			ToiletAuraPacket.SendChange(false, origin.X, origin.Y);
		else
			ServerRemove(origin.X, origin.Y);
	}

	private static Point16 OriginOf(int i, int j)
	{
		Tile t = Main.tile[i, j];
		int oy = j - (t.TileFrameY >= 18 ? 1 : 0);
		return new Point16(i, oy);
	}

	public static readonly RecipeModifier Modifier = new ToiletModifier();

	public static GTRecipe? PostModify(IRecipeLogicMachine machine, GTRecipe? recipe)
	{
		if (recipe is null) return null;
		return Modifier.GetModifier(machine, recipe).Apply(recipe);
	}

	private sealed class ToiletModifier : RecipeModifier
	{
		public override ModifierFunction GetModifier(IRecipeLogicMachine machine, GTRecipe recipe)
		{
			if (machine is not MetaMachine m) return ModifierFunction.IDENTITY;
			if (!InRange(m.Position)) return ModifierFunction.IDENTITY;
			if (recipe.OutputEUt.Voltage > 0) return ModifierFunction.IDENTITY;
			return Flatten;
		}
	}

	private static readonly ModifierFunction Flatten = ModifierFunction.Of(recipe =>
	{
		var c = recipe.Copy();
		c.Duration = 1;
		if (recipe.InputEUt.Voltage > 0)
			EURecipeCapability.PutEUContent(c.TickInputs, new EnergyStack(1));
		return c;
	});
}

public sealed class GregtechToiletAuraSystem : ModSystem
{
	public override void OnWorldUnload() => GregtechToiletAura.Reset();
}
