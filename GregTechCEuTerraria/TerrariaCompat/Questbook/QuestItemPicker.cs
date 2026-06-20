#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.UI;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using UISearchBar = GregTechCEuTerraria.TerrariaCompat.UI.Widgets.UISearchBar;

namespace GregTechCEuTerraria.TerrariaCompat.Questbook;

public sealed class QuestItemPickerSystem : ModalUISystem
{
	private static QuestItemPickerSystem? _instance;
	private QuestItemPickerState? _state;
	private static Action<int>? _onPick;

	protected override string LayerName => "GregTechCEuTerraria: Quest Item Picker";

	public override void Load()  { _instance = this; base.Load(); }
	public override void Unload() { _instance = null; base.Unload(); }

	public static bool IsOpen => _instance?.IsOpenInternal ?? false;

	public static void Open(Action<int> onPick)
	{
		if (_instance?.Ui is null)
			return;
		_onPick = onPick;
		if (_instance._state is null)
		{
			_instance._state = new QuestItemPickerState();
			_instance._state.Activate();
		}
		_instance.Ui.SetState(_instance._state);
		_instance.PushModal();
		_instance._state.OnShown();
	}

	internal static void Pick(int itemType)
	{
		Action<int>? cb = _onPick;
		Close();
		cb?.Invoke(itemType);
	}

	public static void Close() => _instance?.CloseInternal();

	protected override void OnClose()
	{
		UISearchBar.UnfocusAll();
		_onPick = null;
	}
}

public sealed class QuestItemPickerState : FreeModalWindow
{
	private UITerrariaPanel _panel = null!;
	private UISearchBar _search = null!;
	private UIItemGrid _grid = null!;
	private bool _built;

	private const int Pad = 8;
	private const int TitleH = 28;
	private const int SearchH = 26;
	private const int MoveKnobW = 20;
	private const int ResizeKnobSz = 28;

	private string _query = "";
	private readonly List<int> _filtered = new();

	private static List<int>? _universe;
	private static readonly Dictionary<int, string> _nameCache = new();

	protected override void RebuildWindow()
	{
		var root = RootSize();
		ResolveSize(root.X, root.Y, 540f, 600f, minW: 360f, minH: 320f);
		float w = CurW, h = CurH;

		if (!_built) BuildStructure();

		_panel.Width  = StyleDimension.FromPixels(w);
		_panel.Height = StyleDimension.FromPixels(h);

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
		ApplyCenteredMoveClamp(_panel, root, w, h);

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

		var title = new UIText("Pick an item", 1.05f)
		{
			Left = StyleDimension.FromPixels(Pad + MoveKnobW + 6),
			Top  = StyleDimension.FromPixels(Pad),
			IgnoresMouseInteraction = true,
		};
		_panel.Append(title);

		_search = new UISearchBar("Search items...", text => { _query = text ?? ""; Refilter(); });
		_panel.Append(_search);

		_grid = new UIItemGrid(() => _filtered, "No items match this search",
			onPick: QuestItemPickerSystem.Pick) { ScrollBottomInset = ResizeKnobSz };
		_panel.Append(_grid);
	}

	internal void OnShown() => _grid?.ResetPickArming();

	protected override void ApplyOffsetLive()
	{
		if (_panel is null) return;
		_panel.Left = StyleDimension.FromPixels(OffsetX);
		_panel.Top  = StyleDimension.FromPixels(OffsetY);
		Recalculate();
	}

	private void Refilter()
	{
		_filtered.Clear();
		List<int> universe = Universe();
		if (string.IsNullOrWhiteSpace(_query))
		{
			_filtered.AddRange(universe);
			return;
		}
		string[] tokens = _query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
		foreach (int type in universe)
		{
			string name = NameLower(type);
			bool ok = true;
			foreach (string tk in tokens)
				if (!name.Contains(tk)) { ok = false; break; }
			if (ok) _filtered.Add(type);
		}
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
