#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class DockRegions
{
	public const int BottomReserve = 64;
	public const int Pad = 8;
	public const int Margin = 20;
	public const float RecipesWidthFraction = 0.5f;

	private static IEnumerable<Rectangle> Gapped(IEnumerable<Rectangle> windows)
	{
		foreach (var w in windows)
			yield return new Rectangle(w.X - Margin, w.Y - Margin, w.Width + Margin * 2, w.Height + Margin * 2);
	}

	public static Rectangle Recipes(IEnumerable<Rectangle> windows)
	{
		var screen = ScreenLayout.Screen;
		var within = new Rectangle(
			screen.X + Margin, screen.Y + Margin,
			screen.Width - Margin * 2, screen.Height - Margin - BottomReserve);
		var gap = ScreenLayout.FreeRectAnchored(ScreenEdge.Right, within);
		int maxW = (int)(gap.Width * RecipesWidthFraction);
		var stripWithin = new Rectangle(gap.Right - maxW, gap.Y, maxW, gap.Height);
		return ScreenLayout.FreeRectAnchored(ScreenEdge.Right, stripWithin, Gapped(windows));
	}

	public static Rectangle Favorites(IEnumerable<Rectangle> windows)
	{
		var screen = ScreenLayout.Screen;
		int right = VanillaHudOccupancy.InventoryRegion.Right;

		int favLeft = screen.X + Margin;
		void LeftMargin(IEnumerable<Rectangle> obs)
		{
			foreach (var o in obs)
				if (o.Left < screen.Width * 0.25f && o.Right < screen.Width * 0.5f
					&& o.Bottom > screen.Height * 0.55f)
					favLeft = Math.Max(favLeft, o.Right);
		}
		LeftMargin(ScreenLayout.Occupied);
		LeftMargin(windows);

		int favTop = screen.Y;
		foreach (var o in ScreenLayout.Occupied)
			if (o.Right > favLeft && o.Left < right && o.Top < screen.Height * 0.4f)
				favTop = Math.Max(favTop, o.Bottom);
		favTop += Pad;

		int l = favLeft, t = favTop, r = right, b = screen.Bottom - BottomReserve;
		foreach (var w in Gapped(windows))
		{
			if (w.Right <= l || w.Left >= r || w.Bottom <= t || w.Top >= b) continue;
			int hOverlap = Math.Min(w.Right, r) - Math.Max(w.Left, l);
			int vOverlap = Math.Min(w.Bottom, b) - Math.Max(w.Top, t);
			if (hOverlap >= vOverlap) t = Math.Max(t, Math.Min(w.Bottom, b));
			else                      r = Math.Min(r, Math.Max(w.Left, l));
		}
		return new Rectangle(l, t, Math.Max(0, r - l), Math.Max(0, b - t));
	}

	public static Rectangle SettingsBand(Rectangle strip, int height)
		=> new(strip.X, strip.Y, strip.Width, height);

	public static Rectangle RecipesBody(Rectangle strip, int settingsHeight)
		=> new(strip.X, strip.Y + settingsHeight + Pad, strip.Width,
			Math.Max(60, strip.Height - settingsHeight - Pad));
}
