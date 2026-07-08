#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class MeTerminalSortBar : UIElement
{
	private readonly UIMeTerminalGrid _grid;

	public MeTerminalSortBar(UIMeTerminalGrid grid)
	{
		_grid = grid;

		const int btnH = 20, gap = 4;
		int y = 0;
		Add(() => $"Sort: {SortLabel(_grid.SortBy)}", _grid.CycleSort,
			"Sort order: Name / Amount / Mod", ref y, btnH, gap);
		Add(() => _grid.Dir == SortDir.ASCENDING ? "Direction: Ascending" : "Direction: Descending", _grid.ToggleDir,
			"Sort direction", ref y, btnH, gap);
		Add(() => $"View: {ViewLabel(_grid.ViewMode)}", _grid.CycleView,
			"View: All / Stored / Craftable", ref y, btnH, gap);
	}

	private void Add(System.Func<string> label, System.Action onLeft, string tooltip, ref int y, int h, int gap)
	{
		Append(new UITextButton(label, onLeft, null, tooltip, width: 999, height: h)
		{
			Left = StyleDimension.FromPixels(0),
			Top = StyleDimension.FromPixels(y),
			Width = StyleDimension.FromPercent(1f),
			Height = StyleDimension.FromPixels(h),
		});
		y += h + gap;
	}

	private static string SortLabel(SortOrder o) => o switch
	{
		SortOrder.NAME => "Name", SortOrder.AMOUNT => "Amount", _ => "Mod",
	};

	private static string ViewLabel(ViewItems v) => v switch
	{
		ViewItems.ALL => "All", ViewItems.STORED => "Stored", _ => "Craftable",
	};
}
