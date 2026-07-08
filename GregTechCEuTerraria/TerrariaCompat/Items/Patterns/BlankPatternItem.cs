#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Patterns;

public sealed class BlankPatternItem : ModItem, ITextureWarmUp
{
	public override string Texture =>
		"GregTechCEuTerraria/Content/TerrariaCompat/blank_pattern";

	public override void SetStaticDefaults() =>
		Terraria.Localization.Language.GetOrRegister(
			$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => "Blank Pattern");

	public override void SetDefaults()
	{
		Item.maxStack = Item.CommonMaxStack;
		Item.width = 32;
		Item.height = 32;
		Item.rare = ItemRarityID.LightPurple;
	}

	public override void HoldItem(Player player) => EnsureBaked();
	void ITextureWarmUp.WarmUpTexture() => EnsureBaked();

	private void EnsureBaked()
	{
		if (Main.dedServ) return;
		ItemIconBaker.Install(Item.type, new IconLayer(Texture, Color.White, 1f));
	}
}
