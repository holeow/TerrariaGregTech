#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using GregTechCEuTerraria.TerrariaCompat.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class GregtechToiletItem : ModItem, ITextureWarmUp
{
	public override string Name => "gregtech_toilet";
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsItem";

	public void WarmUpTexture() => GregtechToiletArt.InstallItem(Type);

	public override void SetStaticDefaults()
	{
		Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => "Gregtech Toilet");
		Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.Tooltip",
			() => "The Final Overclock.");
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		tooltips.Add(new TooltipLine(Mod, "ToiletRadius",
			$"Radius {GregtechToiletAura.RadiusTiles} tiles"));
	}

	public override void SetDefaults()
	{
		Item.width  = 20;
		Item.height = 34;
		Item.maxStack = 99;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 10;
		Item.useAnimation = 15;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.createTile = ModContent.TileType<GregtechToiletTile>();
		Item.rare = ItemRarityID.Cyan;
		Item.value = Item.sellPrice(gold: 1);
	}

	public override void AddRecipes()
	{
		if (!Mod.TryFind<ModItem>("nan_certificate", out var cert)) return;
		var r = CreateRecipe();
		r.AddIngredient(cert.Type, 1);
		r.AddIngredient(ItemID.TerraToilet, 1);
		r.DisableDecraft();
		r.Register();
	}
}
