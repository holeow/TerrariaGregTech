#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using GregTechCEuTerraria.TerrariaCompat.Net;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;

public sealed class CraftConfirmWindow : UIModalWindow
{
	private Point16 _termPos;
	private AEKey? _key;
	private long _amount;
	private CraftingPlanSummary? _summary;
	private List<CpuInfo> _cpus = new();
	private Dictionary<AEKey, string> _invalid = new();
	private int _selectedCpu = -1;
	private bool _graphMode;
	private bool _missingOnly;
	private bool _pendingRebuild;
	private bool _stationMode;

	public void Bind(Point16 termPos, AEKey key, long amount, CraftingPlanSummary summary,
		List<CpuInfo> cpus, List<(AEKey what, string reason)> invalid)
	{
		_termPos = termPos;
		_key = key;
		_amount = amount;
		_summary = summary;
		_cpus = cpus;
		_invalid = new Dictionary<AEKey, string>();
		if (invalid != null)
			foreach (var (w, reason) in invalid) _invalid[w] = reason;
		_selectedCpu = -1;
		_graphMode = false;
		_missingOnly = false;
		_stationMode = false;
		RemoveAllChildren();
		BuildPanel();
	}

	public void BindStation(Point16 termPos, AEKey key, CraftingPlanSummary summary)
	{
		_termPos = termPos;
		_key = key;
		_amount = 0;
		_summary = summary;
		_cpus = new List<CpuInfo>();
		_invalid = new Dictionary<AEKey, string>();
		_selectedCpu = -1;
		_graphMode = false;
		_missingOnly = false;
		_stationMode = true;
		RemoveAllChildren();
		BuildPanel();
	}

	public override void Update(GameTime gameTime)
	{
		if (_pendingRebuild)
		{
			_pendingRebuild = false;
			RemoveAllChildren();
			BuildPanel();
		}
		base.Update(gameTime);
	}

	public void Unbind()
	{
		RemoveAllChildren();
		_key = null;
		_summary = null;
	}

	private bool HasAvailableCpu =>
		_selectedCpu >= 0 && _selectedCpu < _cpus.Count
			? !_cpus[_selectedCpu].Busy
			: _cpus.Exists(c => !c.Busy);

	private bool Startable => _summary != null && !_summary.Simulation && HasAvailableCpu
		&& _invalid.Count == 0;

