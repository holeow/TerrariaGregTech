#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;

public static class DirectionArrowOverlay
{
	private static readonly Dictionary<Texture2D, Texture2D> _upscaled = new();

	public static float Rotation(IODirection dir) => dir switch
	{
		IODirection.Down  => 0f,
		IODirection.Left  => MathHelper.PiOver2,
		IODirection.Up    => MathHelper.Pi,
		IODirection.Right => MathHelper.Pi + MathHelper.PiOver2,
		_                 => 0f,
	};

	public static void Draw(SpriteBatch sb, Texture2D tex, int originX, int originY,
		int w, int h, IODirection dir, Color color)
	{
		if (dir == IODirection.None) return;

		var art = Upscaled2x(tex);
		Vector2 zero = Main.drawToScreen ? Vector2.Zero
			: new Vector2(Main.offScreenRange, Main.offScreenRange);
		Vector2 center = new Vector2(originX * 16 - (int)Main.screenPosition.X,
		                             originY * 16 - (int)Main.screenPosition.Y) + zero
		               + new Vector2(w * 8f, h * 8f);
		sb.Draw(art, center, null, color, Rotation(dir),
			new Vector2(art.Width / 2f, art.Height / 2f), 1f, SpriteEffects.None, 0f);
	}

	private static Texture2D Upscaled2x(Texture2D src)
	{
		if (_upscaled.TryGetValue(src, out var cached)) return cached;

		int sw = src.Width, sh = src.Height;
		var srcData = new Color[sw * sh];
		src.GetData(srcData);

		int dw = sw * 2, dh = sh * 2;
		var dstData = new Color[dw * dh];
		for (int y = 0; y < sh; y++)
			for (int x = 0; x < sw; x++)
			{
				var c = srcData[y * sw + x];
				int dx = x * 2, dy = y * 2;
				dstData[dy * dw + dx] = c;
				dstData[dy * dw + dx + 1] = c;
				dstData[(dy + 1) * dw + dx] = c;
				dstData[(dy + 1) * dw + dx + 1] = c;
			}

		var art = RuntimeTextureRegistry.New(dw, dh);
		art.SetData(dstData);
		_upscaled[src] = art;
		return art;
	}
}
