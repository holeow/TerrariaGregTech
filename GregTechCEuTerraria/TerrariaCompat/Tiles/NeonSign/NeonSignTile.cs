#nullable enable
using GregTechCEuTerraria.TerrariaCompat.UI.NeonSign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.NeonSign;

public sealed class NeonSignTile : ModTile
{
	public override string Name => "neon_sign";
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/NeonSignTile";

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileSolid[Type]          = false;
		Main.tileSolidTop[Type]       = false;
		Main.tileNoAttach[Type]       = false;
		Main.tileBlockLight[Type]     = false;
		Main.tileLavaDeath[Type]      = false;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.LavaDeath    = false;
		TileObjectData.newTile.AnchorBottom = default(AnchorData);
		TileObjectData.newTile.HookPostPlaceMyPlayer =
			ModContent.GetInstance<NeonSignEntity>().Generic_HookPostPlaceMyPlayer;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(120, 255, 255),
			Language.GetOrRegister($"Mods.GregTechCEuTerraria.Tiles.{Name}.MapEntry",
				() => "Neon Sign"));

		DustType  = DustID.Glass;
		HitSound  = SoundID.Tink;
		MineResist = 1f;
		MinPick    = 0;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (fail || effectOnly) return;
		ModContent.GetInstance<NeonSignEntity>().Kill(i, j);
	}

	public override bool HasSmartInteract(int i, int j,
		Terraria.GameContent.ObjectInteractions.SmartInteractScanSettings settings) => true;

	public override bool RightClick(int i, int j)
	{
		if (Main.dedServ) return false;
		var sign = NeonSignEntity.At(i, j);
		if (sign is null) return false;
		NeonSignUISystem.OpenFor(sign);
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Main.LocalPlayer.cursorItemIconEnabled = false;
		Main.LocalPlayer.noThrow = 2;
		Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<Items.NeonSignItem>();
		Main.LocalPlayer.cursorItemIconText = "";
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var sign = NeonSignEntity.At(i, j);
		if (sign is not null)
			NeonSignRenderer.Draw(spriteBatch, i, j, sign);
	}
}
