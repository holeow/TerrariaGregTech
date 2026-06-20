#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Items;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Bosses.FallenEBF;

// Summons the Fallen EBF
public class OverburnedCoilBlock : ModItem, ITextureWarmUp
{
	private const string CoilBase  = "GregTechCEuTerraria/Content/Textures/block/casings/coils/machine_coil_cupronickel";
	private const string CoilBloom = "GregTechCEuTerraria/Content/Textures/block/casings/coils/machine_coil_cupronickel_bloom";

	public override string Texture => CoilBase;

	public override void SetStaticDefaults()
	{
		Language.GetOrRegister("Mods.GregTechCEuTerraria.Items.OverburnedCoilBlock.DisplayName", () => "Overburned Coil Block");
		Language.GetOrRegister("Mods.GregTechCEuTerraria.Items.OverburnedCoilBlock.Tooltip",
			() => "A cupronickel coil block stoked past its limit\nUse on the surface to summon the Fallen EBF");
		ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
	}

	void ITextureWarmUp.WarmUpTexture() => ItemIconBaker.Install(Item.type,
		new IconLayer(CoilBase, new Color(255, 160, 110)),
		new IconLayer(CoilBloom, Color.White),
		new IconLayer(CoilBloom, new Color(255, 120, 40)));

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 28;
		Item.maxStack = 20;
		Item.rare = ItemRarityID.Orange;
		Item.useAnimation = 45;
		Item.useTime = 45;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.UseSound = SoundID.Roar;
		Item.consumable = true;
		Item.value = Item.sellPrice(silver: 80);
	}

	public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<FallenEBF>());

	public override bool? UseItem(Player player)
	{
		if (player.whoAmI == Main.myPlayer)
		{
			int type = ModContent.NPCType<FallenEBF>();
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NPC.SpawnOnPlayer(player.whoAmI, type);
			else
				NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
		}
		return true;
	}

	public override void AddRecipes()
	{
		if (!Mod.TryFind<ModItem>("cupronickel_coil_block", out var coil))
		{
			Mod.Logger.Warn("[FallenEBF] OverburnedCoilBlock recipe skipped: cupronickel_coil_block not found.");
			return;
		}

		CreateRecipe()
			.AddIngredient(coil.Type, 1)
			.AddTile(TileID.Furnaces)
			.DisableDecraft()
			.Register();
	}
}
