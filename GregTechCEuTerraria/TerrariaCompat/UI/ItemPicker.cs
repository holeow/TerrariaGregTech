#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using UISearchBar = GregTechCEuTerraria.TerrariaCompat.UI.Widgets.UISearchBar;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class ItemPickerSystem : ModalUISystem
{
	private static ItemPickerSystem? _instance;
	private ItemPickerState? _state;
	private static Action<int>? _onPick;
	private static Action<string>? _onPickFluid;
	private static IReadOnlyCollection<int>? _allowedItems;
	private static IReadOnlyCollection<string>? _allowedFluids;

	internal static bool FluidsEnabled => _onPickFluid != null;
	internal static IReadOnlyCollection<int>? AllowedItems => _allowedItems;
	internal static IReadOnlyCollection<string>? AllowedFluids => _allowedFluids;

	protected override string LayerName => "GregTechCEuTerraria: Item Picker";

	protected override bool CloseOnClickOutside => true;

	public override void Load()  { _instance = this; base.Load(); }
	public override void Unload() { _instance = null; base.Unload(); }

	public static bool IsOpen => _instance?.IsOpenInternal ?? false;

	public static void Open(Action<int> onPick, Action<string>? onFluid = null,
		IReadOnlyCollection<int>? allowedItems = null, IReadOnlyCollection<string>? allowedFluids = null)
	{
		if (_instance?.Ui is null)
			return;
		_onPick = onPick;
		_onPickFluid = onFluid;
		_allowedItems = allowedItems;
		_allowedFluids = allowedFluids;
		if (_instance._state is null)
		{
			_instance._state = new ItemPickerState();
			_instance._state.Activate();
		}
		_instance._state.Refilter();
		_instance.Ui.SetState(_instance._state);
		_instance.PushModal(UILayers.TopModal);
	}

	public static void OpenForItem(Action<Item> onItem) => Open(itemType =>
	{
		var it = new Item();
		it.SetDefaults(itemType);
		it.stack = 1;
		onItem(it);
	});

	public static bool TryPickIntoEmpty(int button, Item current, Action<Item> setToItem)
	{
		if (button != 0 || !Main.mouseItem.IsAir || !current.IsAir)
			return false;
		OpenForItem(setToItem);
		return true;
	}

	internal static void Pick(int itemType)
	{
		if (_allowedItems != null && !_allowedItems.Contains(itemType)) return;
		Action<int>? cb = _onPick;
		Close();
		cb?.Invoke(itemType);
	}

	internal static void PickFluid(string fluidId)
	{
		if (_onPickFluid is not { } cb) return;
		if (_allowedFluids != null && !_allowedFluids.Contains(fluidId)) return;
		Close();
		cb(fluidId);
	}

	public static void Close() => _instance?.CloseInternal();

	protected override void OnClose()
	{
		UISearchBar.UnfocusAll();
		_onPick = null;
		_onPickFluid = null;
		_allowedItems = null;
		_allowedFluids = null;
	}
}

public sealed class ItemPickerState : FreeModalWindow
{
	private UITerrariaPanel _panel = null!;
	private UISearchBar _search = null!;
	private UIItemGrid _grid = null!;
	private UIFavoritesPanel _favorites = null!;
	private UIText _title = null!;
	private bool _built;
	private float _mainLeft, _favLeft;

	private const int Pad = 8;
	private const int TitleH = 28;
	private const int SearchH = 26;
	private const int MoveKnobW = 20;
	private const int ResizeKnobSz = 28;

	private string _query = "";
	private readonly List<int> _filtered = new();
	private readonly List<string> _filteredFluids = new();

	private static List<int>? _universe;
	private static List<FluidType>? _fluidUniverse;
	private static readonly Dictionary<int, string> _nameCache = new();

	protected override void RebuildWindow()
	{
		var root = RootSize();
		ResolveSize(root.X, root.Y, 540f, 600f, minW: 360f, minH: 320f);
		float w = CurW, h = CurH;

		if (!_built) BuildStructure();
		_title.SetText(ItemPickerSystem.FluidsEnabled ? "Pick an item or fluid" : "Pick an item");

		const float FavGap = 6f;
		float favWidth = UIFavoritesPanel.PanelWidth;
		_mainLeft = -(FavGap + favWidth) / 2f;
		_favLeft  =  (w + FavGap) / 2f;

		const float Margin = 8f;
		float groupLeft  = root.X / 2f + _mainLeft - w / 2f;
		float groupRight = root.X / 2f + _favLeft + favWidth / 2f;
		float groupTop   = root.Y / 2f - h / 2f;
		OffMinX = Margin - groupLeft;
		OffMaxX = (root.X - Margin) - groupRight;
		OffMinY = Margin - groupTop;
		OffMaxY = (root.Y - Margin) - (groupTop + h);
		OffsetX = ClampOffset(OffsetX, OffMinX, OffMaxX);
		OffsetY = ClampOffset(OffsetY, OffMinY, OffMaxY);

		_panel.Width  = StyleDimension.FromPixels(w);
		_panel.Height = StyleDimension.FromPixels(h);
		_panel.Left   = StyleDimension.FromPixels(_mainLeft + OffsetX);
		_panel.Top    = StyleDimension.FromPixels(OffsetY);

		_favorites.Left = StyleDimension.FromPixels(_favLeft + OffsetX);
		_favorites.Top  = StyleDimension.FromPixels(OffsetY);
		_favorites.SetHeight(h);

		int top = Pad + TitleH + 6;
		_search.Left   = StyleDimension.FromPixels(Pad);
		_search.Top    = StyleDimension.FromPixels(top);
		_search.Width  = StyleDimension.FromPixels((int)w - Pad * 2);
		_search.Height = StyleDimension.FromPixels(SearchH);

		int gridTop = top + SearchH + 6;
		_grid.Left   = StyleDimension.FromPixels(Pad);
		_grid.Top    = StyleDimension.FromPixels(gridTop);
		_grid.Width  = StyleDimension.FromPixels((int)w - Pad * 2);
		_grid.Height = StyleDimension.FromPixels((int)h - gridTop - Pad);

		LayoutHeaderButtons(_panel, w, Pad, TitleH);
		LayoutResizeKnob(_panel, w, h, ResizeKnobSz);

		Recalculate();

		if (!_built)
		{
			_built = true;
			Refilter();
		}
	}

