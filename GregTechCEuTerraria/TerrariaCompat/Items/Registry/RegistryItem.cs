#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Registry;

public sealed class RegistryItem : ModItem, ITextureWarmUp
{
	private readonly string? _id;
	private readonly string? _label;
	private readonly int _maxStack;
	private readonly int _rarity;
	private readonly string? _texturePath;

	public RegistryItem() { }
	public RegistryItem(string id, string label, int maxStack, int rarity, string? texturePath = null)
	{
		_id = id;
		_label = label;
		_maxStack = maxStack;
		_rarity = rarity;
		_texturePath = texturePath;
	}

	public override bool IsLoadingEnabled(Mod mod) => _id != null;
	public override string Name => _id ?? nameof(RegistryItem);

	protected override bool CloneNewInstances => true;

	public override string Texture =>
		_texturePath ?? $"GregTechCEuTerraria/Content/Textures/item/{Name}";

	public override void SetStaticDefaults()
	{
		if (_label != null)
			Terraria.Localization.Language.GetOrRegister(
				$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => _label);

		if (Main.dedServ) return;

		var tex = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
		if (tex.Width > 0 && tex.Height > tex.Width && tex.Height % tex.Width == 0)
		{
			int frames = tex.Height / tex.Width;
			Main.RegisterItemAnimation(Type, new DrawAnimationVertical(
				Machine.Rendering.MachineRenderer.AnimationTicksPerFrame, frames));
		}
	}

	public override void SetDefaults()
	{
		Item.maxStack = 9999;
		Item.width = 32;
		Item.height = 32;
		Item.value = Terraria.Item.buyPrice(silver: 2);
		Item.rare = _rarity;
	}

	public override void HoldItem(Player player)
	{
		base.HoldItem(player);
		ItemIconBaker.Install(Item.type, Texture);
	}

	void ITextureWarmUp.WarmUpTexture() => ItemIconBaker.Install(Item.type, Texture);
}
