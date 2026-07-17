#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public static class TerminalCraftableView
{
	public enum Mode { DontShow, ShowCraftable, ShowAll, ShowAllGregtech }

	public static Mode Current { get; private set; } = Mode.ShowCraftable;

	public static void Set(Mode mode) => Current = mode;

	public static string Label(Mode mode) => mode switch
	{
		Mode.DontShow => "None",
		Mode.ShowCraftable => "Craftable",
		Mode.ShowAll => "All",
		_ => "All + GT",
	};
}

public static class TerminalSearchPersist
{
	public static bool KeepOnClose;
	public static string Saved = "";
}

public static class TerminalSortPersist
{
	public static SortOrder SortBy = SortOrder.NAME;
	public static SortDir SortDir = SortDir.ASCENDING;
	public static ViewItems ViewMode = ViewItems.ALL;
}