	private void BuildPanel()
	{
		if (_summary is null || _key is null) return;

		const int W = 760, H = 600;
		int favW = UIFavoritesPanel.PanelWidth, favGap = 6;
		float mainLeft = -(favGap + favW) / 2f;
		float favLeft = (W + favGap) / 2f;
		var panel = new UITerrariaPanel
		{
			Width = StyleDimension.FromPixels(W),
			Height = StyleDimension.FromPixels(H),
			HAlign = 0.5f,
			VAlign = 0.4f,
			Left = StyleDimension.FromPixels(mainLeft),
		};

		panel.Append(new UIText("Crafting Plan", 0.95f)
		{ Left = StyleDimension.FromPixels(12), Top = StyleDimension.FromPixels(10) });

		string status = _stationMode
			? "[c/FF8888:Not enough ingredients in the network]"
			: _summary.Simulation
			? $"[c/FF8888:Missing items - cannot craft]    {_summary.UsedBytes:N0} bytes"
			: _invalid.Count > 0
				? $"[c/FF8888:Cannot start - missing crafting station(s) (see red items)]    {_summary.UsedBytes:N0} bytes"
				: !HasAvailableCpu
					? $"[c/FF8888:No available Crafting CPU]    {_summary.UsedBytes:N0} bytes"
					: $"Bytes used: {_summary.UsedBytes:N0}";
		panel.Append(new UIText(status, 0.75f)
		{ Left = StyleDimension.FromPixels(12), Top = StyleDimension.FromPixels(32) });

		var favorites = new UIFavoritesPanel
		{
			HAlign = 0.5f,
			VAlign = 0.4f,
			Left = StyleDimension.FromPixels(favLeft),
			HideWhenDocked = true,
		};
		favorites.SetHeight(H);
		Append(favorites);

		int contentLeft = 12;
		int contentW = W - 24;
		if (_graphMode)
		{
			var graph = new CraftGraphView
			{
				Left = StyleDimension.FromPixels(contentLeft),
				Top = StyleDimension.FromPixels(52),
				Width = StyleDimension.FromPixels(contentW),
				Height = StyleDimension.FromPixels(H - 114),
			};
			graph.Build(_summary, _missingOnly);
			panel.Append(graph);
		}
		else
		{
			panel.Append(new EntryList(_summary, _invalid, _missingOnly)
			{
				Left = StyleDimension.FromPixels(contentLeft),
				Top = StyleDimension.FromPixels(52),
				Width = StyleDimension.FromPixels(contentW),
				Height = StyleDimension.FromPixels(H - 114),
			});
		}

		var missingBtn = new UITextButton(
			label: () => _missingOnly ? "Show All" : "Show Missing",
			onLeft: () => { _missingOnly = !_missingOnly; _pendingRebuild = true; },
			width: 120, height: 18)
		{ Left = StyleDimension.FromPixels(W - 132), Top = StyleDimension.FromPixels(30) };
		missingBtn.IsActive = () => _missingOnly;
		panel.Append(missingBtn);

		var graphBtn = new UITextButton(
			label: () => _graphMode ? "Show List" : "Show Graph",
			onLeft: () => { _graphMode = !_graphMode; _pendingRebuild = true; },
			width: 120, height: 18)
		{ Left = StyleDimension.FromPixels(W - 132 - 124), Top = StyleDimension.FromPixels(30) };
		graphBtn.IsActive = () => _graphMode;
		panel.Append(graphBtn);

		// TODO cpu selection when we get different crafting CPU
		panel.Append(new UITextButton(
			label: CpuLabel, onLeft: CycleCpu,
			tooltip: "Choose which Crafting CPU runs this job",
			width: 160, height: 22)
		{
			Left = StyleDimension.FromPixels(W - 172),
			Top = StyleDimension.FromPixels(8),
			IsVisible = () => false,
		});

		const int rowY = H - 44, btnH = 36;
		const float btnTextScale = 0.95f;
		if (_stationMode)
		{
			int sbW = (W - 36) / 2;
			panel.Append(new UITextButton(
				label: () => "Retry", onLeft: RetryStation,
				tooltip: "Reopen the craft window and try again",
				width: sbW, height: btnH, textScale: btnTextScale)
			{ Left = StyleDimension.FromPixels(12), Top = StyleDimension.FromPixels(rowY) });

			panel.Append(new UITextButton(
				label: () => "Close", onLeft: MeCraftConfirmSystem.Close,
				tooltip: null,
				width: sbW, height: btnH, textScale: btnTextScale)
			{ Left = StyleDimension.FromPixels(24 + sbW), Top = StyleDimension.FromPixels(rowY) });
		}
		else
		{
			int btnW = (W - 48) / 3;
			panel.Append(new UITextButton(
				label: () => "Close", onLeft: MeCraftConfirmSystem.Close,
				tooltip: null,
				width: btnW, height: btnH, textScale: btnTextScale)
			{ Left = StyleDimension.FromPixels(12), Top = StyleDimension.FromPixels(rowY) });

			panel.Append(new UITextButton(
				label: () => "Replan", onLeft: Replan,
				tooltip: "Recompute the plan against the current network contents",
				width: btnW, height: btnH, textScale: btnTextScale)
			{ Left = StyleDimension.FromPixels(24 + btnW), Top = StyleDimension.FromPixels(rowY) });

			panel.Append(new UITextButton(
				label: () => "Start", onLeft: Start,
				tooltip: "Begin crafting",
				width: btnW, height: btnH, textScale: btnTextScale)
			{
				Left = StyleDimension.FromPixels(36 + 2 * btnW),
				Top = StyleDimension.FromPixels(rowY),
				IsDisabled = () => !Startable,
			});
		}

		Append(panel);
	}

	private string CpuLabel()
	{
		if (_cpus.Count == 0) return "No Crafting CPUs";
		if (_selectedCpu < 0) return "CPU: Automatic";
		var c = _cpus[_selectedCpu];
		return $"CPU: ({c.Pos.X},{c.Pos.Y})  {c.Bytes:N0}b";
	}

	private void CycleCpu()
	{
		if (_cpus.Count == 0) return;
		_selectedCpu++;
		if (_selectedCpu >= _cpus.Count) _selectedCpu = -1;
	}

	private void Replan()
	{
		if (_key is null) return;
		MeCraftPackets.Begin(_termPos, _key, _amount);
	}

	private void RetryStation()
	{
		if (_key is not AEItemKey ik) return;
		MeCraftConfirmSystem.Close();
		MeStationCraftSystem.OpenFor(_termPos, ik.GetItem());
	}

	private void Start()
	{
		if (_key is null || !Startable) return;
		bool automatic = _selectedCpu < 0;
		var cpuPos = automatic ? default : _cpus[_selectedCpu].Pos;
		MeCraftPackets.Submit(_termPos, _key, _amount, automatic, cpuPos);
		MeCraftConfirmSystem.Close();
	}

