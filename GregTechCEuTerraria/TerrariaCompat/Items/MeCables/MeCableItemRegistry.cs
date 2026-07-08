#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Util;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.MeCables;

public static class MeCableItemRegistry
{
	private static readonly Dictionary<AEColor, MeCableItem> _items = new();

	public static int Count => _items.Count;

	public static int? Get(AEColor color) => _items.TryGetValue(color, out var it) ? it.Type : null;

	public static void Register(Mod mod)
	{
		_items.Clear();
		foreach (var color in AllColors())
		{
			var item = new MeCableItem(color);
			mod.AddContent(item);
			_items[color] = item;
		}
	}

	private static IEnumerable<AEColor> AllColors()
	{
		foreach (var c in AEColors.VALID_COLORS) yield return c;
		yield return AEColor.TRANSPARENT;
	}

	public static void Unload() => _items.Clear();
}
