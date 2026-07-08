#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Terminal;

public static class StationCapabilities
{
	private static List<(int tile, string name)>? _universe;
	private static Dictionary<int, string>? _names;

	public static IReadOnlyList<(int tile, string name)> Universe()
	{
		if (_universe != null) return _universe;
		BuildNames();
		var tiles = new HashSet<int>();
		for (int i = 0; i < Terraria.Recipe.numRecipes; i++)
			foreach (int t in Main.recipe[i].requiredTile)
				if (t > -1) tiles.Add(t);
		var list = new List<(int tile, string name)>();
		foreach (int t in tiles) list.Add((t, NameFor(t)));
		list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
		_universe = list;
		return _universe;
	}

	public static bool HasStationItem(int tile)
	{
		BuildNames();
		return _names!.ContainsKey(tile);
	}

	public static bool IsSupportable(Terraria.Recipe r)
	{
		foreach (int t in r.requiredTile)
			if (t > -1 && !HasStationItem(t)) return false;
		return true;
	}

	public static string NameFor(int tile)
	{
		BuildNames();
		if (_names!.TryGetValue(tile, out var n)) return n;
		string map = MapName(tile);
		return string.IsNullOrEmpty(map) ? $"Tile {tile}" : map;
	}

	private static string MapName(int tile)
	{
		try { return Lang.GetMapObjectName(Terraria.Map.MapHelper.TileToLookup(tile, 0)) ?? ""; }
		catch { return ""; }
	}

	private static void BuildNames()
	{
		if (_names != null) return;
		_names = new Dictionary<int, string>();
		foreach (var kv in ContentSamples.ItemsByType)
		{
			var it = kv.Value;
			if (it != null && !it.IsAir && it.createTile > -1 && !_names.ContainsKey(it.createTile))
				_names[it.createTile] = it.Name;
		}
	}
}

public sealed class CraftingStationsWindow : UIModalWindow
{
	private MetaMachine? _machine;

	public void Bind(MetaMachine machine)
	{
		_machine = machine;
		RemoveAllChildren();
		BuildPanel();
	}

	public void Unbind()
	{
		RemoveAllChildren();
		_machine = null;
	}

	private HashSet<int> Covered() =>
		_machine is IMeCraftingHost h ? h.Crafting.StationTiles() : new HashSet<int>();

	private List<string> CapNames(bool covered)
	{
		var have = Covered();
		var list = new List<string>();
		foreach (var (tile, name) in StationCapabilities.Universe())
			if (have.Contains(tile) == covered) list.Add(name);
		return list;
	}

	private void BuildPanel()
	{
		if (_machine is null) return;

		const int cell = 40, pad = 12, gap = 2, titleH = 30, listH = 220, headH = 18;
		int cols = CraftingStationState.StationCols;
		int rows = (CraftingStationState.StationSlots + cols - 1) / cols;
		int gridH = rows * (cell + gap) - gap;
		const int w = 540;
		int gridY = titleH;
		int listTop = gridY + gridH + 10;
		int h = listTop + headH + listH + pad;

		var panel = new UITerrariaPanel
		{
			Width = StyleDimension.FromPixels(w),
			Height = StyleDimension.FromPixels(h),
			HAlign = 0.5f,
			VAlign = 0.4f,
		};

		panel.Append(new UIText("Crafting Stations", 0.9f)
		{ Left = StyleDimension.FromPixels(pad), Top = StyleDimension.FromPixels(9) });

		for (int i = 0; i < CraftingStationState.StationSlots; i++)
		{
			int r = i / cols, c = i % cols;
			panel.Append(new UISlot(_machine, SlotGroup.CraftingStation, i, ItemSlot.Context.ChestItem)
			{
				Left = StyleDimension.FromPixels(pad + c * (cell + gap)),
				Top = StyleDimension.FromPixels(gridY + r * (cell + gap)),
				Width = StyleDimension.FromPixels(cell),
				Height = StyleDimension.FromPixels(cell),
			});
		}

		int colW = (w - pad * 2 - gap) / 2;
		int rightX = pad + colW + gap;

		panel.Append(new UIText("Missing", 0.75f)
		{ Left = StyleDimension.FromPixels(pad), Top = StyleDimension.FromPixels(listTop) });
		panel.Append(new UIText("Covered", 0.75f)
		{ Left = StyleDimension.FromPixels(rightX), Top = StyleDimension.FromPixels(listTop) });

		panel.Append(new CapList(() => CapNames(false), new Color(235, 120, 120))
		{
			Left = StyleDimension.FromPixels(pad),
			Top = StyleDimension.FromPixels(listTop + headH),
			Width = StyleDimension.FromPixels(colW),
			Height = StyleDimension.FromPixels(listH),
		});
		panel.Append(new CapList(() => CapNames(true), new Color(120, 230, 120))
		{
			Left = StyleDimension.FromPixels(rightX),
			Top = StyleDimension.FromPixels(listTop + headH),
			Width = StyleDimension.FromPixels(colW),
			Height = StyleDimension.FromPixels(listH),
		});

		Append(panel);
	}

	private sealed class CapList : UIElement
	{
		private readonly Func<List<string>> _supplier;
		private readonly Color _color;
		private int _scroll;
		private readonly Scrollbar _bar = new();
		private const int LineH = 18;

		public CapList(Func<List<string>> supplier, Color color)
		{
			_supplier = supplier;
			_color = color;
		}

		public override void ScrollWheel(UIScrollWheelEvent evt)
		{
			base.ScrollWheel(evt);
			if (!IsMouseHovering) return;
			Scrollbar.Wheel(evt, ref _scroll, LineH);
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			var b = GetDimensions().ToRectangle();
			var px = TextureAssets.MagicPixel.Value;
			sb.Draw(px, b, new Color(20, 22, 50) * 0.4f);
			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				PlayerInput.LockVanillaMouseScroll("GregTechCEuTerraria/StationCaps");
			}

			var items = _supplier();
			int sbW = Scrollbar.Width;
			int viewLines = Math.Max(1, b.Height / LineH);
			int maxScroll = Math.Max(0, items.Count - viewLines);
			var mouse = ModalEscape.PollCursor();
			bool showBar = items.Count > viewLines;
			Rectangle track = Rectangle.Empty, thumb = Rectangle.Empty;
			if (showBar)
			{
				track = new Rectangle(b.Right - sbW, b.Y, sbW, b.Height);
				thumb = _bar.Update(track, maxScroll, (float)viewLines / items.Count, ref _scroll, mouse);
			}
			if (_scroll > maxScroll) _scroll = maxScroll;

			var font = FontAssets.MouseText.Value;
			const float ts = 0.8f;
			float avail = b.Width - 8 - (showBar ? sbW + 2 : 0);
			for (int i = 0; i < viewLines; i++)
			{
				int idx = i + _scroll;
				if (idx >= items.Count) break;
				string s = items[idx];
				if (font.MeasureString(s).X * ts > avail)
				{
					while (s.Length > 1 && font.MeasureString(s + "...").X * ts > avail)
						s = s.Substring(0, s.Length - 1);
					s += "...";
				}
				Terraria.Utils.DrawBorderString(sb, s,
					new Vector2(b.X + 4, b.Y + 2 + i * LineH), _color, ts);
			}

			if (showBar) _bar.Draw(sb, track, thumb, mouse);
		}
	}
}
