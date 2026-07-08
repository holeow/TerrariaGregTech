#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Core;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Terminal;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Terminal;

public static class MeModularTerminalHud
{
	private const int Pad = 8;
	private const int Margin = 12;
	private const int Gap = 8;
	private const int TitleH = 22, ToggleH = 22, SortH = 20, RowGap = 6;

	public static void Build(MachineUIState state, MeModularTerminalMachine term)
	{
		var screen = ScreenLayout.Screen;
		float invRight = VanillaHudOccupancy.InventoryRegion.Right;

		float centerLeft = invRight + Gap;
		float centerW = Math.Max(300f, (screen.Width - invRight) * 0.5f - Gap);
		float top = Margin;
		float bottom = screen.Bottom - VanillaHudOccupancy.BottomReserve;

		string mode = ResolveMode(term);
		bool crafting = term.HasUpgrade("crafting");
		bool encoding = term.HasUpgrade("pattern_encoding");
		int cell = (int)Math.Clamp((centerW - Pad * 2) / 9f - 2f, 24f, 40f);

		var grid = mode == MeModularTerminalMode.Terminal ? new UIMeTerminalGrid(term) : null;

		float upperH = UpperHeight(mode, crafting, cell);
		float bottomH = encoding ? MePatternEncodingBar.BarHeight + 24 : 0f;
		float centralTop = top + upperH + Gap;
		float centralBottom = (encoding ? bottom - bottomH - Gap : bottom);
		float centralH = Math.Max(120f, centralBottom - centralTop);

		BuildUpper(state, term, mode, crafting, cell, centerLeft, top, centerW, upperH);
		BuildCentral(state, term, mode, grid, crafting, centerLeft, centralTop, centerW, centralH);
		if (encoding)
			BuildEncoding(state, term, centerLeft, bottom - bottomH, centerW, bottomH);

		state.Append(new UpgradeSetWatcher(term, state));
	}

	private static int UpgradeSignature(MeModularTerminalMachine term)
	{
		int h = 17;
		foreach (var u in term.InstalledUpgrades()) h = h * 31 + u.Id.GetHashCode();
		return h;
	}

	private sealed class UpgradeSetWatcher : UIElement
	{
		private readonly MeModularTerminalMachine _term;
		private readonly MachineUIState _state;
		private readonly int _sig;

		public UpgradeSetWatcher(MeModularTerminalMachine term, MachineUIState state)
		{
			_term = term;
			_state = state;
			_sig = UpgradeSignature(term);
		}

		public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
		{
			base.Update(gameTime);
			if (UpgradeSignature(_term) != _sig) _state.RequestRebuild();
		}
	}

	private static string ResolveMode(MeModularTerminalMachine term)
	{
		var m = MeModularTerminalMode.Current;
		if (m == MeModularTerminalMode.CraftingStatus && term.HasUpgrade("crafting_status")) return m;
		if (m == MeModularTerminalMode.PatternAccess && term.HasUpgrade("pattern_access")) return m;
		return MeModularTerminalMode.Terminal;
	}

	private static float UpperHeight(string mode, bool crafting, int cell) =>
		4 + TitleH + ToggleH + RowGap + cell + RowGap + 4;

	private static void BuildUpper(MachineUIState state, MeModularTerminalMachine term, string mode,
		bool crafting, int cell, float left, float top, float w, float h)
	{
		var panel = new UITerrariaPanel
		{
			Left = StyleDimension.FromPixels(left),
			Top = StyleDimension.FromPixels(top),
			Width = StyleDimension.FromPixels(w),
			Height = StyleDimension.FromPixels(h),
		};

		panel.Append(new UIText(term.DisplayName, 0.85f)
		{
			Left = StyleDimension.FromPixels(Pad),
			Top = StyleDimension.FromPixels(4),
		});
		panel.Append(new UIDynamicLabel(() =>
		{
			var net = term.Network;
			if (net == null) return $"[c/FF8888:{Language.GetTextValue(AELocale.TerminalNotConnected)}]";
			return Language.GetTextValue(AELocale.TerminalStorageDevices, net.MountedStorageCount);
		}, 0.6f)
		{
			HAlign = 1f,
			Left = StyleDimension.FromPixels(-Pad),
			Top = StyleDimension.FromPixels(6),
			Width = StyleDimension.FromPixels(120),
			Height = StyleDimension.FromPixels(14),
		});

		int y = TitleH + 2;
		BuildModeToggles(state, term, panel, w, y, mode);
		y += ToggleH + RowGap;

		SlotRow(panel, term, SlotGroup.InventoryInput, MeModularTerminalMachine.UpgradeSlots, Pad, y, cell, UpgradeSlotHint());
		if (crafting)
		{
			const int csW = 140;
			panel.Append(new UITextButton(
				label: () => "Crafting Stations",
				onLeft: () => MeCraftingStationsSystem.OpenFor(term),
				tooltip: "Configure which crafting stations this terminal can use",
				width: csW, height: cell, textScale: 0.8f)
			{ Left = StyleDimension.FromPixels(w - Pad - csW), Top = StyleDimension.FromPixels(y) });
		}
		y += cell + RowGap;

		state.Append(panel);
	}

