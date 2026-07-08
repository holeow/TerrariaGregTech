#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Fluids;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIItemGrid : UIElement
{
	private const int CellSize = 36;
	private const int CellPad  = 2;
	private const int Margin   = 6;
	private const int ScrollbarWidth = Scrollbar.Width;
	private const int MinThumbHeight = 28;
	private const float VanillaNativeSlotPixels = 52f;

	private static readonly Dictionary<int, Item> _itemCache = new();
	private static readonly Item[] _drawSlot = { new() };

	private static bool _warmedVanilla;

	public static void WarmVanillaItemTextures()
	{
		if (_warmedVanilla || Main.dedServ) return;
		_warmedVanilla = true;
		for (int t = 1; t < ItemID.Count; t++)
		{
			var asset = TextureAssets.Item[t];
			if (asset != null && (int)asset.State == 0)
				Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);
		}
	}

	private static readonly IReadOnlyList<string> _noFluids = Array.Empty<string>();

	private readonly Func<IReadOnlyList<int>> _source;
	private readonly Func<IReadOnlyList<string>>? _fluidSource;
	private readonly string _emptyHint;
	private readonly Action<int>? _onPick;
	private readonly Action<string>? _onPickFluid;
	private int _scroll;
	private readonly Scrollbar _bar = new();

	public int ScrollBottomInset;

	public UIItemGrid(Func<IReadOnlyList<int>> source, string emptyHint = "No items match this search",
		Action<int>? onPick = null, Func<IReadOnlyList<string>>? fluidSource = null,
		Action<string>? onPickFluid = null)
	{
		_source = source;
		_emptyHint = emptyHint;
		_onPick = onPick;
		_fluidSource = fluidSource;
		_onPickFluid = onPickFluid;
	}

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);
		if (!IsMouseHovering) return;
		Scrollbar.Wheel(evt, ref _scroll);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		var outer = GetDimensions().ToRectangle();
		var px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, outer, new Color(20, 22, 50) * 0.45f);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			PlayerInput.LockVanillaMouseScroll("GregTechCEuTerraria/ItemGrid");
			HoverItemTracker.SuppressNextHoverPick();
		}

		var content = new Rectangle(
			outer.X + Margin,
			outer.Y + Margin,
			outer.Width - Margin * 2 - ScrollbarWidth,
			outer.Height - Margin * 2);

		var src = _source();
		var fluids = _fluidSource?.Invoke() ?? _noFluids;
		int total = src.Count + fluids.Count;
		if (total == 0)
		{
			Terraria.Utils.DrawBorderString(sb, _emptyHint,
				new Vector2(content.X + 8, content.Y + 8),
				Color.LightGray, 0.85f);
			return;
		}

		int step = CellSize + CellPad;
		int cols = Math.Max(1, (content.Width + CellPad) / step);
		int rows = (total + cols - 1) / cols;
		int totalH = rows * step;
		int viewH = content.Height;
		int maxScroll = Math.Max(0, totalH - viewH);
		if (_scroll > maxScroll) _scroll = maxScroll;

		var mouse = ModalEscape.PollCursor();

		Rectangle trackRect = Rectangle.Empty, thumbRect = Rectangle.Empty;
		if (totalH > viewH)
		{
			int barX = outer.Right - Margin - ScrollbarWidth;
			int barH = Math.Max(MinThumbHeight, content.Height - ScrollBottomInset);
			trackRect = new Rectangle(barX, content.Y, ScrollbarWidth, barH);
			thumbRect = _bar.Update(trackRect, maxScroll, (float)viewH / totalH, ref _scroll, mouse);
		}
		bool draggingThisFrame = _bar.Dragging;

		bool inside = content.Contains(mouse);

		int firstRow = _scroll / step;
		int lastRow  = Math.Min(rows - 1, (_scroll + viewH - 1) / step);

		int hoveredType = 0;
		string? hoveredFluid = null;

		float oldScale = Main.inventoryScale;
		Main.inventoryScale = CellSize / VanillaNativeSlotPixels;
		try
		{
			ScissorDraw.Draw(sb, ScissorDraw.DeviceClip(content), () =>
			{
			for (int r = firstRow; r <= lastRow; r++)
			{
				int yTop = content.Y - _scroll + r * step;
				for (int c = 0; c < cols; c++)
				{
					int idx = r * cols + c;
					if (idx >= total) break;
					int xLeft = content.X + c * step;
					var rect = new Rectangle(xLeft, yTop, CellSize, CellSize);
					if (rect.Bottom < content.Y || rect.Y > content.Bottom) continue;

					bool isHover = !draggingThisFrame && inside && rect.Contains(mouse);

					if (idx < src.Count)
					{
						int itemType = src[idx];
						if (!_itemCache.TryGetValue(itemType, out var cached))
						{
							cached = new Item();
							cached.SetDefaults(itemType);
							_itemCache[itemType] = cached;
						}
						_drawSlot[0] = cached;
						if (isHover)
						{
							hoveredType = itemType;
							ItemSlot.OverrideHover(_drawSlot, ItemSlot.Context.CraftingMaterial, 0);
							ItemSlot.MouseHover(_drawSlot, ItemSlot.Context.CraftingMaterial, 0);
						}
						ItemSlot.Draw(sb, _drawSlot, ItemSlot.Context.CraftingMaterial, 0,
							new Vector2(rect.X, rect.Y));
					}
					else
					{
						string fluidId = fluids[idx - src.Count];
						BrowserFluidSlot.Draw(sb, rect, FluidRegistry.Get(fluidId));
						if (isHover) hoveredFluid = fluidId;
					}
				}
			}
			});
		}
		finally
		{
			Main.inventoryScale = oldScale;
		}

		if (hoveredType > 0)
		{
			Main.LocalPlayer.mouseInterface = true;
			BrowserHover.SetItem(hoveredType);

			if (_onPick != null)
			{
				if (MouseClick.LeftPressed)
					_onPick(hoveredType);
			}
			else
			{
				ItemDrag.ArmFromHover(hoveredType, null, null);
				var click = ItemDrag.Active ? default : BrowserSlotInteraction.PollReleased();
				BrowserSlotInteraction.HandleItem(click, hoveredType, inFavoritesPane: false);
			}
		}
		else if (hoveredFluid != null)
		{
			Main.LocalPlayer.mouseInterface = true;
			var fluid = FluidRegistry.Get(hoveredFluid);
			BrowserHover.SetFluid(hoveredFluid, fluid?.DisplayName ?? hoveredFluid);
			BrowserFluidSlot.EmitTooltip(fluid, 0, hoveredFluid);

			if (_onPickFluid != null)
			{
				if (MouseClick.LeftPressed)
					_onPickFluid(hoveredFluid);
			}
			else if (fluid != null)
			{
				ItemDrag.ArmFromHover(0, hoveredFluid, fluid.DisplayName);
				var click = ItemDrag.Active ? default : BrowserSlotInteraction.PollReleased();
				BrowserSlotInteraction.HandleFluid(click, fluid, null, inFavoritesPane: false);
			}
		}

		if (totalH > viewH)
			_bar.Draw(sb, trackRect, thumbRect, mouse);
	}

}
