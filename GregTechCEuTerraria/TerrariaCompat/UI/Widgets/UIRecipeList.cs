#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIRecipeList : UIElement
{
	private readonly Func<IReadOnlyList<GTRecipe>> _sourceProvider;
	private readonly string _emptyHint;

	public Action<GTRecipe>? OnSelectRecipe;

	public Action<string>? OnStationFilter;

	private bool _awaitRelease;
	public void IgnoreHeldClick() => _awaitRelease = true;

	private int _scrollOffsetPx;

	public int ScrollOffsetPx { get => _scrollOffsetPx; set => _scrollOffsetPx = value < 0 ? 0 : value; }

	public int ScrollBottomInset;

	private readonly Scrollbar _bar = new();

	private const int ScrollbarWidth = Scrollbar.Width;
	private const int Margin = 6;
	private const int MinThumbHeight = 28;
	private const int SelectGutter = RecipeRowRenderer.SelectGutter;

	public UIRecipeList(Func<IReadOnlyList<GTRecipe>> sourceProvider, string emptyHint = "No recipes")
	{
		_sourceProvider = sourceProvider;
		_emptyHint = emptyHint;
	}

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);
		Scrollbar.Wheel(evt, ref _scrollOffsetPx);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var outer = GetDimensions().ToRectangle();
		var px = TextureAssets.MagicPixel.Value;

		if (_awaitRelease && !Main.mouseLeft) _awaitRelease = false;

		spriteBatch.Draw(px, outer, new Color(20, 22, 50) * 0.45f);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			PlayerInput.LockVanillaMouseScroll("GregTechCEuTerraria/RecipeBrowser");
			HoverItemTracker.SuppressNextHoverPick();
		}

		var content = new Rectangle(
			outer.X + Margin,
			outer.Y + Margin,
			outer.Width - Margin * 2 - ScrollbarWidth,
			outer.Height - Margin * 2);

		var src = _sourceProvider();
		if (src.Count == 0)
		{
			Terraria.Utils.DrawBorderString(spriteBatch, _emptyHint,
				new Vector2(content.X + 8, content.Y + 8),
				Color.LightGray, 0.85f);
			return;
		}

		int rowH = RecipeRowRenderer.RowHeight;
		int totalH = src.Count * rowH;
		int viewH = content.Height;
		int maxOffset = Math.Max(0, totalH - viewH);
		var mouse = ModalEscape.PollCursor();

		Rectangle trackRect = Rectangle.Empty, thumbRect = Rectangle.Empty;
		if (totalH > viewH)
		{
			int barX = outer.Right - Margin - ScrollbarWidth;
			int barH = Math.Max(MinThumbHeight, content.Height - ScrollBottomInset);
			trackRect = new Rectangle(barX, content.Y, ScrollbarWidth, barH);
			thumbRect = _bar.Update(trackRect, maxOffset, (float)viewH / totalH, ref _scrollOffsetPx, mouse);
		}
		bool draggingThisFrame = _bar.Dragging;
		if (_scrollOffsetPx > maxOffset) _scrollOffsetPx = maxOffset;

		int firstRow = _scrollOffsetPx / rowH;
		int lastRow = Math.Min(src.Count - 1, (_scrollOffsetPx + viewH - 1) / rowH);

		int hoveredRow = -1;
		bool picked = false;
		int selGutter = OnSelectRecipe != null ? SelectGutter : 0;
		ScissorDraw.Draw(spriteBatch, ScissorDraw.DeviceClip(content), () =>
		{
		for (int i = firstRow; i <= lastRow; i++)
		{
			int yTop = content.Y - _scrollOffsetPx + i * rowH;
			var rowBounds = new Rectangle(content.X, yTop, content.Width, rowH);
			var contentBounds = selGutter > 0
				? new Rectangle(rowBounds.X + selGutter, yTop, rowBounds.Width - selGutter, rowH)
				: rowBounds;

			bool rowHovered = !draggingThisFrame && rowBounds.Contains(mouse) && content.Contains(mouse);
			if (rowHovered)
			{
				spriteBatch.Draw(px, rowBounds, new Color(100, 130, 200) * 0.25f);
				hoveredRow = i;
			}

			RecipeRowRenderer.Draw(spriteBatch, contentBounds, src[i], Color.White);

			if (selGutter > 0)
			{
				var selBtn = RecipeRowRenderer.SelectButtonRect(rowBounds);
				bool overSel = !draggingThisFrame && content.Contains(mouse) && selBtn.Contains(mouse);
				RecipeRowRenderer.DrawSelectButton(spriteBatch, selBtn, overSel);
				if (overSel)
				{
					Main.LocalPlayer.mouseInterface = true;
					Main.instance.MouseText("Select this recipe");
					if (!_awaitRelease && MouseClick.LeftPressed && !_bar.Dragging)
					{
						OnSelectRecipe!(src[i]);
						picked = true;
						break;
					}
				}
			}
		}
		});

		if (hoveredRow >= 0 && !picked)
		{
			Main.LocalPlayer.mouseInterface = true;
			int hyTop = content.Y - _scrollOffsetPx + hoveredRow * rowH;
			var rowBounds = selGutter > 0
				? new Rectangle(content.X + selGutter, hyTop, content.Width - selGutter, rowH)
				: new Rectangle(content.X, hyTop, content.Width, rowH);

			var craftRecipe = RecipeRowRenderer.FindAvailableVanillaCraft(src[hoveredRow]);
			var craftBtn    = craftRecipe != null
				? RecipeRowRenderer.CraftButtonRect(rowBounds)
				: Rectangle.Empty;
			bool overCraft  = craftRecipe != null && craftBtn.Contains(mouse);

			if (overCraft)
			{
				int qty = ItemSlot.ShiftInUse ? 10 : 1;
				Main.instance.MouseText($"Craft {qty}x {craftRecipe!.createItem.Name}");
				if (MouseClick.LeftPressed && !_bar.Dragging)
				{
					for (int n = 0; n < qty; n++)
					{
						Terraria.Recipe.FindRecipes(canDelayCheck: false);
						bool stillAvailable = false;
						for (int i = 0; i < Main.numAvailableRecipes; i++)
							if (Main.availableRecipe[i] >= 0 &&
							    ReferenceEquals(Main.recipe[Main.availableRecipe[i]], craftRecipe))
							{ stillAvailable = true; break; }
						if (!stillAvailable) break;

						if (Main.mouseItem.stack > 0
						    && Main.mouseItem.type != craftRecipe.createItem.type)
							break;

						Main.CraftItem(craftRecipe);
					}
				}
			}
			else
			{
				RecipeRowRenderer.HandleQueryClick(src[hoveredRow], rowBounds, mouse,
					_bar.Dragging, !_awaitRelease && !_bar.Dragging, OnStationFilter);
			}
		}
		if (totalH > viewH)
			_bar.Draw(spriteBatch, trackRect, thumbRect, mouse);
	}
}
