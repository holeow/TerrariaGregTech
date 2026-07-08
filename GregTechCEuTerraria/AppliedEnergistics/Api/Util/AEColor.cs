// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.util.AEColor), Forge 1.20.1. Original MIT header preserved verbatim
// below per AE2's license terms.
//
// The MIT License (MIT)
//
// Copyright (c) 2013 AlgorithmX2
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#nullable enable
using System.Collections.Generic;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Util;

public enum AEColor : byte
{
	WHITE = 0,
	ORANGE,
	MAGENTA,
	LIGHT_BLUE,
	YELLOW,
	LIME,
	PINK,
	GRAY,
	LIGHT_GRAY,
	CYAN,
	PURPLE,
	BLUE,
	BROWN,
	GREEN,
	RED,
	BLACK,
	TRANSPARENT,
}

public static class AEColors
{
	public const int TINTINDEX_DARK = 1;
	public const int TINTINDEX_MEDIUM = 2;
	public const int TINTINDEX_BRIGHT = 3;
	public const int TINTINDEX_MEDIUM_BRIGHT = 4;

	public static readonly IReadOnlyList<AEColor> VALID_COLORS = new[]
	{
		AEColor.WHITE, AEColor.ORANGE, AEColor.MAGENTA, AEColor.LIGHT_BLUE, AEColor.YELLOW,
		AEColor.LIME, AEColor.PINK, AEColor.GRAY, AEColor.LIGHT_GRAY, AEColor.CYAN,
		AEColor.PURPLE, AEColor.BLUE, AEColor.BROWN, AEColor.GREEN, AEColor.RED, AEColor.BLACK,
	};

	private readonly record struct Info(
		string EnglishName, string TranslationKey, string RegistryPrefix,
		int BlackVariant, int MediumVariant, int WhiteVariant, int ContrastTextColor);

	private static readonly Info[] _info =
	{
		new("White",      "gui.ae2.White",      "white",      0xBEBEBE, 0xDBDBDB, 0xFAFAFA, 0x000000),
		new("Orange",     "gui.ae2.Orange",     "orange",     0xF99739, 0xFAAE44, 0xF4DEC3, 0x000000),
		new("Magenta",    "gui.ae2.Magenta",    "magenta",    0x821E82, 0xB82AB8, 0xC598C8, 0x000000),
		new("Light Blue", "gui.ae2.LightBlue",  "light_blue", 0x628DCB, 0x82ACE7, 0xD8F6FF, 0x000000),
		new("Yellow",     "gui.ae2.Yellow",     "yellow",     0xFFF7AA, 0xF8FF4A, 0xFFFFE8, 0x000000),
		new("Lime",       "gui.ae2.Lime",       "lime",       0x7CFF4A, 0xBBFF51, 0xE7F7D7, 0x000000),
		new("Pink",       "gui.ae2.Pink",       "pink",       0xDC8DB5, 0xF8B5D7, 0xF7DEEB, 0x000000),
		new("Gray",       "gui.ae2.Gray",       "gray",       0x7C7C7C, 0xA0A0A0, 0xC9C9C9, 0x000000),
		new("Light Gray", "gui.ae2.LightGray",  "light_gray", 0x9D9D9D, 0xCDCDCD, 0xEFEFEF, 0x000000),
		new("Cyan",       "gui.ae2.Cyan",       "cyan",       0x2F9BA5, 0x51AAC6, 0xAEDDF4, 0x000000),
		new("Purple",     "gui.ae2.Purple",     "purple",     0x8230B2, 0xA453CE, 0xC7A3CC, 0x000000),
		new("Blue",       "gui.ae2.Blue",       "blue",       0x2D29A0, 0x514AFF, 0xDDE6FF, 0x000000),
		new("Brown",      "gui.ae2.Brown",      "brown",      0x724E35, 0xB7967F, 0xE0D2C8, 0x000000),
		new("Green",      "gui.ae2.Green",      "green",      0x45A021, 0x60E32E, 0xE3F2E3, 0x000000),
		new("Red",        "gui.ae2.Red",        "red",        0xA50029, 0xFF003C, 0xFFE6ED, 0x000000),
		new("Black",      "gui.ae2.Black",      "black",      0x2B2B2B, 0x565656, 0x848484, 0xFFFFFF),
		new("Fluix",      "gui.ae2.Fluix",      "fluix",      0x1B2344, 0x895CA8, 0xD7BBEC, 0x000000),
	};

	public static string EnglishName(this AEColor c) => _info[(int)c].EnglishName;
	public static string TranslationKey(this AEColor c) => _info[(int)c].TranslationKey;
	public static string RegistryPrefix(this AEColor c) => _info[(int)c].RegistryPrefix;
	public static int BlackVariant(this AEColor c) => _info[(int)c].BlackVariant;
	public static int MediumVariant(this AEColor c) => _info[(int)c].MediumVariant;
	public static int WhiteVariant(this AEColor c) => _info[(int)c].WhiteVariant;
	public static int ContrastTextColor(this AEColor c) => _info[(int)c].ContrastTextColor;

	public static int GetVariantByTintIndex(this AEColor c, int tintIndex)
	{
		switch (tintIndex)
		{
			case 0:
				return -1;
			case TINTINDEX_DARK:
				return c.BlackVariant();
			case TINTINDEX_MEDIUM:
				return c.MediumVariant();
			case TINTINDEX_BRIGHT:
				return c.WhiteVariant();
			case TINTINDEX_MEDIUM_BRIGHT:
				int light = c.WhiteVariant();
				int dark = c.MediumVariant();
				return (((light >> 16 & 0xff) + (dark >> 16 & 0xff)) / 2 << 16)
					| (((light >> 8 & 0xff) + (dark >> 8 & 0xff)) / 2 << 8)
					| (((light & 0xff) + (dark & 0xff)) / 2);
			default:
				return -1;
		}
	}
}
