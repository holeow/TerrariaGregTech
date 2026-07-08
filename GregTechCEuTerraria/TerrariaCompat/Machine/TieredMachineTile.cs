#nullable enable
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using GregTechCEuTerraria.TerrariaCompat.UI;
using GregTechCEuTerraria.TerrariaCompat.UI.Layouts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace GregTechCEuTerraria.TerrariaCompat.Machine;

public class TieredMachineTile : MetaMachineTile
{
	protected readonly VoltageTier _tier;
	protected readonly MachineDefinition? _def;
	private readonly bool _isTemplate;

	public TieredMachineTile() { _isTemplate = true; _tier = VoltageTier.LV; }
	public TieredMachineTile(VoltageTier tier, MachineDefinition def)
	{
		_tier = tier;
		_def = def;
		_isTemplate = false;
	}

	public override MachineDefinition Definition => _def!;

	public override bool IsLoadingEnabled(Mod mod) => !_isTemplate;
	public override string Name => _def == null
		? GetType().Name
		: _def.Tiered ? $"{VoltageTiers.Id(_tier)}_{_def.Id}" : _def.Id;
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsTile";

	protected virtual Color MapColor => new Color(140, 130, 110);

	public override void SetStaticDefaults()
	{
		ApplyDefaults();
		TileObjectData.addTile(Type);

		// BindDefinition's tile-type backstop.
		MachineRegistry.MapTile(Type, _def!.Id, _tier);

		AddMapEntry(MapColor,
			Language.GetOrRegister($"Mods.GregTechCEuTerraria.Tiles.{Name}.MapEntry",
				() => _def!.Tiered ? $"{VoltageTiers.ShortName(_tier)} {_def.Label}" : _def.Label));

		DustType   = MineDustType;
		HitSound   = Terraria.ID.SoundID.Tink;
		MineResist = 1.5f;
	}

	protected virtual int MineDustType => Terraria.ID.DustID.Stone;

	public override MachineRenderer.Casing CasingKind => MapCasing(_def!.Casing);
	public override string OverlayDir              => _def!.ResolveOverlayDirForTier(_tier);
	public override string OverlayBasename         => _def!.OverlayBasename;
	public override string PipeOverlayBasename     => _def!.PipeOverlayBasename;
	public override string TintedOverlayBasename   => _def!.TintedOverlayBasename;
	public override string EmissiveOverlayBasename => _def!.EmissiveOverlayBasename;
	public override bool   AnimateIdleOverlay      => _def!.AnimateIdleOverlay;
	public override string? CustomFaceAssetPath => _def!.CustomFaceAssetPath;
	protected override VoltageTier TileTier => _tier;

	private static Microsoft.Xna.Framework.Graphics.Texture2D? _gtArrow;

	public override void PostDraw(int i, int j, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
	{
		base.PostDraw(i, j, spriteBatch);

		if (!MachineCellResolver.TryFindMachineAt(i, j, out var m)) return;
		if (m.AutoOutput is not { } ao) return;
		if (!ao.IsAutoOutputItems && !ao.IsAutoOutputFluids) return;

		var (w, h) = m.Size;
		int originX = m.Position.X, originY = m.Position.Y;
		if (i != originX + w - 1 || j != originY + h - 1) return;

		_gtArrow ??= ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>(
			"GregTechCEuTerraria/Content/TerrariaCompat/gt_direction",
			ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

		var light = Lighting.GetColor(originX, originY);
		if (ao.IsAutoOutputItems)
			DirectionArrowOverlay.Draw(spriteBatch, _gtArrow, originX, originY, w, h, ao.ItemOutputDirection, light);
		if (ao.IsAutoOutputFluids)
			DirectionArrowOverlay.Draw(spriteBatch, _gtArrow, originX, originY, w, h, ao.FluidOutputDirection, light);
	}

	public override string? CustomCasingTexturePath
	{
		get
		{
			var name = _def?.FusedCasingTileName;
			if (string.IsNullOrEmpty(name)) return null;
			var mod = Terraria.ModLoader.ModLoader.GetMod("GregTechCEuTerraria");
			if (!mod.TryFind<Terraria.ModLoader.ModTile>(name, out var tile)) return null;
			return Terraria.ModLoader.TileLoader.GetTile(tile.Type) is Tiles.Casings.CasingTile casing
				? casing.BlockTexture : null;
		}
	}

	internal static MachineRenderer.Casing MapCasing(MachineCasing c) => c switch
	{
		MachineCasing.Voltage       => MachineRenderer.Casing.Voltage,
		MachineCasing.BrickedBronze => MachineRenderer.Casing.BrickedBronze,
		MachineCasing.BrickedSteel  => MachineRenderer.Casing.BrickedSteel,
		MachineCasing.CokeBricks    => MachineRenderer.Casing.CokeBricks,
		MachineCasing.Firebricks    => MachineRenderer.Casing.Firebricks,
		MachineCasing.PumpDeck      => MachineRenderer.Casing.PumpDeck,
		_                           => MachineRenderer.Casing.None,
	};

	public override bool RightClick(int i, int j)
	{
		if (MachineCellResolver.TryFindMachineAt(i, j, out var clicked)
		    && Items.TerminalItem.TryAutoBuild(clicked, Main.LocalPlayer))
			return true;

		if (clicked is Pipelike.LongDistance.LongDistanceEndpointMachine ep
		    && IsScrewdriver(Main.LocalPlayer.HeldItem))
		{
			if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
				Net.LdEndpointTogglePacket.SendRequest(ep.Position.X, ep.Position.Y);
			else
				Net.LdEndpointTogglePacket.Apply(ep);
			return true;
		}

		if (_def!.LayoutKey == "none") return false;
		if (!MachineCellResolver.TryFindMachineAt(i, j, out var machine)) return false;
		var layout = MachineLayoutRegistry.Build(machine);
		if (layout is null) return false;
		MachineUISystem.OpenFor(machine, layout);
		return true;
	}

	private static bool IsScrewdriver(Item item)
	{
		if (item?.ModItem is not Items.Tools.ToolItem tool) return false;
		string n = tool.ToolType.Name;
		return n == "screwdriver" || n.EndsWith("_screwdriver");
	}
}
