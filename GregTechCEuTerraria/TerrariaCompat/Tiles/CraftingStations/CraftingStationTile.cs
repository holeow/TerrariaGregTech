#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.CraftingStations;

public sealed class CraftingStationTile : ModTile, ITextureWarmUp
{
	private const int FrameStride = 3 * 18; // 54

	private readonly string? _id;
	private readonly string? _displayName;
	private readonly string[]? _adjKeys;
	private bool _aliased;
	private int _overlayItem = -1;

	public CraftingStationTile() { }
	public CraftingStationTile(string id, string displayName, string[]? adjKeys = null)
	{
		_id = id;
		_displayName = displayName;
		_adjKeys = adjKeys;
	}

	public override bool IsLoadingEnabled(Mod mod) => _id != null;
	public override string Name => _id ?? nameof(CraftingStationTile);
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsTile";

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileSolid[Type]          = false;
		Main.tileNoAttach[Type]       = true;
		Main.tileLavaDeath[Type]      = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.addTile(Type);

		if (_adjKeys is { Length: > 0 })
		{
			var adj = new System.Collections.Generic.List<int>(_adjKeys.Length);
			foreach (var key in _adjKeys)
				if (CraftingStationRegistry.TryGetTile(key, out int at)) adj.Add(at);
			AdjTiles = adj.ToArray();
		}
		else
		{
			AdjTiles = new int[] { Type };
		}

		AddMapEntry(new Color(150, 120, 90),
			Language.GetOrRegister($"Mods.GregTechCEuTerraria.Tiles.{Name}.MapEntry",
				() => _displayName ?? Name));

		DustType   = DustID.Iron;
		HitSound   = SoundID.Tink;
		MineResist = 1.5f;
		MinPick    = 0;
	}

	public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
	{
		Tile t = Main.tile[i, j];
		var origin = new Point16(i - t.TileFrameX / 18, j - t.TileFrameY / 18);
		int frame = CraftingStationAnim.FrameFor(origin);
		if (frame > 0) frameYOffset += frame * FrameStride;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		WarmUpTexture();
		return true;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (_id is null || Main.dedServ) return;
		Tile t = Main.tile[i, j];
		if (t.TileFrameX != 36 || t.TileFrameY != 36) return;

		if (_overlayItem < 0)
			_overlayItem = CraftingStationRegistry.TryGetOverlayTool(_id, out int it) ? it : 0;
		if (_overlayItem == 0) return;

		Items.Tools.ToolItemLoader.EnsureBaked(_overlayItem);
		var img = TextureAssets.Item[_overlayItem].Value;
		if (img is null) return;

		Vector2 zero = Main.drawToScreen ? Vector2.Zero
		                                 : new Vector2(Main.offScreenRange, Main.offScreenRange);

		Vector2 center = new Vector2(i * 16 - 8, j * 16 + 8) - Main.screenPosition + zero;
		var src = Main.itemAnimations[_overlayItem]?.GetFrame(img) ?? img.Frame();
		spriteBatch.Draw(img, center, src, Lighting.GetColor(i, j), 0f,
			src.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
	}

	public void WarmUpTexture()
	{
		if (_id is null || _aliased || Main.dedServ) return;
		if (Type < TextureAssets.Tile.Length && TileID.Autohammer < TextureAssets.Tile.Length)
		{
			Main.instance.LoadTiles(TileID.Autohammer);
			TextureAssets.Tile[Type] = TextureAssets.Tile[TileID.Autohammer];
			_aliased = true;
		}
	}
}
