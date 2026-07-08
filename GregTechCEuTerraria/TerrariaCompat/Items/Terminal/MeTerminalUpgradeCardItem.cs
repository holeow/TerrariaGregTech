#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Terminal;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Terminal;

public sealed class MeTerminalUpgradeCardItem : ModItem, ITextureWarmUp
{
	private const string PlaceholderTexture = "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsItem";

	private readonly MeTerminalUpgrade? _upgrade;

	public MeTerminalUpgradeCardItem() { }
	public MeTerminalUpgradeCardItem(MeTerminalUpgrade upgrade) { _upgrade = upgrade; }

	public override string Name => _upgrade?.CardItemName ?? "me_terminal_upgrade_card";
	public override string Texture => _upgrade != null
		? $"GregTechCEuTerraria/Content/TerrariaCompat/me_terminal_upgrade_card_{_upgrade.Id}"
		: PlaceholderTexture;

	protected override bool CloneNewInstances => true;

	public override bool IsLoadingEnabled(Mod mod) => _upgrade != null;

	public override void SetStaticDefaults()
	{
		if (_upgrade == null) return;
		Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => _upgrade.DisplayName);
		MeTerminalUpgrades.BindItemType(_upgrade.Id, Type);
	}

	public override void SetDefaults()
	{
		Item.maxStack = 99;
		Item.width = 32;
		Item.height = 32;
		Item.rare = ItemRarityID.LightPurple;
		Item.value = Terraria.Item.buyPrice(gold: 1);
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		tooltips.Add(new TooltipLine(Mod, "MeTerminalUpgrade", "Install in an ME Terminal upgrade slot"));
	}

	public override void HoldItem(Player player)
	{
		base.HoldItem(player);
		ItemIconBaker.Install(Item.type, Texture);
	}

	void ITextureWarmUp.WarmUpTexture() => ItemIconBaker.Install(Item.type, Texture);
}
