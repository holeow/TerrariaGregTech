#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;

internal static class GregtechToiletArt
{
	private const int SrcTile = TileID.Toilets;
	private const int FrameX = 0;
	private const int FrameTopY = 1560;
	private const int DirStride = 18;
	private const int W = 16;
	private const int TopH = 16;
	private const int Pad = 2;
	private const int BottomH = 18;

	private static bool _tileDone, _itemDone;

	public static void InstallTile(int tileType)
	{
		if (Main.dedServ || _tileDone) return;
		if (!TryReadSheet(out var src, out int sw, out int sh)) return;

		int sheetW = DirStride * 2;
		int sheetH = TopH + Pad + BottomH;
		var sheet = new Color[sheetW * sheetH];

		for (int d = 0; d < 2; d++)
		{
			int srcX = FrameX + d * DirStride;
			int dstX = d * DirStride;
			var top = Greyscale(srcX, FrameTopY, W, TopH, src, sw, sh);
			var bot = Greyscale(srcX, FrameTopY + TopH + Pad, W, BottomH, src, sw, sh);
			Blit(top, W, TopH, sheet, sheetW, dstX, 0);
			Blit(bot, W, BottomH, sheet, sheetW, dstX, TopH + Pad);
		}

		var tex = RuntimeTextureRegistry.New(sheetW, sheetH);
		tex.SetData(sheet);
		TextureAssets.Tile[tileType] = MachineRenderer.WrapAsset(tex, $"gtceu_toilet_tile_{tileType}");
		_tileDone = true;
	}

	public static void InstallItem(int itemType)
	{
		if (Main.dedServ || _itemDone) return;
		if (!TryReadSheet(out var src, out int sw, out int sh)) return;

		var top = Greyscale(FrameX, FrameTopY, W, TopH, src, sw, sh);
		var bot = Greyscale(FrameX, FrameTopY + TopH + Pad, W, BottomH, src, sw, sh);

		int iconH = TopH + BottomH;
		var icon = new Color[W * iconH];
		Blit(top, W, TopH, icon, W, 0, 0);
		Blit(bot, W, BottomH, icon, W, 0, TopH);

		var up = Upscale2x(icon, W, iconH);
		var tex = RuntimeTextureRegistry.New(W * 2, iconH * 2);
		tex.SetData(up);
		TextureAssets.Item[itemType] = MachineRenderer.WrapAsset(tex, $"gtceu_toilet_item_{itemType}");
		_itemDone = true;
	}

	private static bool TryReadSheet(out Color[] src, out int w, out int h)
	{
		src = Array.Empty<Color>(); w = 0; h = 0;
		Main.instance.LoadTiles(SrcTile);
		var asset = TextureAssets.Tile[SrcTile];
		if (asset?.Value is not { } tex || tex.Width <= 0 || tex.Height <= 0) return false;
		w = tex.Width; h = tex.Height;
		src = new Color[w * h];
		tex.GetData(src);
		return true;
	}

	private static Color[] Greyscale(int srcX, int srcY, int w, int h, Color[] src, int srcW, int srcH)
	{
		var px = new Color[w * h];
		for (int y = 0; y < h; y++)
		for (int x = 0; x < w; x++)
		{
			int sx = srcX + x, sy = srcY + y;
			Color c = (sx >= 0 && sy >= 0 && sx < srcW && sy < srcH) ? src[sy * srcW + sx] : default;
			if (c.A == 0) { px[y * w + x] = default; continue; }
			byte v = (byte)Math.Max(c.R, Math.Max(c.G, c.B));
			px[y * w + x] = new Color(v, v, v, c.A);
		}
		return px;
	}

	private static void Blit(Color[] srcPx, int sw, int sh, Color[] dst, int dw, int ox, int oy)
	{
		for (int y = 0; y < sh; y++)
		for (int x = 0; x < sw; x++)
			dst[(oy + y) * dw + (ox + x)] = srcPx[y * sw + x];
	}

	private static Color[] Upscale2x(Color[] px, int w, int h)
	{
		var up = new Color[w * 2 * h * 2];
		for (int y = 0; y < h * 2; y++)
		for (int x = 0; x < w * 2; x++)
			up[y * (w * 2) + x] = px[(y / 2) * w + (x / 2)];
		return up;
	}
}
