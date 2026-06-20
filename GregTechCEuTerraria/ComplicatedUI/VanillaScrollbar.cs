#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class VanillaScrollbar
{
	public const int Width = 20;
	private const int Cap = 6;

	private static Asset<Texture2D>? _bar;
	private static Asset<Texture2D>? _inner;

	public static void Draw(SpriteBatch sb, Rectangle track, Rectangle thumb, bool thumbHot)
	{
		if (Main.dedServ) return;
		_bar   ??= Main.Assets.Request<Texture2D>("Images/UI/Scrollbar", AssetRequestMode.ImmediateLoad);
		_inner ??= Main.Assets.Request<Texture2D>("Images/UI/ScrollbarInner", AssetRequestMode.ImmediateLoad);

		DrawBar(sb, _bar.Value, track, Color.White);
		DrawBar(sb, _inner.Value, thumb, Color.White * (thumbHot ? 1f : 0.85f));
	}

	private static void DrawBar(SpriteBatch sb, Texture2D tex, Rectangle r, Color color)
	{
		int w = tex.Width;
		sb.Draw(tex, new Rectangle(r.X, r.Y, r.Width, Cap),
			new Rectangle(0, 0, w, Cap), color);
		sb.Draw(tex, new Rectangle(r.X, r.Y + Cap, r.Width, Math.Max(0, r.Height - Cap * 2)),
			new Rectangle(0, Cap, w, 4), color);
		sb.Draw(tex, new Rectangle(r.X, r.Bottom - Cap, r.Width, Cap),
			new Rectangle(0, tex.Height - Cap, w, Cap), color);
	}
}
