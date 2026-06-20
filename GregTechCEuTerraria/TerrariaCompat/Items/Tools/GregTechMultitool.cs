#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools;

public sealed class GregTechMultitool : ModItem
{
	private const string AnyWrench     = "GregTechCEuTerraria:gtceu:tools/crafting_wrenches";
	private const string AnyWireCutter = "GregTechCEuTerraria:gtceu:tools/crafting_wire_cutters";
	private const string AnyScrewdriver = "GregTechCEuTerraria:gtceu:tools/crafting_screwdrivers";

	public override string Texture => "Terraria/Images/Item_3611"; // Grand Design

	public override void SetStaticDefaults()
	{
		Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName",
			() => "Greg Multitool");
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 32;
		Item.maxStack = 1;
		Item.rare = ItemRarityID.LightPurple;
		Item.value = Item.sellPrice(gold: 2);
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = Item.useAnimation = 12;
		Item.autoReuse = false;
		Item.noMelee = true;
		Item.UseSound = null;
	}

	public override bool? UseItem(Player player) => null;

	public override void AddRecipes()
	{
		var r = CreateRecipe();
		r.AddRecipeGroup(AnyWrench, 1);
		r.AddRecipeGroup(AnyWireCutter, 1);
		r.AddRecipeGroup(AnyScrewdriver, 1);
		r.AddTile(TileID.WorkBenches);
		r.DisableDecraft();
		r.Register();
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		tooltips.Add(new TooltipLine(Mod, "MultitoolControls1",
			"[c/A0E0FF:Right-click]: open tool menu"));
		tooltips.Add(new TooltipLine(Mod, "MultitoolControls2",
			"[c/A0E0FF:Left-drag]: place wire/pipe"));
		tooltips.Add(new TooltipLine(Mod, "MultitoolWidth",
			"Synthesizes any width from single wires"));
	}

	public static void AppendHint(Mod mod, List<TooltipLine> tooltips)
	{
		tooltips.Add(new TooltipLine(mod, "MultitoolHint",
			"Try to use Greg Multitool for better cable management, its really convenient!")
		{ OverrideColor = new Color(0xA0, 0xE0, 0xFF) });
	}
}
