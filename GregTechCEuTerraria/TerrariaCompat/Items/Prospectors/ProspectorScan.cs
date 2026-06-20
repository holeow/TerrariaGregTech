#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Common.Materials;
using GregTechCEuTerraria.TerrariaCompat.Items;
using GregTechCEuTerraria.TerrariaCompat.Tiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Prospectors;

public static class ProspectorScan
{
	public readonly struct OreHit
	{
		public readonly ushort TileType;
		public readonly int IconItem;
		public readonly string Name;
		public readonly int Count;
		public readonly Point NearestTile;
		public readonly float NearestDistPx;

		public OreHit(ushort tileType, int iconItem, string name, int count, Point nearestTile, float nearestDistPx)
		{
			TileType = tileType;
			IconItem = iconItem;
			Name = name;
			Count = count;
			NearestTile = nearestTile;
			NearestDistPx = nearestDistPx;
		}
	}

	private static Dictionary<ushort, (int Icon, string Name)>? _catalog;

	private static Dictionary<ushort, (int Icon, string Name)> Catalog()
	{
		if (_catalog != null) return _catalog;
		var map = new Dictionary<ushort, (int, string)>();

		for (int t = 0; t < ItemID.Count; t++)
		{
			Item it = ContentSamples.ItemsByType[t];
			int tile = it.createTile;
			if (tile < 0 || tile >= TileID.Sets.Ore.Length) continue;
			if (!TileID.Sets.Ore[tile]) continue;
			var key = (ushort)tile;
			if (!map.ContainsKey(key))
				map[key] = (t, Lang.GetItemNameValue(t));
		}

		foreach (var (materialId, tile) in OreTileRegistry.All)
		{
			int raw = MaterialItemRegistry.Get(materialId, "raw_ore") ?? 0;
			string name = Language.GetTextValue($"Mods.GregTechCEuTerraria.Materials.{materialId}") + " Ore";
			map[tile.Type] = (raw, name);
		}

		_catalog = map;
		return _catalog;
	}

	public static List<OreHit> ScanAround(Player player, int radiusTiles)
	{
		var catalog = Catalog();

		int cx = (int)(player.Center.X / 16f);
		int cy = (int)(player.Center.Y / 16f);
		int x0 = System.Math.Clamp(cx - radiusTiles, 1, Main.maxTilesX - 2);
		int x1 = System.Math.Clamp(cx + radiusTiles, 1, Main.maxTilesX - 2);
		int y0 = System.Math.Clamp(cy - radiusTiles, 1, Main.maxTilesY - 2);
		int y1 = System.Math.Clamp(cy + radiusTiles, 1, Main.maxTilesY - 2);

		var counts = new Dictionary<ushort, int>();
		var nearest = new Dictionary<ushort, Point>();
		var nearestSq = new Dictionary<ushort, float>();
		Vector2 center = player.Center;

		for (int x = x0; x <= x1; x++)
		for (int y = y0; y <= y1; y++)
		{
			Tile tile = Main.tile[x, y];
			if (!tile.HasTile) continue;
			ushort type = tile.TileType;
			if (type >= TileID.Sets.Ore.Length || !TileID.Sets.Ore[type]) continue;

			counts.TryGetValue(type, out int c);
			counts[type] = c + 1;

			float dx = (x * 16f + 8f) - center.X;
			float dy = (y * 16f + 8f) - center.Y;
			float sq = dx * dx + dy * dy;
			if (!nearestSq.TryGetValue(type, out float best) || sq < best)
			{
				nearestSq[type] = sq;
				nearest[type] = new Point(x, y);
			}
		}

		var hits = new List<OreHit>(counts.Count);
		foreach (var (type, count) in counts)
		{
			catalog.TryGetValue(type, out var info);
			string name = !string.IsNullOrEmpty(info.Name)
				? info.Name
				: (info.Icon > 0 ? Lang.GetItemNameValue(info.Icon) : "Ore");
			Point near = nearest[type];
			float dist = (float)System.Math.Sqrt(nearestSq[type]);
			hits.Add(new OreHit(type, info.Icon, name, count, near, dist));
		}

		hits.Sort((a, b) => a.NearestDistPx.CompareTo(b.NearestDistPx));
		return hits;
	}

	public static bool IsOreTile(int x, int y)
	{
		if (x < 1 || y < 1 || x >= Main.maxTilesX - 1 || y >= Main.maxTilesY - 1) return false;
		Tile tile = Main.tile[x, y];
		if (!tile.HasTile) return false;
		ushort type = tile.TileType;
		return type < TileID.Sets.Ore.Length && TileID.Sets.Ore[type];
	}
}
