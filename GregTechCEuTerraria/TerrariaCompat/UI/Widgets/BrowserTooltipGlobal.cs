#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class BrowserTooltipGlobal : GlobalItem
{
	public static readonly Color Header = new(130, 230, 130);
	public static readonly Color Detail = new(170, 180, 200);
	public static readonly Color Member = new(150, 200, 150);
	public static readonly Color Source = new(255, 220, 140);
	public static readonly Color Chance = new(255, 220, 120);

	private static bool _pending;
	private static bool _replace;
	private static readonly List<(string text, Color color, bool afterName)> _lines = new();

	public static void Begin(bool replace = false)
	{
		_pending = true;
		_replace = replace;
		_lines.Clear();
	}

	public static void AfterName(string text, Color color) => _lines.Add((text, color, true));

	public static void Append(string text, Color color) => _lines.Add((text, color, false));

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (!_pending) return;
		_pending = false;
		if (_replace) tooltips.Clear();

		int nameIdx = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "ItemName");
		int insertAt = nameIdx < 0 ? 0 : nameIdx + 1;

		int id = 0;
		foreach (var (text, color, afterName) in _lines)
		{
			var line = new TooltipLine(Mod, "GTBrowser" + id++, text) { OverrideColor = color };
			if (afterName && !_replace)
				tooltips.Insert(insertAt++, line);
			else
				tooltips.Add(line);
		}
	}
}
