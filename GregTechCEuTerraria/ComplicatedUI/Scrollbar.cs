#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class Scrollbar
{
	public const int Width = VanillaScrollbar.Width;
	private const int MinThumb = 28;

	private bool _dragging;
	private int _anchorPx;

	public bool Dragging => _dragging;

	public Rectangle Update(Rectangle track, int maxScroll, float visibleFraction, ref int scroll, Point mouse)
	{
		if (maxScroll <= 0 || track.Height <= 0)
		{
			_dragging = false;
			return Rectangle.Empty;
		}

		scroll = Math.Clamp(scroll, 0, maxScroll);

		int thumbH = Math.Clamp((int)(visibleFraction * track.Height), MinThumb, track.Height);
		int travel = Math.Max(1, track.Height - thumbH);
		int thumbY = track.Y + (int)((float)scroll / maxScroll * travel);
		var thumb = new Rectangle(track.X, thumbY, track.Width, thumbH);

		if (MouseClick.LeftPressed && track.Contains(mouse))
		{
			_dragging = true;
			_anchorPx = thumb.Contains(mouse) ? mouse.Y - thumbY : thumbH / 2;
		}

		if (_dragging && Main.mouseLeft)
		{
			int clamped = Math.Clamp(mouse.Y - _anchorPx - track.Y, 0, travel);
			scroll = (int)Math.Round((float)clamped / travel * maxScroll);
			thumb = new Rectangle(track.X, track.Y + clamped, track.Width, thumbH);
		}
		else if (!Main.mouseLeft)
		{
			_dragging = false;
		}

		return thumb;
	}

	public static void Wheel(Terraria.UI.UIScrollWheelEvent evt, ref int scroll, int unitPx = 1)
	{
		scroll -= (int)Math.Round(evt.ScrollWheelValue / (float)Math.Max(1, unitPx));
		if (scroll < 0) scroll = 0;
	}

	public void Draw(SpriteBatch sb, Rectangle track, Rectangle thumb, Point mouse)
	{
		if (thumb == Rectangle.Empty) return;
		bool hot = _dragging || thumb.Contains(mouse);
		VanillaScrollbar.Draw(sb, track, thumb, hot);
		if (hot) Main.LocalPlayer.mouseInterface = true;
	}
}
