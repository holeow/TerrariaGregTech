#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools.Multitool;

public static class MultitoolState
{
	public static string ActiveLayerId = "cable";
	public static bool Cutting;

	private static readonly Dictionary<string, string?> _armedByLayer = new();
	private static readonly Dictionary<string, int> _widthByLayer = new();

	public static string? ArmedVariantKey
	{
		get => _armedByLayer.TryGetValue(ActiveLayerId, out var k) ? k : null;
		set => _armedByLayer[ActiveLayerId] = value;
	}

	public static int Width
	{
		get => _widthByLayer.TryGetValue(ActiveLayerId, out var w) ? w : 0;
		set => _widthByLayer[ActiveLayerId] = value;
	}

	public static int WidthFor(string layerId) => _widthByLayer.TryGetValue(layerId, out var w) ? w : 0;

	public static string? ArmedFor(string layerId) => _armedByLayer.TryGetValue(layerId, out var k) ? k : null;

	public static bool RadialOpen;
	public static Vector2 RadialAnchor;

	public static bool IsHeld(Player player) =>
		player.HeldItem is { IsAir: false } it && it.type == ModContent.ItemType<GregTechMultitool>();

	public const int MaxPathTiles = 2000;

	public static List<Point> LPath(Point a, Point b, int direction)
	{
		Point corner = direction == 1 ? new Point(a.X, b.Y) : new Point(b.X, a.Y);
		var seen = new HashSet<Point>();
		var outp = new List<Point>();

		void Add(Point p) { if (seen.Add(p)) outp.Add(p); }
		void Seg(Point s, Point e)
		{
			int dx = Math.Sign(e.X - s.X), dy = Math.Sign(e.Y - s.Y);
			Point c = s;
			Add(c);
			while (c != e && outp.Count < MaxPathTiles)
			{
				c = new Point(c.X + dx, c.Y + dy);
				Add(c);
			}
		}

		Seg(a, corner);
		Seg(corner, b);
		return outp;
	}
}
