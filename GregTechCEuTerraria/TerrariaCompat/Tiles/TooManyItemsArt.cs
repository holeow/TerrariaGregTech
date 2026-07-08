#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles;

internal static class TooManyItemsArt
{
	private const string CasingPath = "GregTechCEuTerraria/Content/Textures/block/casings/gcym/atomic_casing";
	private const string OrbPath    = "GregTechCEuTerraria/Content/Textures/item/data_orb";

	private static Color[]? _art;
	private static Texture2D? _plateTex;

	public static Texture2D? PlateTexture
	{
		get
		{
			if (_plateTex != null) return _plateTex;
			var art = Build32();
			if (art is null) return null;
			_plateTex = RuntimeTextureRegistry.New(32, 32);
			_plateTex.SetData(art);
			return _plateTex;
		}
	}

	private static Color[]? Build32()
	{
		if (_art != null) return _art;

		Texture2D casing, orb;
		try
		{
			casing = ModContent.Request<Texture2D>(CasingPath, AssetRequestMode.ImmediateLoad).Value;
			orb    = ModContent.Request<Texture2D>(OrbPath,    AssetRequestMode.ImmediateLoad).Value;
		}
		catch { return null; }

		var casingPx = new Color[casing.Width * casing.Height];
		casing.GetData(casingPx);
		var art = new Color[32 * 32];
		for (int y = 0; y < 32; y++)
			for (int x = 0; x < 32; x++)
			{
				Color c = casingPx[(y / 2) * casing.Width + (x / 2)];
				art[y * 32 + x] = new Color(c.R, c.G, c.B, (byte)255);
			}

		var orbPx = new Color[orb.Width * orb.Height];
		orb.GetData(orbPx);
		const int orbSize = 16, off = (32 - orbSize) / 2;
		for (int y = 0; y < orbSize; y++)
			for (int x = 0; x < orbSize; x++)
			{
				Color src = orbPx[y * orb.Width + x];
				if (src.A == 0) continue;
				float sa = src.A / 255f, ia = 1f - sa;
				int di = (off + y) * 32 + (off + x);
				Color dst = art[di];
				art[di] = new Color(
					(byte)(src.R * sa + dst.R * ia),
					(byte)(src.G * sa + dst.G * ia),
					(byte)(src.B * sa + dst.B * ia),
					(byte)255);
			}

		_art = art;
		return art;
	}
}
