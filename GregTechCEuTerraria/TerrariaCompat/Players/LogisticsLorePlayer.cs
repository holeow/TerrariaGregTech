#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.Players;

public sealed class LogisticsLorePlayer : ModPlayer
{
	private const string GrantedKey = "gtLogisticsLoreGranted";

	private static int _terminalType = -2;

	private bool _granted;

	private int TerminalType()
	{
		if (_terminalType != -2) return _terminalType;
		_terminalType = Mod.TryFind<ModItem>("me_modular_terminal", out var mi) ? mi.Type : -1;
		return _terminalType;
	}

	public override void PostUpdate()
	{
		if (_granted) return;
		if (Main.netMode == NetmodeID.Server) return;
		if (Player.whoAmI != Main.myPlayer) return;

		int termType = TerminalType();
		if (termType <= 0 || !Player.HasItem(termType)) return;

		_granted = true;
		Utils.PlayerGive.Give(Player,
			Player.GetSource_GiftOrReward("GregTechCEuTerraria/LogisticsLore"),
			ModContent.ItemType<HistoryOfLogisticsItem>(), 1);
	}

	public override void SaveData(TagCompound tag)
	{
		if (_granted) tag[GrantedKey] = true;
	}

	public override void LoadData(TagCompound tag)
	{
		_granted = tag.GetBool(GrantedKey);
	}
}
