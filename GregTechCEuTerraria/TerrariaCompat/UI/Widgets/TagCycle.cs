#nullable enable
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

internal static class TagCycle
{
	public const int Period = 48;

	public static int Index(int count) =>
		count <= 0 ? 0 : (int)(Main.GameUpdateCount / Period % (uint)count);
}
