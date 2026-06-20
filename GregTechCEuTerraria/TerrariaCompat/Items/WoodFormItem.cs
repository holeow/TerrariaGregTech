#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Items.Registry;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class WoodFormItem : ModItem, ITextureWarmUp
{
	private const string TextureRoot = "GregTechCEuTerraria/Content/Textures/";
	private const string PlatePrimary = "item/material_sets/wood/plate";
	private const string PlateSecondary = "item/material_sets/wood/plate_secondary";

	private readonly string? _id;
	private readonly string? _label;
	private readonly int _rarity;
	private readonly bool _isLog;
	private readonly string? _woodColorTexture;

	public WoodFormItem() { }

	public WoodFormItem(string id, string label, int rarity, bool isLog, string woodColorTexture)
	{
		_id = id;
		_label = label;
		_rarity = rarity;
		_isLog = isLog;
		_woodColorTexture = woodColorTexture;
	}

	public override bool IsLoadingEnabled(Mod mod) => _id != null;
	public override string Name => _id ?? nameof(WoodFormItem);
	protected override bool CloneNewInstances => true;

	public override string Texture => _isLog
		? "Terraria/Images/Item_" + ItemID.Wood
		: TextureRoot + PlatePrimary;

	public override void SetStaticDefaults()
	{
		if (_id != null && !string.IsNullOrEmpty(_label))
			Language.GetOrRegister(
				$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => _label!);
	}

	public override void SetDefaults()
	{
		Item.maxStack = 9999;
		if (_isLog && ContentSamples.ItemsByType.TryGetValue(ItemID.Wood, out var wood))
		{
			Item.width = wood.width;
			Item.height = wood.height;
		}
		else
		{
			Item.width = 32;
			Item.height = 32;
		}
		Item.value = Terraria.Item.buyPrice(silver: 1);
		Item.rare = _rarity;
	}

	public override void HoldItem(Player player)
	{
		base.HoldItem(player);
		EnsureTextureBaked();
	}

	void ITextureWarmUp.WarmUpTexture() => EnsureTextureBaked();

	private void EnsureTextureBaked()
	{
		Color wood = ItemIconBaker.AverageColor(TextureRoot + _woodColorTexture);
		if (_isLog)
		{
			ItemIconBaker.InstallGreyscaleTintedFromVanilla(Item.type, ItemID.Wood, wood, upscale: false);
			return;
		}
		ItemIconBaker.Install(Item.type, new[]
		{
			new IconLayer(TextureRoot + PlatePrimary, wood),
			new IconLayer(TextureRoot + PlateSecondary, Darken(wood, 0.6f)),
		});
	}

	private static Color Darken(Color c, float f) =>
		new((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f), c.A);
}
