#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.BossDrops.MultiblockBag;
using GregTechCEuTerraria.TerrariaCompat.Items.Pipes;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using GregTechCEuTerraria.TerrariaCompat.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class SkipStoneAgeBag : ModItem, ITextureWarmUp
{
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsItem";

	public void WarmUpTexture() => StarterBagArt.InstallFor(Item.type, "gtceu:bronze_block");

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 3;
		Terraria.Localization.Language.GetOrRegister(
			$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName",
			() => "Starter Bag: Skip Stone Age");
		Terraria.Localization.Language.GetOrRegister(
			$"Mods.GregTechCEuTerraria.Items.{Name}.Tooltip",
			() => "Right-click to open. A steam-age auto-mining setup so you can skip manual mining.");
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
		var src = new EntitySource_Gift(player, "GregTechCEuTerraria/SkipStoneAgeBag");
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

		AddMachine(list, "hp_steam_miner",        1);
		AddMachine(list, "hp_steam_solid_boiler", 2);
		AddMachine(list, "bronze_crate",          2);
		AddMachine(list, "hp_steam_furnace",      1);
		AddMachine(list, "hp_steam_macerator",    1);
		AddResolved(list, "gtceu:coke_gem", 100);
		AddResolved(list, "gtceu:sticky_resin", 100);
		AddPipe(list, "steel_tiny_fluid_pipe", 100);
		AddPipe(list, "tin_small_item_pipe",   100);
		AddMachine(list, "pipe_intersection",   10);
		AddBag(list, "primitive_pump", 1);
		AddBag(list, "coke_oven",      1);

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

	private static void AddPipe(List<(int, int)> list, string bareId, int stack)
	{
		if (PipeItemRegistry.Get(bareId) is int type) list.Add((type, stack));
	}

	private static void AddBag(List<(int, int)> list, string multiId, int stack)
	{
		if (MultiblockBagLoader.TryGet(multiId, out int type)) list.Add((type, stack));
	}
}
