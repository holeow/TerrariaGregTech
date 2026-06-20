#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Covers;

public sealed class CoverItem : ModItem, ITextureWarmUp
{
	private readonly string? _id;
	private readonly string? _label;
	private readonly int _maxStack;
	private readonly int _rarity;
	private readonly string? _coverId;

	public CoverItem() { }
	public CoverItem(string id, string label, int maxStack, int rarity, string coverId)
	{
		_id = id;
		_label = label;
		_maxStack = maxStack;
		_rarity = rarity;
		_coverId = coverId;
	}

	public string CoverId => _coverId ?? "";

	public override bool IsLoadingEnabled(Mod mod) => _id != null;
	public override string Name => _id ?? nameof(CoverItem);
	public override string Texture => $"GregTechCEuTerraria/Content/Textures/item/{Name}";

	protected override bool CloneNewInstances => true;

	public override void SetStaticDefaults()
	{
		if (_label != null)
		{
			string display = _label.Contains("cover", System.StringComparison.OrdinalIgnoreCase)
				? _label
				: _label + " Cover";
			Terraria.Localization.Language.GetOrRegister(
				$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => display);
		}

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
		Item.maxStack = 99;
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

	private static readonly HashSet<string> InertCoverIds = new()
	{
		"computer_monitor", "wireless_transmitter",
	};

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		base.ModifyTooltips(tooltips);
		if (_coverId != null && InertCoverIds.Contains(_coverId))
			tooltips.Add(new TooltipLine(Mod, "InertCover",
				"Does nothing as a cover - crafting ingredient only")
			{ OverrideColor = new Color(170, 170, 180) });
	}
}
