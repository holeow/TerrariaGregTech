#nullable enable
namespace GregTechCEuTerraria.TerrariaCompat.UI.Terminal;

public static class MeModularTerminalMode
{
	public const string Terminal = "terminal";
	public const string CraftingStatus = "crafting_status";
	public const string PatternAccess = "pattern_access";

	public static string Current { get; private set; } = Terminal;

	public static void Set(string mode) => Current = mode;

	public static bool Is(string mode) => Current == mode;
}