	private sealed class EntryList : UIElement
	{
		private readonly CraftingPlanSummary _summary;
		private readonly Dictionary<AEKey, string> _invalid;
		private readonly bool _missingOnly;
		private int _scrollRow;
		private readonly Scrollbar _bar = new();
		private const int SbW = Scrollbar.Width;

		public EntryList(CraftingPlanSummary summary, Dictionary<AEKey, string> invalid, bool missingOnly)
		{
			_summary = summary;
			_invalid = invalid;
			_missingOnly = missingOnly;
		}

		public override void ScrollWheel(UIScrollWheelEvent evt)
		{
			base.ScrollWheel(evt);
			if (!IsMouseHovering) return;
			Scrollbar.Wheel(evt, ref _scrollRow, CraftCell.Step);
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			var b = GetDimensions().ToRectangle();
			var px = TextureAssets.MagicPixel.Value;
			sb.Draw(px, b, new Color(20, 22, 50) * 0.5f);
			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				PlayerInput.LockVanillaMouseScroll("GregTechCEuTerraria/MeCraftConfirm");
			}

			IReadOnlyList<CraftingPlanSummaryEntry> entries = _summary.Entries;
			if (_missingOnly)
			{
				var filtered = new List<CraftingPlanSummaryEntry>();
				foreach (var e in _summary.Entries) if (e.MissingAmount > 0) filtered.Add(e);
				entries = filtered;
			}
			int gridW = b.Width - SbW;
			int cols = System.Math.Max(1, (gridW + CraftCell.Pad) / CraftCell.Step);
			int totalRows = (entries.Count + cols - 1) / cols;
			int viewRows = System.Math.Max(1, b.Height / CraftCell.Step);
			int maxRowScroll = System.Math.Max(0, totalRows - viewRows);

			var mouse = ModalEscape.PollCursor();

			bool showBar = totalRows > viewRows;
			Rectangle track = Rectangle.Empty, thumb = Rectangle.Empty;
			if (showBar)
			{
				track = new Rectangle(b.Right - SbW, b.Y, SbW, b.Height);
				thumb = _bar.Update(track, maxRowScroll, (float)viewRows / totalRows, ref _scrollRow, mouse);
			}
			if (_scrollRow > maxRowScroll) _scrollRow = maxRowScroll;

			CraftingPlanSummaryEntry? hovered = null;
			for (int r = 0; r < viewRows; r++)
			{
				for (int c = 0; c < cols; c++)
				{
					int i = (r + _scrollRow) * cols + c;
					if (i >= entries.Count) break;
					var e = entries[i];
					var rect = new Rectangle(b.X + c * CraftCell.Step, b.Y + r * CraftCell.Step, CraftCell.Size, CraftCell.Size);

					Color bg = e.MissingAmount > 0 ? new Color(120, 40, 40) * 0.7f
						: e.CraftAmount > 0 ? new Color(34, 90, 50) * 0.7f
						: new Color(30, 34, 64) * 0.7f;
					long amount = e.MissingAmount > 0 ? e.MissingAmount
						: e.CraftAmount > 0 ? e.CraftAmount : e.StoredAmount;
					bool hov = !_bar.Dragging && rect.Contains(mouse);
					_invalid.TryGetValue(e.What, out var badReason);
					CraftCell.Draw(sb, rect, e.What, amount, bg, hov, badReason);
					if (badReason != null) DrawRedFrame(sb, px, rect);
					if (hov) hovered = e;
				}
			}

			if (hovered != null && !_bar.Dragging)
			{
				var click = BrowserSlotInteraction.Poll();
				if (hovered.What is AEItemKey ik)
					BrowserSlotInteraction.HandleItem(click, ik.GetItem(), inFavoritesPane: false);
				else if (hovered.What is AEFluidKey fk)
					BrowserSlotInteraction.HandleFluid(click, fk.GetFluid(), recipeAmountMb: null, inFavoritesPane: false);
			}

			if (showBar)
				_bar.Draw(sb, track, thumb, mouse);

		}

		private static void DrawRedFrame(SpriteBatch sb, Texture2D px, Rectangle r)
		{
			var col = new Color(230, 60, 60);
			const int t = 2;
			sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, t), col);
			sb.Draw(px, new Rectangle(r.X, r.Bottom - t, r.Width, t), col);
			sb.Draw(px, new Rectangle(r.X, r.Y, t, r.Height), col);
			sb.Draw(px, new Rectangle(r.Right - t, r.Y, t, r.Height), col);
		}
	}
}
