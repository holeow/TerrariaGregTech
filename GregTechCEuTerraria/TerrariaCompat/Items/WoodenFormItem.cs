#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class WoodenFormItem : ModItem, ITextureWarmUp
{
	private static readonly Color WarnRed = new(255, 70, 70);

	private readonly string? _id;
	private readonly string? _label;
	private readonly int _rarity;

	public WoodenFormItem() { }
	public WoodenFormItem(string id, string label, int rarity)
	{
		_id = id;
		_label = label;
		_rarity = rarity;
	}

	public override bool IsLoadingEnabled(Mod mod) => _id != null;
	public override string Name => _id ?? nameof(WoodenFormItem);

	protected override bool CloneNewInstances => true;

	public override string Texture => $"GregTechCEuTerraria/Content/Textures/item/{Name}";

	public override void SetStaticDefaults()
	{
		if (_label != null)
			Language.GetOrRegister(
				$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => _label);

		if (Main.dedServ) return;

		var tex = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
		if (tex.Width > 0 && tex.Height > tex.Width && tex.Height % tex.Width == 0)
		{
			int frames = tex.Height / tex.Width;
			Main.RegisterItemAnimation(Type, new DrawAnimationVertical(
				MachineRenderer.AnimationTicksPerFrame, frames));
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

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		base.ModifyTooltips(tooltips);
		tooltips.Add(new TooltipLine(Mod, "FormConsumed",
			"CONSUMED when used in a craft!") { OverrideColor = WarnRed });
		tooltips.Add(new TooltipLine(Mod, "FormFreeAlt",
			"A worse recipe that needs no form is also available") { OverrideColor = WarnRed });
	}

	public override void HoldItem(Player player)
	{
		base.HoldItem(player);
		ItemIconBaker.Install(Item.type, Texture);
	}

	void ITextureWarmUp.WarmUpTexture() => ItemIconBaker.Install(Item.type, Texture);
}
