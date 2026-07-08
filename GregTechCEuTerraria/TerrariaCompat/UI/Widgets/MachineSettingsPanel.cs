#nullable enable
using System;
using System.Collections.Generic;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class MachineSettingsPanel : UIElement
{
	public const int RowHeight = 22;
	private const int Gap = 4;

	public MachineSettingsPanel(int width, IReadOnlyList<UIElement> rows)
	{
		Width = StyleDimension.FromPixels(width);
		int y = 0;
		foreach (var row in rows)
		{
			row.Left = StyleDimension.FromPixels(0);
			row.Top  = StyleDimension.FromPixels(y);
			Append(row);
			int h = (int)row.Height.Pixels;
			y += (h > 0 ? h : RowHeight) + Gap;
		}
		Height = StyleDimension.FromPixels(Math.Max(0, y - Gap));
	}

	public int PixelHeight => (int)Height.Pixels;

	public static UITextButton Toggle(int width, Func<string> label, Action onLeft, string tooltip,
		Func<bool>? isActive = null, Action? onRight = null)
		=> new(label, onLeft, onRight, tooltip, width, RowHeight) { IsActive = isActive };
}
