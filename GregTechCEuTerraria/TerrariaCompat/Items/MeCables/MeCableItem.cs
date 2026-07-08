#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Util;
using GregTechCEuTerraria.TerrariaCompat.Items.Pipes;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;
using GregTechCEuTerraria.TerrariaCompat.UI;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.MeCables;

public sealed class MeCableItem : ModItem, ITextureWarmUp
{
	private readonly string? _id;
	private readonly AEColor _color;
	private int _removeCooldown;

	public MeCableItem() { }

	public MeCableItem(AEColor color)
	{
		_id = $"me_cable_{color.RegistryPrefix()}";
		_color = color;
	}

	public AEColor Color => _color;

	public override string Name => _id ?? nameof(MeCableItem);

	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/me_cable";
	protected override bool CloneNewInstances => true;
	public override bool IsLoadingEnabled(Mod mod) => _id != null;

	public override void SetStaticDefaults()
	{
		if (_id is null) return;
		Terraria.Localization.Language.GetOrRegister(
			$"Mods.GregTechCEuTerraria.Items.{_id}.DisplayName",
			() => $"ME {_color.EnglishName()} Cable");
	}

	public override void SetDefaults()
	{
		Item.maxStack = Item.CommonMaxStack;
		Item.width = 32;
		Item.height = 32;
		Item.useTime = 2;
		Item.useAnimation = 6;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.autoReuse = true;
		Item.consumable = false;
		Item.rare = ItemRarityID.LightPurple;
		Item.UseSound = null;
	}

	public override bool? UseItem(Player player)
	{
		if (_id is null) return null;
		if (Main.myPlayer != player.whoAmI) return null;

		int x = (int)(Main.MouseWorld.X / 16);
		int y = (int)(Main.MouseWorld.Y / 16);
		if (x <= 0 || x >= Main.maxTilesX - 1 || y <= 0 || y >= Main.maxTilesY - 1) return false;
		if (Item.stack <= 0) return false;

		if (!MeCableLayerHandle.Instance.TryPlace(new MeCableCell(_color), x, y, player))
			return false;
		Terraria.Audio.SoundEngine.PlaySound(SoundID.Item50, new Microsoft.Xna.Framework.Vector2(x * 16, y * 16));
		Item.stack--;
		return true;
	}

	public override void ModifyTooltips(System.Collections.Generic.List<Terraria.ModLoader.TooltipLine> tooltips)
	{
		if (_id is null) return;
		Tools.GregTechMultitool.AppendHint(Mod, tooltips);
	}

	public override void HoldItem(Player player)
	{
		EnsureTextureBaked();
		if (Main.myPlayer != player.whoAmI || _id is null) return;
		PipeHeldItemBehavior.Tick(player, "ME Cable", MeCableLayerHandle.Instance,
			MeCableBody, ref _removeCooldown, Item.useTime);
	}

	void ITextureWarmUp.WarmUpTexture() => EnsureTextureBaked();

	private void EnsureTextureBaked() =>
		ItemIconBaker.Install(Item.type,
			new IconLayer(Texture, MeCableRenderer.FromHex(_color.MediumVariant()), 1f));

	private static string? MeCableBody(int x, int y)
	{
		var cell = MeCableLayerSystem.Cables.CellAt(x, y);
		if (cell is null) return null;

		var net = MeNetworkSystem.NetAt(x, y);
		string cellLine = $"ME Cable: {cell.Value.Color.EnglishName()}";
		string netLine = net != null
			? $"Network: {net.Cells.Count} cables   {net.Devices.Count} devices"
			: "Network: not initialized";
		return string.Join("\n", cellLine, netLine);
	}

	public static bool CutAt(Player player, int x, int y) =>
		MeCableLayerHandle.Instance.CutAt(x, y, player);
}
