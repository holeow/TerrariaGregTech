#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using GregTechCEuTerraria.TerrariaCompat.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class SteamAgeSkipBag : ModItem, ITextureWarmUp
{
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsItem";

	public void WarmUpTexture() => StarterBagArt.InstallFor(Item.type, "gtceu:steel_block");

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 3;
		Terraria.Localization.Language.GetOrRegister(
			$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName",
			() => "Starter Bag: Skip Steam Age");
		Terraria.Localization.Language.GetOrRegister(
			$"Mods.GregTechCEuTerraria.Items.{Name}.Tooltip",
			() => "Right-click to open. Gives a minimal LV bootstrap kit");
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
		var src = new EntitySource_Gift(player, "GregTechCEuTerraria/SteamAgeSkipBag");
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

		AddMachine(list, "lv_solar_panel_machine", 4);
		AddMachine(list, "lv_lamp",                4);
		AddMachine(list, "lv_battery_buffer_4x",   1);
		AddMachine(list, "lv_sodium_battery",      1);
		AddResolved(list, "gtceu:lv_machine_hull",        4);
		AddResolved(list, "gtceu:basic_electronic_circuit", 8);
		AddResolved(list, "gtceu:vacuum_tube",           16);
		AddResolved(list, "gtceu:steel_ingot",           32);
		AddResolved(list, "gtceu:steel_plate",           64);
		AddResolved(list, "gtceu:copper_plate",          32);
		AddResolved(list, "gtceu:tin_plate",             32);
		AddResolved(list, "gtceu:iron_rod",              32);
		AddResolved(list, "gtceu:magnetic_iron_rod",     16);
		AddResolved(list, "gtceu:rubber_plate",          64);
		AddResolved(list, "gtceu:rubber_ingot",         250);
		AddResolved(list, "gtceu:tin_single_wire",       32);
		AddResolved(list, "gtceu:copper_single_wire",    16);
		AddMachine(list, "simple_item_pipe",  100);
		AddMachine(list, "simple_fluid_pipe", 100);
		AddMachine(list, "pipe_intersection",  10);

		_contents = list;
		return list;
	}

	private static void AddResolved(List<(int, int)> list, string upstreamId, int stack)
	{
		int type = IngredientResolverImpl.Instance.ResolveItemType(upstreamId);
		if (type > 0) list.Add((type, stack));
	}

	private static void AddMachine(List<(int, int)> list, string machineKey, int stack)
	{
		if (ModContent.GetInstance<GregTechCEuTerraria>().TryFind<ModItem>(machineKey, out var mi))
			list.Add((mi.Type, stack));
	}
}
