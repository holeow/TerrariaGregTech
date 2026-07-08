#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;

public sealed class GregtechToiletGizmoSystem : ModSystem
{
	private static readonly List<Vector2> _centers = new();
	private static uint _lastScan;
	private static bool _scanned;

	public override void OnWorldUnload() => _scanned = false;

	public override void PostDrawTiles()
	{
		if (Main.dedServ) return;
		RescanIfNeeded();
		if (_centers.Count == 0) return;

		var pixel = TextureAssets.MagicPixel.Value;
		if (pixel is null) return;

		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
			Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null,
			Main.GameViewMatrix.TransformationMatrix);

		float radius = GregtechToiletAura.RadiusTiles * 16f;
		float pulse  = 0.55f + 0.25f * (float)Math.Sin(Main.GameUpdateCount * 0.05f);
		Color color  = new Color(120, 235, 255) * pulse;

		foreach (var c in _centers)
			DrawRing(Main.spriteBatch, pixel, c - Main.screenPosition, radius, color);

		Main.spriteBatch.End();
	}

	private static void RescanIfNeeded()
	{
		if (_scanned && Main.GameUpdateCount - _lastScan < 12) return;
		_lastScan = Main.GameUpdateCount;
		_scanned = true;
		_centers.Clear();

		int type = ModContent.TileType<GregtechToiletTile>();
		int pad = GregtechToiletAura.RadiusTiles + 2;
		int x0 = Math.Max(0, (int)(Main.screenPosition.X / 16) - pad);
		int y0 = Math.Max(0, (int)(Main.screenPosition.Y / 16) - pad);
		int x1 = Math.Min(Main.maxTilesX - 1, (int)((Main.screenPosition.X + Main.screenWidth) / 16) + pad);
		int y1 = Math.Min(Main.maxTilesY - 1, (int)((Main.screenPosition.Y + Main.screenHeight) / 16) + pad);

		for (int x = x0; x <= x1; x++)
		for (int y = y0; y <= y1; y++)
		{
			Tile t = Main.tile[x, y];
			if (!t.HasTile || t.TileType != type) continue;
			if (t.TileFrameY != 0) continue;
			_centers.Add(new Vector2(x * 16 + 8, y * 16 + 16));
		}
	}

	private static void DrawRing(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius, Color color)
	{
		const int segments = 256;
		Vector2 prev = center + new Vector2(radius, 0f);
		for (int s = 1; s <= segments; s++)
		{
			float a = MathHelper.TwoPi * s / segments;
			Vector2 next = center + new Vector2((float)Math.Cos(a) * radius, (float)Math.Sin(a) * radius);
			DrawLine(sb, pixel, prev, next, color, 2.5f);
			prev = next;
		}
	}

	private static void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 a, Vector2 b, Color color, float thickness)
	{
		Vector2 d = b - a;
		float len = d.Length();
		if (len <= 0f) return;
		float rot = (float)Math.Atan2(d.Y, d.X);
		sb.Draw(pixel, a, new Rectangle(0, 0, 1, 1), color, rot,
			new Vector2(0f, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0f);
	}
}
