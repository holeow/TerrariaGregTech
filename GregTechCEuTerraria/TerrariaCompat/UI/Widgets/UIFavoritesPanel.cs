#nullable enable
using System;
using GregTechCEuTerraria.Api.Fluids;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIFavoritesPanel : UITerrariaPanel
{
	private const int Cols = 4;
	private const int CellSize = 30;
	private const int CellPad = 2;
	private const int Margin = 6;
	private const int ScrollbarWidth = VanillaScrollbar.Width;
	private const int MinThumbHeight = 28;
	public const int PanelWidth = Cols * (CellSize + CellPad) + Margin * 2 + ScrollbarWidth;
	private const float VanillaNativeSlotPixels = 52f;

	public int ScrollBottomInset;

	private static readonly Item[] _slotItems = { new() };

	private int _scroll;
	private bool _leftDown;
	private bool _rightDown;
	private bool _drag;
	private int _dragAnchorOffsetPx;

	public Func<bool>? IsOccluded;

	public UIFavoritesPanel()
	{
		Width  = StyleDimension.FromPixels(PanelWidth);
	}

	public void SetHeight(float h) => Height = StyleDimension.FromPixels(h);

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);
		if (!IsMouseHovering) return;
		_scroll -= evt.ScrollWheelValue;
		if (_scroll < 0) _scroll = 0;
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		var outer = GetDimensions().ToRectangle();

		Terraria.Utils.DrawBorderString(sb, "Favorites",
			new Vector2(outer.X + 6, outer.Y + 4), new Color(220, 230, 255), 0.78f);

		var content = new Rectangle(
			outer.X + Margin,
			outer.Y + 22,
			outer.Width - Margin * 2 - ScrollbarWidth,
			outer.Height - 22 - Margin);

		var entries = FavoritesPlayer.Local.Entries;
		if (entries.Count == 0)
		{
			Terraria.Utils.DrawBorderString(sb, "Alt+click\nto pin",
				new Vector2(content.X + 4, content.Y + 8),
				new Color(140, 150, 180), 0.7f);
			_leftDown  = Main.mouseLeft;
			_rightDown = Main.mouseRight;
			return;
		}

		int totalRows = (entries.Count + Cols - 1) / Cols;
		int rowH = CellSize + CellPad;
		int viewH = content.Height;
		int maxScroll = System.Math.Max(0, totalRows * rowH - viewH);
		if (_scroll > maxScroll) _scroll = maxScroll;

		bool occluded = IsOccluded?.Invoke() ?? false;

		var mouse = GlobalRecipeBrowserState.BrowserCursor();
		bool inside = !occluded && content.Contains(mouse);
		if (IsMouseHovering && !occluded)
		{
			Main.LocalPlayer.mouseInterface = true;
			PlayerInput.LockVanillaMouseScroll("GregTechCEuTerraria/FavoritesPanel");
		}

		int totalH = totalRows * rowH;
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

			if (!occluded)
			{
				if (Main.mouseLeft && !_leftDown && trackRect.Contains(mouse))
				{
					_drag = true;
					_dragAnchorOffsetPx = thumbRect.Contains(mouse) ? mouse.Y - thumbY : thumbH / 2;
				}
				if (_drag && Main.mouseLeft)
				{
					int newThumbTop = mouse.Y - _dragAnchorOffsetPx;
					int travelMax = Math.Max(1, barH - thumbH);
					int clampedTop = Math.Clamp(newThumbTop - content.Y, 0, travelMax);
					_scroll = (int)((float)clampedTop / travelMax * maxScroll);
					draggingThisFrame = true;
				}
				else if (!Main.mouseLeft)
				{
					_drag = false;
				}
				if (draggingThisFrame)
				{
					thumbY = content.Y + (travel > 0 ? (int)(travel * ((float)_scroll / maxScroll)) : 0);
					thumbRect = new Rectangle(barX, thumbY, ScrollbarWidth, thumbH);
				}
			}
		}

		float oldScale = Main.inventoryScale;
		Main.inventoryScale = CellSize / VanillaNativeSlotPixels;
		FavoritesPlayer.Entry hovered = default;
		bool hasHovered = false;
		try
		{
			ScissorDraw.Draw(sb, ScissorDraw.DeviceClip(content), () =>
			{
			for (int i = 0; i < entries.Count; i++)
			{
				int col = i % Cols;
				int row = i / Cols;
				int yTop = content.Y - _scroll + row * rowH;
				if (yTop + CellSize < content.Y || yTop > content.Bottom) continue;

				var rect = new Rectangle(
					content.X + col * (CellSize + CellPad),
					yTop,
					CellSize, CellSize);

				var entry = entries[i];
				bool isHover = !draggingThisFrame && inside && rect.Contains(mouse);

				if (entry.ItemType > 0)
				{
					_slotItems[0].SetDefaults(entry.ItemType);
					if (isHover)
					{
						Main.LocalPlayer.mouseInterface = true;
						ItemSlot.OverrideHover(_slotItems, ItemSlot.Context.CraftingMaterial, 0);
						ItemSlot.MouseHover(_slotItems, ItemSlot.Context.CraftingMaterial, 0);
					}
					ItemSlot.Draw(sb, _slotItems, ItemSlot.Context.CraftingMaterial, 0,
						new Vector2(rect.X, rect.Y));
				}
				else if (entry.FluidId is not null)
				{
					var fluid = FluidRegistry.Get(entry.FluidId);
					BrowserFluidSlot.Draw(sb, rect, fluid,
						amountMb: 0, fallbackLabel: entry.FluidLabel);
					if (isHover)
					{
						Main.LocalPlayer.mouseInterface = true;
						BrowserFluidSlot.EmitTooltip(fluid, amountMb: 0,
							fallbackLabel: entry.FluidLabel);
					}
				}

				if (isHover)
				{
					hovered = entry;
					hasHovered = true;
					if (entry.ItemType > 0)
						BrowserHover.SetItem(entry.ItemType);
					else if (entry.FluidId is not null)
						BrowserHover.SetFluid(entry.FluidId, entry.FluidLabel ?? entry.FluidId);
				}
			}
			});
		}
		finally
		{
			Main.inventoryScale = oldScale;
		}

		if (hasHovered && !_drag)
		{
			var click = BrowserSlotInteraction.Poll(_leftDown, _rightDown);
			if (hovered.ItemType > 0)
				BrowserSlotInteraction.HandleItem(click, hovered.ItemType,
					inFavoritesPane: true);
			else if (hovered.FluidId is not null)
			{
				var fluid = FluidRegistry.Get(hovered.FluidId);
				if (fluid is not null)
					BrowserSlotInteraction.HandleFluid(click, fluid,
						recipeAmountMb: null, inFavoritesPane: true);
				else if (click.Alt && click.Lmb)
					FavoritesPlayer.Local.RemoveFluid(hovered.FluidId);
			}
		}
		_leftDown  = Main.mouseLeft;
		_rightDown = Main.mouseRight;

		if (totalH > viewH)
		{
			bool thumbHot = _drag || thumbRect.Contains(mouse);
			VanillaScrollbar.Draw(sb, trackRect, thumbRect, thumbHot);
			if (thumbHot) Main.LocalPlayer.mouseInterface = true;
		}
	}

}
