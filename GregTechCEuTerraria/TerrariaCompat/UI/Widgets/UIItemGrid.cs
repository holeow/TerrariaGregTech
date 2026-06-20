#nullable enable
using System;
using System.Collections.Generic;
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
	private const int ScrollbarWidth = VanillaScrollbar.Width;
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

	private readonly Func<IReadOnlyList<int>> _source;
	private readonly string _emptyHint;
	private readonly Action<int>? _onPick;
	private int _scroll;
	private bool _leftDown, _rightDown;
	private bool _pickArmed;
	private bool _dragging;
	private int _dragAnchorOffsetPx;

	public int ScrollBottomInset;

	public UIItemGrid(Func<IReadOnlyList<int>> source, string emptyHint = "No items match this search",
		Action<int>? onPick = null)
	{
		_source = source;
		_emptyHint = emptyHint;
		_onPick = onPick;
	}

	public void ResetPickArming()
	{
		_pickArmed = false;
		_leftDown = Main.mouseLeft;
		_rightDown = Main.mouseRight;
	}

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);
		if (!IsMouseHovering) return;
		_scroll -= evt.ScrollWheelValue;
		if (_scroll < 0) _scroll = 0;
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
		if (src.Count == 0)
		{
			Terraria.Utils.DrawBorderString(sb, _emptyHint,
				new Vector2(content.X + 8, content.Y + 8),
				Color.LightGray, 0.85f);
			_leftDown  = Main.mouseLeft;
			_rightDown = Main.mouseRight;
			return;
		}

		int step = CellSize + CellPad;
		int cols = Math.Max(1, (content.Width + CellPad) / step);
		int rows = (src.Count + cols - 1) / cols;
		int totalH = rows * step;
		int viewH = content.Height;
		int maxScroll = Math.Max(0, totalH - viewH);
		if (_scroll > maxScroll) _scroll = maxScroll;

		var mouse = GlobalRecipeBrowserState.BrowserCursor();

		bool draggingThisFrame = false;
		Rectangle trackRect = Rectangle.Empty, thumbRect = Rectangle.Empty;
		if (totalH > viewH)
		{
			int barX = outer.Right - Margin - ScrollbarWidth;
			int barH = Math.Max(MinThumbHeight, content.Height - ScrollBottomInset);
			trackRect = new Rectangle(barX, content.Y, ScrollbarWidth, barH);
			float frac = (float)viewH / totalH;
			int thumbH = Math.Max(MinThumbHeight, (int)(barH * frac));
			int travel = barH - thumbH;
			int thumbY = content.Y + (travel > 0 ? (int)(travel * ((float)_scroll / maxScroll)) : 0);
			thumbRect = new Rectangle(barX, thumbY, ScrollbarWidth, thumbH);

			if (Main.mouseLeft && !_leftDown && trackRect.Contains(mouse))
			{
				_dragging = true;
				_dragAnchorOffsetPx = thumbRect.Contains(mouse)
					? mouse.Y - thumbY
					: thumbH / 2;
			}
			if (_dragging && Main.mouseLeft)
			{
				int newThumbTop = mouse.Y - _dragAnchorOffsetPx;
				int travelMax = Math.Max(1, barH - thumbH);
				int clampedTop = Math.Clamp(newThumbTop - content.Y, 0, travelMax);
				_scroll = (int)((float)clampedTop / travelMax * maxScroll);
				draggingThisFrame = true;
			}
			else if (!Main.mouseLeft)
			{
				_dragging = false;
			}
			if (draggingThisFrame)
			{
				thumbY = content.Y + (travel > 0 ? (int)(travel * ((float)_scroll / maxScroll)) : 0);
				thumbRect = new Rectangle(barX, thumbY, ScrollbarWidth, thumbH);
			}
		}

		bool inside = content.Contains(mouse);

		int firstRow = _scroll / step;
		int lastRow  = Math.Min(rows - 1, (_scroll + viewH - 1) / step);

		int hoveredType = 0;
		Rectangle hoveredRect = Rectangle.Empty;

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
					if (idx >= src.Count) break;
					int xLeft = content.X + c * step;
					var rect = new Rectangle(xLeft, yTop, CellSize, CellSize);
					if (rect.Bottom < content.Y || rect.Y > content.Bottom) continue;

					int itemType = src[idx];
					if (!_itemCache.TryGetValue(itemType, out var cached))
					{
						cached = new Item();
						cached.SetDefaults(itemType);
						_itemCache[itemType] = cached;
					}
					_drawSlot[0] = cached;

					bool isHover = !draggingThisFrame && inside && rect.Contains(mouse);
					if (isHover)
					{
						hoveredType = itemType;
						hoveredRect = rect;
						ItemSlot.OverrideHover(_drawSlot, ItemSlot.Context.CraftingMaterial, 0);
						ItemSlot.MouseHover(_drawSlot, ItemSlot.Context.CraftingMaterial, 0);
					}

					ItemSlot.Draw(sb, _drawSlot, ItemSlot.Context.CraftingMaterial, 0,
						new Vector2(rect.X, rect.Y));
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
				if (!Main.mouseLeft) _pickArmed = true;
				if (_pickArmed && Main.mouseLeft && !_leftDown)
					_onPick(hoveredType);
			}
			else
			{
				var click = BrowserSlotInteraction.Poll(_leftDown, _rightDown);
				BrowserSlotInteraction.HandleItem(click, hoveredType, inFavoritesPane: false);
			}
		}

		_leftDown = Main.mouseLeft;
		_rightDown = Main.mouseRight;

		if (totalH > viewH)
		{
			bool thumbHot = _dragging || thumbRect.Contains(mouse);
			VanillaScrollbar.Draw(sb, trackRect, thumbRect, thumbHot);
			if (thumbHot) Main.LocalPlayer.mouseInterface = true;
		}
	}

}
