#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Common.Materials;
using GregTechCEuTerraria.Api.Data.Chemical.Material;
using GregTechCEuTerraria.TerrariaCompat.Items;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles;

public sealed class OreTile : ModTile
{
	private readonly Material? _material;

	private static bool _preDrawFallbackLogged;

	public override string Name => _material != null ? $"{_material.Id}_ore" : nameof(OreTile);

	public override string Texture => "Terraria/Images/Tiles_1";

	public override bool IsLoadingEnabled(Mod mod) => _material != null;

	public OreTile() { }

	public OreTile(Material material) { _material = material; }

	public override void SetStaticDefaults()
	{
		if (_material == null) return;

		TileID.Sets.Ore[Type] = true;
		Main.tileSpelunker[Type] = true;
		Main.tileOreFinderPriority[Type] = 410;
		Main.tileShine2[Type] = true;
		Main.tileShine[Type] = 975;
		Main.tileMergeDirt[Type] = true;
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		DustType = DustID.Platinum;
		HitSound = SoundID.Tink;

		uint c = _material.Color ?? 0xAAAAAA;
		var mapColor = new Color((byte)((c >> 16) & 0xFF), (byte)((c >> 8) & 0xFF), (byte)(c & 0xFF));
		string mapEntryKey = $"Mods.GregTechCEuTerraria.Tiles.{_material.Id}_ore.MapEntry";
		string materialNameKey = $"Mods.GregTechCEuTerraria.Materials.{_material.Id}";
		var name = Language.GetOrRegister(mapEntryKey,
			() => $"{Language.GetTextValue(materialNameKey)} Ore");
		AddMapEntry(mapColor, name);
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (_material == null) return;
		bool isGem = _material.Forms.Contains("GEM");
		var sheet = OreRenderer.GetSheet(isGem);
		if (sheet == null)
		{
			if (!_preDrawFallbackLogged)
			{
				_preDrawFallbackLogged = true;
				ModContent.GetInstance<GregTechCEuTerraria>().Logger.Warn(
					$"[OreTile.PostDraw] ore sheet not baked for {_material.Id} (gem={isGem})");
			}
			return;
		}

		Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange, Main.offScreenRange);
		Vector2 pos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero;

		Tile t = Main.tile[i, j];
		int fx = Math.Min(t.TileFrameX, sheet.Width - 16);
		int fy = Math.Min(t.TileFrameY, sheet.Height - 16);
		if (fx < 0 || fy < 0) return;
		var frame = new Rectangle(fx, fy, 16, 16);

		Color light = Lighting.GetColor(i, j);
		var p = Main.LocalPlayer;
		if (p.findTreasure && Main.IsTileSpelunkable(i, j))
		{
			if (light.R < 200) light.R = 200;
			if (light.G < 170) light.G = 170;
		}
		spriteBatch.Draw(sheet, pos, frame, OreRenderer.MultiplyRGB(light, MaterialColor()));
	}

	private Color MaterialColor()
	{
		uint c = _material?.Color ?? 0xAAAAAAu;
		return new Color((byte)((c >> 16) & 0xFF), (byte)((c >> 8) & 0xFF), (byte)(c & 0xFF));
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		if (_material == null) yield break;
		int rawOreType = MaterialItemRegistry.Get(_material.Id, "raw_ore") ?? 0;
		if (rawOreType > 0)
			yield return new Item(rawOreType) { stack = OreTileRegistry.RawOrePerBlock };
	}
}