	private void BuildStructure()
	{
		_panel = new UITerrariaPanel { HAlign = 0.5f, VAlign = 0.5f };
		Append(_panel);

		var moveKnob = NewMoveKnob("Drag to move");
		moveKnob.Left   = StyleDimension.FromPixels(Pad);
		moveKnob.Top    = StyleDimension.FromPixels(Pad);
		moveKnob.Width  = StyleDimension.FromPixels(MoveKnobW);
		moveKnob.Height = StyleDimension.FromPixels(TitleH);
		_panel.Append(moveKnob);

		_title = new UIText("Pick an item", 1.05f)
		{
			Left = StyleDimension.FromPixels(Pad + MoveKnobW + 6),
			Top  = StyleDimension.FromPixels(Pad),
			IgnoresMouseInteraction = true,
		};
		_panel.Append(_title);

		_favorites = new UIFavoritesPanel
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			OnPickItem = ItemPickerSystem.Pick,
			OnPickFluid = ItemPickerSystem.PickFluid,
			HideWhenDocked = true,
		};
		Append(_favorites);

		_search = new UISearchBar("Search...", text => { _query = text ?? ""; Refilter(); });
		_panel.Append(_search);

		_grid = new UIItemGrid(() => _filtered, "No items match this search",
			onPick: ItemPickerSystem.Pick,
			fluidSource: () => _filteredFluids,
			onPickFluid: ItemPickerSystem.PickFluid) { ScrollBottomInset = ResizeKnobSz };
		_panel.Append(_grid);
	}

	protected override void ApplyOffsetLive()
	{
		if (_panel is null) return;
		_panel.Left = StyleDimension.FromPixels(_mainLeft + OffsetX);
		_panel.Top  = StyleDimension.FromPixels(OffsetY);
		if (_favorites is not null)
		{
			_favorites.Left = StyleDimension.FromPixels(_favLeft + OffsetX);
			_favorites.Top  = StyleDimension.FromPixels(OffsetY);
		}
		Recalculate();
	}

	internal void Refilter()
	{
		_filtered.Clear();
		_filteredFluids.Clear();
		string[] tokens = string.IsNullOrWhiteSpace(_query)
			? Array.Empty<string>()
			: _query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

		IEnumerable<int> universe = ItemPickerSystem.AllowedItems ?? Universe();
		foreach (int type in universe)
			if (Matches(NameLower(type), tokens)) _filtered.Add(type);

		if (ItemPickerSystem.FluidsEnabled)
			foreach (var f in FluidUniverse())
			{
				if (ItemPickerSystem.AllowedFluids != null && !ItemPickerSystem.AllowedFluids.Contains(f.Id)) continue;
				if (Matches(f.DisplayName.ToLowerInvariant(), tokens)) _filteredFluids.Add(f.Id);
			}
	}

	private static bool Matches(string name, string[] tokens)
	{
		foreach (string tk in tokens)
			if (!name.Contains(tk)) return false;
		return true;
	}

	private static List<FluidType> FluidUniverse()
	{
		if (_fluidUniverse != null) return _fluidUniverse;
		_fluidUniverse = new List<FluidType>(FluidRegistry.All);
		_fluidUniverse.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
		return _fluidUniverse;
	}

	private static string NameLower(int type)
	{
		if (_nameCache.TryGetValue(type, out string? n)) return n;
		n = Lang.GetItemNameValue(type).ToLowerInvariant();
		_nameCache[type] = n;
		return n;
	}

	private static List<int> Universe()
	{
		if (_universe != null) return _universe;
		_universe = new List<int>(ContentSamples.ItemsByType.Count);
		foreach (var kv in ContentSamples.ItemsByType)
			if (kv.Key > 0 && kv.Value != null) _universe.Add(kv.Key);
		_universe.Sort();
		return _universe;
	}
}
