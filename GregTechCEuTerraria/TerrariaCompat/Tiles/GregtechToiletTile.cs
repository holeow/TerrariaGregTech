#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles;

public sealed class GregtechToiletTile : ModTile, ITextureWarmUp
{
	public override string Name => "gregtech_toilet";
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsTile";

	public const int NextStyleHeight = 40;

	public void WarmUpTexture() => GregtechToiletArt.InstallTile(Type);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type]       = true;
		Main.tileLavaDeath[Type]      = false;
		Main.tileSolid[Type]          = false;
		Main.tileSolidTop[Type]       = false;
		Main.tileBlockLight[Type]     = false;
		Main.tileLighted[Type]        = true;

		TileID.Sets.CanBeSatOnForPlayers[Type]   = true;
		TileID.Sets.CanBeSatOnForNPCs[Type]      = true;
		TileID.Sets.DisableSmartCursor[Type]     = true;
		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
		AdjTiles = new int[] { TileID.Toilets };

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.CoordinateHeights = new[] { 16, 18 };
		TileObjectData.newTile.CoordinatePaddingFix = new Point16(0, 2);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.LavaDeath = false;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(150, 150, 155), Language.GetOrRegister(
			$"Mods.GregTechCEuTerraria.Tiles.{Name}.MapEntry", () => "Gregtech Toilet"));

		DustType = DustID.Stone;
		HitSound = SoundID.Tink;
		MineResist = 1.5f;
		MinPick = 0;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		WarmUpTexture();

		Tile tile = Main.tile[i, j];
		if (tile.TileFrameY != 0) return false;

		var sheet = TextureAssets.Tile[Type]?.Value;
		if (sheet is null) return false;

		int dir = tile.TileFrameX;
		DrawCell(spriteBatch, sheet, i, j, new Rectangle(dir, 0, 16, 16));
		DrawCell(spriteBatch, sheet, i, j + 1, new Rectangle(dir, 16 + 2, 16, 18));
		return false;
	}

	private static void DrawCell(SpriteBatch sb, Texture2D sheet, int cx, int cy, Rectangle src)
	{
		Vector2 zero = Main.drawToScreen ? Vector2.Zero
		                                 : new Vector2(Main.offScreenRange, Main.offScreenRange);
		Vector2 pos = new Vector2(cx * 16 - (int)Main.screenPosition.X,
		                          cy * 16 - (int)Main.screenPosition.Y) + zero;
		Color light = Lighting.GetColor(cx, cy);
		sb.Draw(sheet, pos, src, light, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		r = 1f;
		g = 1f;
		b = 1f;
	}

	public override void PlaceInWorld(int i, int j, Item item) => GregtechToiletAura.OnPlaced(i, j);

	public override void KillMultiTile(int i, int j, int frameX, int frameY) => GregtechToiletAura.OnRemoved(i, j);

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
		=> settings.player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance);

	public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		info.TargetDirection = tile.TileFrameX != 0 ? 1 : -1;
		info.AnchorTilePosition.X = i;
		info.AnchorTilePosition.Y = j;
		if (tile.TileFrameY % NextStyleHeight == 0)
			info.AnchorTilePosition.Y++;
		info.ExtraInfo.IsAToilet = true;
	}

	public override bool RightClick(int i, int j)
	{
		Player player = Main.LocalPlayer;
		if (player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance))
		{
			player.GamepadEnableGrappleCooldown();
			player.sitting.SitDown(player, i, j);
		}
		return true;
	}

	public override void HitWire(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		int spawnX = i;
		int spawnY = j - (tile.TileFrameY % NextStyleHeight) / 18;
		Wiring.SkipWire(spawnX, spawnY);
		Wiring.SkipWire(spawnX, spawnY + 1);
		if (Wiring.CheckMech(spawnX, spawnY, 60))
			Projectile.NewProjectile(Wiring.GetProjectileSource(spawnX, spawnY),
				spawnX * 16 + 8, spawnY * 16 + 12, 0f, 0f, ProjectileID.ToiletEffect, 0, 0f, Main.myPlayer);
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		if (!player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance))
			return;

		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<Items.GregtechToiletItem>();
		if (Main.tile[i, j].TileFrameX / 18 < 1)
			player.cursorItemIconReversed = true;
	}
}
