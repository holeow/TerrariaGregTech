#nullable enable
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Terminal;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Terminal;

public static class MeTerminalUpgradeLoader
{
	public static void Register(Mod mod)
	{
		MeTerminalUpgrades.Clear();

		Add(mod, new MeTerminalUpgrade("crafting", "me_crafting_card", "Crafting Terminal Card"));
		Add(mod, new MeTerminalUpgrade("crafting_status", "me_crafting_status_card", "Crafting Status Terminal Card"));
		Add(mod, new MeTerminalUpgrade("pattern_access", "me_pattern_access_card", "Pattern Access Terminal Card"));
		Add(mod, new MeTerminalUpgrade("pattern_encoding", "me_pattern_encoding_card", "Pattern Encoding Terminal Card"));
	}

	private static void Add(Mod mod, MeTerminalUpgrade upgrade)
	{
		MeTerminalUpgrades.Register(upgrade);
		mod.AddContent(new MeTerminalUpgradeCardItem(upgrade));
	}
}