	private static void BuildModeToggles(MachineUIState state, MeModularTerminalMachine term,
		UITerrariaPanel panel, float w, int y, string activeMode)
	{
		var modes = new List<(string id, string label)> { (MeModularTerminalMode.Terminal, "Terminal") };
		if (term.HasUpgrade("crafting_status")) modes.Add((MeModularTerminalMode.CraftingStatus, "Crafting Status"));
		if (term.HasUpgrade("pattern_access")) modes.Add((MeModularTerminalMode.PatternAccess, "Pattern Access"));

		int n = modes.Count;
		int bw = (int)((w - Pad * 2 - (n - 1) * 4) / n);
		for (int i = 0; i < n; i++)
		{
			string id = modes[i].id;
			string label = modes[i].label;
			panel.Append(new UITextButton(
				label: () => id == activeMode ? $"[c/55FFFF:{label}]" : label,
				onLeft: () => { MeModularTerminalMode.Set(id); state.RequestRebuild(); },
				tooltip: null,
				width: bw, height: ToggleH)
			{
				Left = StyleDimension.FromPixels(Pad + i * (bw + 4)),
				Top = StyleDimension.FromPixels(y),
			});
		}
	}

	private static void BuildSortRow(UITerrariaPanel panel, UIMeTerminalGrid grid, float w, int y)
	{
		int sw = (int)((w - Pad * 2 - 12) / 4);
		SortBtn(panel, () => $"Sort: {SortLabel(grid.SortBy)}", grid.CycleSort, Pad, y, sw);
		SortBtn(panel, () => grid.Dir == SortDir.ASCENDING ? "Direction: Ascending" : "Direction: Descending", grid.ToggleDir, Pad + sw + 4, y, sw);
		SortBtn(panel, () => $"View: {ViewLabel(grid.ViewMode)}", grid.CycleView, Pad + (sw + 4) * 2, y, sw);
		SortBtn(panel, () => TerminalSearchPersist.KeepOnClose ? "Search: Keep on close" : "Search: Don't keep", grid.ToggleSearchPersist, Pad + (sw + 4) * 3, y, sw);
	}

	private static void SortBtn(UITerrariaPanel panel, Func<string> label, Action onLeft, int x, int y, int w)
	{
		panel.Append(new UITextButton(label, onLeft, null, null, w, SortH, 0.55f)
		{
			Left = StyleDimension.FromPixels(x),
			Top = StyleDimension.FromPixels(y),
		});
	}

	private static void BuildCraftableRow(UITerrariaPanel panel, float w, int y)
	{
		panel.Append(new UIText("Show manual craft:", 0.7f)
		{
			Left = StyleDimension.FromPixels(Pad),
			Top = StyleDimension.FromPixels(y + 4),
		});

		const int labelW = 116;
		int bx = Pad + labelW;
		int area = (int)(w - Pad * 2 - labelW);
		int bw = (area - 12) / 4;
		CraftBtn(panel, TerminalCraftableView.Mode.DontShow, bx, y, bw);
		CraftBtn(panel, TerminalCraftableView.Mode.ShowCraftable, bx + (bw + 4), y, bw);
		CraftBtn(panel, TerminalCraftableView.Mode.ShowAll, bx + (bw + 4) * 2, y, bw);
		CraftBtn(panel, TerminalCraftableView.Mode.ShowAllGregtech, bx + (bw + 4) * 3, y, bw);
	}

