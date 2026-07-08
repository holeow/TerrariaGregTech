#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class SuperStorageSystemBag : ModItem, ITextureWarmUp
{
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsItem";

	public void WarmUpTexture()
	{
		if (ModContent.GetInstance<GregTechCEuTerraria>().TryFind<ModItem>("me_modular_terminal", out var mi))
			StarterBagArt.InstallFor(Item.type, mi.Type);
	}

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 3;
		Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName",
			() => "Super Storage System Startup Bag");
		Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.Tooltip",
			() => "Will give you everything to setup full autocrafting. Normally fully available at around LV age");
	}

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = 99;
		Item.rare = ItemRarityID.Cyan;
		Item.consumable = true;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 20;
		Item.useAnimation = 20;
	}

	public override bool CanRightClick() => true;

	public override void RightClick(Player player)
	{
		var src = new EntitySource_Gift(player, "GregTechCEuTerraria/SuperStorageSystemBag");
		foreach (var (type, count) in Contents())
			global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(player, src, type, count);
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		var contents = Contents();
		if (contents.Count == 0) return;
		tooltips.Add(new TooltipLine(Mod, "BagHeader", "[c/AAEEFF:Contents]"));
		foreach (var (type, count) in contents)
			tooltips.Add(new TooltipLine(Mod, $"BagItem_{type}",
				$"  {count}x {Lang.GetItemName(type).Value}"));
	}

	private static List<(int Type, int Count)>? _contents;

	private static List<(int Type, int Count)> Contents()
	{
		if (_contents != null) return _contents;
		var list = new List<(int, int)>();

		AddByName(list, "me_modular_terminal",     1);
		AddByName(list, "me_crafting_card",         1);
		AddByName(list, "me_crafting_status_card",  1);
		AddByName(list, "me_pattern_access_card",   1);
		AddByName(list, "me_pattern_encoding_card", 1);
		AddByName(list, "quantum_computer",         2);
		AddByName(list, "me_interface",            10);
		AddByName(list, "me_pattern_provider",     30);
		AddByName(list, "me_cable_fluix",         300);

		_contents = list;
		return list;
	}

	private static void AddByName(List<(int, int)> list, string itemName, int stack)
	{
		if (ModContent.GetInstance<GregTechCEuTerraria>().TryFind<ModItem>(itemName, out var mi))
			list.Add((mi.Type, stack));
	}
}
