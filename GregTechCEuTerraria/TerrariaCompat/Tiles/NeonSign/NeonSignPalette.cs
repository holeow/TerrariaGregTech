#nullable enable
using System;
using Microsoft.Xna.Framework;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.NeonSign;

public static class NeonSignPalette
{
	public static readonly (string Name, Color Color)[] Colors =
	{
		("White",  new Color(255, 255, 255)),
		("Red",    new Color(255,  80,  80)),
		("Orange", new Color(255, 150,  40)),
		("Yellow", new Color(255, 230,  90)),
		("Green",  new Color(120, 255, 120)),
		("Cyan",   new Color(120, 255, 255)),
		("Blue",   new Color(110, 150, 255)),
		("Purple", new Color(190, 120, 255)),
		("Pink",   new Color(255, 140, 220)),
		("Gray",   new Color(170, 170, 180)),
		("Gold",   new Color(255, 200,  60)),
		("Lime",   new Color(190, 255,  80)),
	};

	public static int Count => Colors.Length;

	public static Color ColorFor(int index) => Colors[Math.Clamp(index, 0, Colors.Length - 1)].Color;
}