	private static void CraftBtn(UITerrariaPanel panel, TerminalCraftableView.Mode mode, int x, int y, int w)
	{
		panel.Append(new UITextButton(
			label: () => TerminalCraftableView.Current == mode
				? $"[c/55FFFF:{TerminalCraftableView.Label(mode)}]"
				: TerminalCraftableView.Label(mode),
			onLeft: () => TerminalCraftableView.Set(mode),
			tooltip: null,
			width: w, height: SortH, textScale: 0.8f)
		{
			Left = StyleDimension.FromPixels(x),
			Top = StyleDimension.FromPixels(y),
		});
	}

	private static void SlotRow(UITerrariaPanel panel, MetaMachine machine, SlotGroup group,
		int count, int x, int y, int cell, string? emptyHint = null)
	{
		for (int i = 0; i < count; i++)
			panel.Append(new UISlot(machine, group, i, ItemSlot.Context.ChestItem)
			{
				Left = StyleDimension.FromPixels(x + i * (cell + 2)),
				Top = StyleDimension.FromPixels(y),
				Width = StyleDimension.FromPixels(cell),
				Height = StyleDimension.FromPixels(cell),
				EmptyHint = emptyHint,
			});
	}

	private static string UpgradeSlotHint()
	{
		var sb = new System.Text.StringBuilder("Available terminal upgrades can be put here:");
		foreach (var u in MeTerminalUpgrades.All)
			sb.Append("\n  ").Append(ShortName(u.DisplayName));
		return sb.ToString();
	}

	private static string ShortName(string displayName) =>
		displayName.Replace(" Terminal Card", "").Replace(" Card", "");

	private static void BuildCentral(MachineUIState state, MeModularTerminalMachine term, string mode,
		UIMeTerminalGrid? grid, bool crafting, float left, float top, float w, float h)
	{
		var panel = new UITerrariaPanel
		{
			Left = StyleDimension.FromPixels(left),
			Top = StyleDimension.FromPixels(top),
			Width = StyleDimension.FromPixels(w),
			Height = StyleDimension.FromPixels(h),
		};

		if (mode == MeModularTerminalMode.Terminal && grid != null)
		{
			int y = 8;
			BuildSortRow(panel, grid, w, y);
			y += SortH + RowGap;
			if (crafting)
			{
				BuildCraftableRow(panel, w, y);
				y += SortH + RowGap;
			}

			grid.Left = StyleDimension.FromPixels(8);
			grid.Top = StyleDimension.FromPixels(y);
			grid.Width = StyleDimension.FromPixels(w - 16);
			grid.Height = StyleDimension.FromPixels(h - y - 8);
			panel.Append(grid);
		}
		else
		{
			UIElement content = mode switch
			{
				MeModularTerminalMode.CraftingStatus => new CraftStatusView(term.Position, w - 16, h - 16),
				MeModularTerminalMode.PatternAccess => new UIPatternAccessList(term),
				_ => grid!,
			};
			content.Left = StyleDimension.FromPixels(8);
			content.Top = StyleDimension.FromPixels(8);
			content.Width = StyleDimension.FromPixels(w - 16);
			content.Height = StyleDimension.FromPixels(h - 16);
			panel.Append(content);
		}

		state.Append(panel);
	}

	private static void BuildEncoding(MachineUIState state, MeModularTerminalMachine term,
		float left, float top, float w, float h)
	{
		var panel = new UITerrariaPanel
		{
			Left = StyleDimension.FromPixels(left),
			Top = StyleDimension.FromPixels(top),
			Width = StyleDimension.FromPixels(w),
			Height = StyleDimension.FromPixels(h),
		};
		panel.Append(new UIText("Pattern Encoding", 0.6f)
		{
			Left = StyleDimension.FromPixels(8),
			Top = StyleDimension.FromPixels(4),
		});
		panel.Append(new MePatternEncodingBar(term)
		{
			Left = StyleDimension.FromPixels(8),
			Top = StyleDimension.FromPixels(20),
			Width = StyleDimension.FromPixels(MePatternEncodingBar.BarWidth),
			Height = StyleDimension.FromPixels(MePatternEncodingBar.BarHeight),
		});
		state.Append(panel);
	}

	private static string SortLabel(SortOrder o) => o switch
	{
		SortOrder.NAME => "Name", SortOrder.AMOUNT => "Amount", _ => "Mod",
	};

	private static string ViewLabel(ViewItems v) => v switch
	{
		ViewItems.ALL => "All", ViewItems.STORED => "Stored", _ => "Autocraftable",
	};
}
