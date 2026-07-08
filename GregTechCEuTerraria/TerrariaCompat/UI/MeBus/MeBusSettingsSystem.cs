#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.MeBus;

public sealed class MeBusSettingsSystem : ModalUISystem
{
	private MeBusSettingsState? _state;

	private const string LayerNameStr = "GregTechCEuTerraria: ME Bus Settings";
	protected override string LayerName => LayerNameStr;
	protected override bool CloseOnEscape => false;

	public override void Load()
	{
		base.Load();
		if (!Main.dedServ)
		{
			_state = new MeBusSettingsState();
			_state.Activate();
		}
	}

	public override void Unload()
	{
		_state = null;
		base.Unload();
	}

	public static bool IsOpen
		=> ModContent.GetInstance<MeBusSettingsSystem>()?.IsOpenInternal ?? false;

	public static void OpenFor(int x, int y)
	{
		var sys = ModContent.GetInstance<MeBusSettingsSystem>();
		if (sys?.Ui is null || sys._state is null) return;
		ModUIRegistry.OnOpen(Close);
		sys._state.Bind(x, y);
		sys.Ui.SetState(sys._state);
		sys.PushModal();
		Main.playerInventory = true;
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close() => ModContent.GetInstance<MeBusSettingsSystem>()?.CloseInternal();

	public static bool IsOpenable(int x, int y)
	{
		if (x <= 0 || x >= Main.maxTilesX - 1 || y <= 0 || y >= Main.maxTilesY - 1) return false;
		if (!MeCableLayerSystem.Cables.Has(x, y)) return false;
		return HasAnyInventory(x, y) || HasAnyAttachment(x, y);
	}

	protected override void OnClose()
	{
		_state?.Unbind();
		ModUIRegistry.OnClose(Close);
		SoundEngine.PlaySound(SoundID.MenuClose);
	}

	protected override bool ShouldAutoClose()
	{
		if (_state is null) return false;
		if (!MeCableLayerSystem.Cables.Has(_state.CellX, _state.CellY)) return true;
		return !Main.LocalPlayer.IsInTileInteractionRange(
			_state.CellX, _state.CellY, TileReachCheckSettings.Simple);
	}

	private static int _hoverX, _hoverY;
	private static bool _hoverShow;

	private static bool HasAnyInventory(int x, int y)
	{
		foreach (var (side, dx, dy) in Api.Capability.IODirectionExtensions.Cardinal4)
			if (Capabilities.WorldCapability.HasInventoryAt(
				x + dx, y + dy, Api.Capability.IODirectionExtensions.Opposite(side)))
				return true;
		return false;
	}

	private static bool HasAnyAttachment(int x, int y)
	{
		foreach (var (side, _, _) in Api.Capability.IODirectionExtensions.Cardinal4)
			if (MeBusLayerSystem.Buses.Get(x, y, side) != null)
				return true;
		return false;
	}

	private static bool IsLayerAffectingItem(Item item)
	{
		if (item.IsAir) return false;
		if (item.ModItem is Items.MeCables.MeCableItem) return true;
		if (item.ModItem is Items.Tools.ToolItem tool && tool.IsWireCutter) return true;
		return false;
	}

	public override void PostUpdateInput()
	{
		_hoverShow = false;
		if (Main.dedServ) return;
		base.PostUpdateInput();

		var p = Main.LocalPlayer;
		if (p is null) return;
		if (IsLayerAffectingItem(p.HeldItem))
		{
			if (Main.mouseRight)
			{
				p.controlUseTile = false;
				p.releaseUseTile = false;
			}
			return;
		}

		if (UILayers.IsCursorOverAnyModal()) return;

		WorldInteract.WorldCursor.RawCell(out int rawX, out int rawY);

		int x, y;
		if (MeCableLayerSystem.Cables.Has(rawX, rawY)) { x = rawX; y = rawY; }
		else if (MeCableLayerSystem.Cables.Has(Player.tileTargetX, Player.tileTargetY))
		{ x = Player.tileTargetX; y = Player.tileTargetY; }
		else return;

		if (!p.IsInTileInteractionRange(x, y, TileReachCheckSettings.Simple)) return;

		if (!HasAnyInventory(x, y) && !HasAnyAttachment(x, y)) return;

		bool sameAsBound = IsOpen && _state is not null && _state.CellX == x && _state.CellY == y;
		if (!sameAsBound) { _hoverX = x; _hoverY = y; _hoverShow = true; }
	}

	public override void UpdateUI(GameTime gameTime)
	{
		base.UpdateUI(gameTime);

		if (_hoverShow)
		{
			WorldHoverTooltip.Set("RMB: configure ME buses");
			WorldHoverTooltip.SetHighlight(_hoverX, _hoverY, new Color(120, 220, 255, 200));
		}
	}
}
