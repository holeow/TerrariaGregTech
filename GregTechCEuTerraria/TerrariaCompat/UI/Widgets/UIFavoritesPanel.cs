#nullable enable
using System;
using System.Collections.Generic;
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
	private const int CellSize = 36;
	private const int CellPad = 2;
	private const int Margin = 6;
	private const int ScrollbarWidth = Scrollbar.Width;
	private const int MinThumbHeight = 28;
	public const int PanelWidth = Cols * (CellSize + CellPad) + Margin * 2 + ScrollbarWidth;
	private const float VanillaNativeSlotPixels = 52f;

	public int ScrollBottomInset;
	public bool DrawBackground = true;
	public bool HideWhenDocked;
	public bool HideWhenMagicStorageOpen;

	private static readonly Item[] _slotItems = { new() };

	private int _scroll;
	private readonly Scrollbar _bar = new();

	public Action<int>? OnPickItem;
	public Action<string>? OnPickFluid;

	public Func<IReadOnlyList<FavoritesPlayer.Entry>>? EntriesSource;
	public string Title = "Favorites";
	public string EmptyHint = "Alt+click\nto pin";
	public bool IsFavoritesPane = true;

	public UIFavoritesPanel()
	{
		Width  = StyleDimension.FromPixels(PanelWidth);
	}

	public void SetHeight(float h) => Height = StyleDimension.FromPixels(h);

	private static void DrawTagBorder(SpriteBatch sb, Rectangle r)
	{
		var c = new Color(190, 130, 245);
		var px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, 2), c);
		sb.Draw(px, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), c);
		sb.Draw(px, new Rectangle(r.X, r.Y, 2, r.Height), c);
		sb.Draw(px, new Rectangle(r.Right - 2, r.Y, 2, r.Height), c);
	}

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);
		if (!IsMouseHovering) return;
		Scrollbar.Wheel(evt, ref _scroll);
	}

	private bool Hidden =>
		(HideWhenDocked && GlobalRecipeBrowserSystem.DockedActive)
		|| (HideWhenMagicStorageOpen && MagicStorageUi.IsOpen);

	public override bool ContainsPoint(Vector2 point) => !Hidden && base.ContainsPoint(point);

	protected override void DrawSelf(SpriteBatch sb)
	{
		if (Hidden) return;
		if (DrawBackground) base.DrawSelf(sb);

		var outer = GetDimensions().ToRectangle();

		Terraria.Utils.DrawBorderString(sb, Title,
			new Vector2(outer.X + 6, outer.Y + 4), new Color(220, 230, 255), 0.78f);

		var content = new Rectangle(
			outer.X + Margin,
			outer.Y + 22,
			outer.Width - Margin * 2 - ScrollbarWidth,
			outer.Height - 22 - Margin);

		int cols = Math.Max(1, (content.Width + CellPad) / (CellSize + CellPad));

		var entries = EntriesSource?.Invoke() ?? FavoritesPlayer.Local.Entries;
		if (entries.Count == 0)
		{
			Terraria.Utils.DrawBorderString(sb, EmptyHint,
				new Vector2(content.X + 4, content.Y + 8),
				new Color(140, 150, 180), 0.7f);
			return;
		}

		int totalRows = (entries.Count + cols - 1) / cols;
		int rowH = CellSize + CellPad;
		int viewH = content.Height;
		int maxScroll = System.Math.Max(0, totalRows * rowH - viewH);
		if (_scroll > maxScroll) _scroll = maxScroll;

		var mouse = ModalEscape.PollCursor();
		bool inside = content.Contains(mouse);
		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			PlayerInput.LockVanillaMouseScroll("GregTechCEuTerraria/FavoritesPanel");
		}

		int totalH = totalRows * rowH;
		Rectangle trackRect = Rectangle.Empty, thumbRect = Rectangle.Empty;
		if (totalH > viewH)
		{
			int barX = outer.Right - Margin - ScrollbarWidth;
			int barH = Math.Max(MinThumbHeight, content.Height - ScrollBottomInset);
			trackRect = new Rectangle(barX, content.Y, ScrollbarWidth, barH);
			thumbRect = _bar.Update(trackRect, maxScroll, (float)viewH / totalH, ref _scroll, mouse);
		}
		bool draggingThisFrame = _bar.Dragging;

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
				int col = i % cols;
				int row = i / cols;
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
				else if (entry.TagLabel is not null && entry.TagMembers is { Length: > 0 } tagMem)
				{
					_slotItems[0].SetDefaults(tagMem[0]);
					ItemSlot.Draw(sb, _slotItems, ItemSlot.Context.CraftingMaterial, 0,
						new Vector2(rect.X, rect.Y));
					DrawTagBorder(sb, rect);
					if (isHover)
					{
						Main.LocalPlayer.mouseInterface = true;
						Main.instance.MouseText(entry.TagLabel);
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
					else if (entry.TagLabel is not null && entry.TagMembers is not null)
						BrowserHover.SetTag(entry.TagLabel, new HashSet<int>(entry.TagMembers));
				}
			}
			});
		}
		finally
		{
			Main.inventoryScale = oldScale;
		}

		if (hasHovered && !_bar.Dragging)
		{
			if (MouseClick.LeftPressed)
			{
				if (hovered.ItemType > 0) ItemDrag.ArmItem(hovered.ItemType);
				else if (hovered.FluidId is not null) ItemDrag.ArmFluid(hovered.FluidId, hovered.FluidLabel);
			}
			var click = ItemDrag.Active ? default : BrowserSlotInteraction.PollReleased();
			if (OnPickItem != null)
			{
				if (hovered.ItemType > 0)
				{
					if (click.Alt && click.Lmb) FavoritesPlayer.Local.RemoveItem(hovered.ItemType);
					else if (click.Lmb) OnPickItem(hovered.ItemType);
				}
				else if (hovered.FluidId is not null)
				{
					if (click.Alt && click.Lmb) FavoritesPlayer.Local.RemoveFluid(hovered.FluidId);
					else if (click.Lmb && OnPickFluid != null) OnPickFluid(hovered.FluidId);
				}
			}
			else if (hovered.ItemType > 0)
				BrowserSlotInteraction.HandleItem(click, hovered.ItemType,
					inFavoritesPane: IsFavoritesPane);
			else if (hovered.FluidId is not null)
			{
				var fluid = FluidRegistry.Get(hovered.FluidId);
				if (fluid is not null)
					BrowserSlotInteraction.HandleFluid(click, fluid,
						recipeAmountMb: null, inFavoritesPane: IsFavoritesPane);
				else if (click.Alt && click.Lmb)
					FavoritesPlayer.Local.RemoveFluid(hovered.FluidId);
			}
			else if (hovered.TagLabel is not null)
			{
				var mem = new HashSet<int>(hovered.TagMembers ?? System.Array.Empty<int>());
				if (click.Alt && click.Lmb)
				{
					if (IsFavoritesPane) FavoritesPlayer.Local.RemoveTag(hovered.TagLabel);
					else                 FavoritesPlayer.Local.BringTagToFront(hovered.TagLabel, mem);
				}
				else if (click.Lmb || click.Rmb)
					BrowserSlotInteraction.HandleTag(click, hovered.TagLabel, mem);
			}
		}
		if (totalH > viewH)
			_bar.Draw(sb, trackRect, thumbRect, mouse);
	}

}
