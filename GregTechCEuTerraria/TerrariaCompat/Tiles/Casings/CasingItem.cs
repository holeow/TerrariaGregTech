#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.Casings;

public sealed class CasingItem : ModItem, ITextureWarmUp
{
	private readonly string? _id;
	private readonly string? _texture;
	private readonly string? _displayName;
	private readonly int _maxStack;
	private readonly int _rarity;

	public CasingItem() { }
	public CasingItem(string id, string texture, string displayName, int maxStack, int rarity)
	{
		_id = id;
		_texture = texture;
		_displayName = displayName;
		_maxStack = maxStack;
		_rarity = rarity;
	}

	public override bool IsLoadingEnabled(Mod mod) => _id != null;
	public override string Name => _id ?? nameof(CasingItem);
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsItem";
	protected override bool CloneNewInstances => true;

	public override void SetStaticDefaults()
	{
		if (_id != null && !string.IsNullOrEmpty(_displayName))
			Language.GetOrRegister(
				$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => _displayName!);
	}

	public override void SetDefaults()
	{
		if (_id is null) return;
		Item.DefaultToPlaceableTile(Mod.Find<ModTile>(Name).Type);
		Item.useTime = Players.CenteredPlacementPlayer.PlaceUseTime;
		Item.useAnimation = Players.CenteredPlacementPlayer.PlaceUseAnimation;
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = 9999;
		Item.rare = _rarity;
	}

	public void WarmUpTexture()
	{
		if (_texture is not null)
			CasingRenderer.EnsureItemTexture(Type, _texture);
	}
}
