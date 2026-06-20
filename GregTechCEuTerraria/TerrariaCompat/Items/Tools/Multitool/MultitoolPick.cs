#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools.Multitool;

internal static class MultitoolPick
{
	public static bool TryPickUnderCursor(Player player, int x, int y)
	{
		var active = MultitoolLayers.Active;
		if (active.TryPick(x, y, out var key, out var width))
		{
			Apply(active, key, width, x, y);
			return true;
		}
		foreach (var layer in MultitoolLayers.All)
		{
			if (layer == active) continue;
			if (layer.TryPick(x, y, out key, out width))
			{
				Apply(layer, key, width, x, y);
				return true;
			}
		}
		return false;
	}

	private static void Apply(MultitoolLayer layer, string key, int width, int x, int y)
	{
		MultitoolState.Cutting = false;
		MultitoolState.ActiveLayerId = layer.Id;
		MultitoolState.ArmedVariantKey = key;
		if (width > 0)
		{
			foreach (var w in layer.WidthOptions)
				if (w == width) { MultitoolState.Width = width; break; }
		}
		SoundEngine.PlaySound(SoundID.Grab, new Vector2(x * 16f, y * 16f));
	}

	public static string HotkeyHint()
	{
		var named = new List<string>();
		var kb = MultitoolSystem.Eyedropper;
		if (kb != null)
		{
			try
			{
				foreach (var k in kb.GetAssignedKeys(InputMode.Keyboard))
					if (k != "None") named.Add(k);
			}
			catch { }
		}
		return named.Count > 0
			? "Eyedropper: " + string.Join(", ", named)
			: "Eyedropper: NOT SET (change in Controls)";
	}
}
