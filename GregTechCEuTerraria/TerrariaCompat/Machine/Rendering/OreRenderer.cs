#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;

public static class OreRenderer
{
	private static Texture2D? _ironSheet;
	private static Texture2D? _gemMaskSheet;
	private static bool _ironTried, _gemTried;

	private const int GemSaturationThreshold = 35;

	public static Texture2D? GetSheet(bool isGem) => isGem ? GemMaskSheet() : IronSheet();

	private static Texture2D? IronSheet()
	{
		if (_ironTried) return _ironSheet;
		_ironTried = true;
		_ironSheet = BakeGreyscaleSheet(TileID.Iron, gemMaskOnly: false);
		return _ironSheet;
	}

	private static Texture2D? GemMaskSheet()
	{
		if (_gemTried) return _gemMaskSheet;
		_gemTried = true;
		_gemMaskSheet = BakeGreyscaleSheet(TileID.Sapphire, gemMaskOnly: true);
		return _gemMaskSheet;
	}

	private static Texture2D? BakeGreyscaleSheet(int tileId, bool gemMaskOnly)
	{
		if (Main.dedServ) return null;
		Main.instance.LoadTiles(tileId);
		var asset = TextureAssets.Tile[tileId];
		if (asset?.Value is not { } src || src.Width <= 0 || src.Height <= 0) return null;

		int n = src.Width * src.Height;
		var px = new Color[n];
		src.GetData(px);
		for (int k = 0; k < n; k++)
		{
			var c = px[k];
			if (c.A == 0) { px[k] = default; continue; }
			int max = Math.Max(c.R, Math.Max(c.G, c.B));
			int min = Math.Min(c.R, Math.Min(c.G, c.B));
			if (gemMaskOnly && max - min < GemSaturationThreshold)
			{
				px[k] = default;
				continue;
			}
			byte v = (byte)max;
			px[k] = new Color(v, v, v, c.A);
		}

		var tex = RuntimeTextureRegistry.New(src.Width, src.Height);
		tex.SetData(px);
		return tex;
	}

	public static Color MultiplyRGB(Color a, Color b) =>
		new(a.R * b.R / 255, a.G * b.G / 255, a.B * b.B / 255, (byte)255);
}
