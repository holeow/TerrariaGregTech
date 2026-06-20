#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using GregTechCEuTerraria.TerrariaCompat.Tiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class GlobalRecipeBrowserSystem : ModalUISystem
{
	private static GlobalRecipeBrowserSystem? _instance;
	private GlobalRecipeBrowserState? _state;

	private const string LayerNameStr = "GregTechCEuTerraria: GlobalRecipeBrowser";
	protected override string LayerName => LayerNameStr;
	public override bool PinSupported => true;

	public override void Load()
	{
		_instance = this;
		base.Load();
		if (!Main.dedServ)
		{
			_state = new GlobalRecipeBrowserState();
			_state.Host = this;
			_state.Activate();
		}
	}

	public override void Unload()
	{
		_instance = null;
		_state = null;
		base.Unload();
	}

	public override void OnWorldLoad()
	{
		// Pre-bake search-text cache + loot registry
		if (!Main.dedServ)
		{
			GlobalRecipeBrowserState.SyncLocaleCaches();
			RecipeSearch.WarmCache();
			_state?.Warm();
			Loot.LootRegistry.Warm();
		}
	}

	protected override void OnClose()
	{
		_state?.SaveQueryForReopen();
		Widgets.UISearchBar.UnfocusAll();
	}

	protected override void AddExtraLayers(List<GameInterfaceLayer> layers)
		=> UILayers.InsertButton(layers,
			"GregTechCEuTerraria: TooManyItems Button",
			() => { DrawInventoryButton(); return true; });

	public static void Open()
	{
		var s = _instance;
		if (s?.Ui is null || s._state is null) return;
		if (s.Ui.CurrentState == s._state) return;
		s._state.RebuildFromScratch();
		s.Ui.SetState(s._state);
		s.PushModal();
		Main.playerInventory = true;
	}

	// R/U hover hotkeys
	public static void OpenFiltered(int itemType, GlobalRecipeBrowserState.BrowseFilter filter)
	{
		if (Show() is { } s) s._state!.ApplyItemFilter(itemType, filter);
	}

	public static void OpenFilteredFluid(string fluidId, string label,
		GlobalRecipeBrowserState.BrowseFilter filter)
	{
		if (Show() is { } s) s._state!.ApplyFluidFilter(fluidId, label, filter);
	}

	public static void OpenFilteredTag(string tagLabel, HashSet<int> items,
		GlobalRecipeBrowserState.BrowseFilter filter)
	{
		if (Show() is { } s) s._state!.ApplyTagFilter(tagLabel, items, filter);
	}

	private static GlobalRecipeBrowserSystem? Show()
	{
		var s = _instance;
		if (s?.Ui is null || s._state is null) return null;
		if (s.Ui.CurrentState != s._state) s.Ui.SetState(s._state);
		s.PushModal();
		Main.playerInventory = true;
		return s;
	}

	public static void Close() => _instance?.CloseInternal();

	public static void Toggle()
	{
		if (IsOpen)
			Close();
		else
			Open();
	}

	public static bool IsOpen => _instance?.IsOpenInternal ?? false;

	public static bool CursorOverHigherModal => UILayers.IsCursorOverHigherModal(LayerNameStr);

	private static void DrawInventoryButton()
		=> UILayers.DrawStackedButton(
			slot: 0,
			background: new Color(38, 42, 70),
			drawIcon: (sb, r) =>
			{
				var plate = TooManyItemsArt.PlateTexture;
				if (plate != null) sb.Draw(plate, r, Color.White);
			},
			tooltip: "Open Too Many Items",
			onClick: Open,
			keybind: ModalToggleKeybinds.OpenRecipeBrowser);
}
