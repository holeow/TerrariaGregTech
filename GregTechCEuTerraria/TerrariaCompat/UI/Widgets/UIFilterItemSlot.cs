#nullable enable
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIFilterItemSlot : UIElement
{
	public const int NativeUnscaledSize = 22;
	private const float VanillaNativeSlotPixels = 52f;

	private readonly ICoverable _entity;
	private readonly CoverSide _side;
	private readonly bool _fluid;
	private readonly Item[] _render = { new() };


	public UIFilterItemSlot(ICoverable entity, CoverSide side, bool fluid)
	{
		_entity = entity;
		_side = side;
		_fluid = fluid;
		Width = StyleDimension.FromPixels(NativeUnscaledSize);
		Height = StyleDimension.FromPixels(NativeUnscaledSize);
	}

	private Item FilterItem()
	{
		var cover = _entity.GetCoverAtSide(_side);
		return (_fluid ? cover?.UiFluidFilterHandler?.FilterItem
		               : cover?.UiItemFilterHandler?.FilterItem) ?? new Item();
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		_render[0] = FilterItem();

		var bounds = GetDimensions().ToRectangle();
		float oldScale = Main.inventoryScale;
		Main.inventoryScale = bounds.Width / VanillaNativeSlotPixels;
		try
		{
			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				ItemSlot.OverrideHover(_render, ItemSlot.Context.ChestItem, 0);
				ItemSlot.MouseHover(_render, ItemSlot.Context.ChestItem, 0);
			}
			ItemSlot.Draw(spriteBatch, _render, ItemSlot.Context.ChestItem, 0, new Vector2(bounds.X, bounds.Y));
		}
		finally
		{
			Main.inventoryScale = oldScale;
		}
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		if (_render[0].IsAir && Main.mouseItem.IsAir) return;
		CoverActions.Send(CoverFilterAction.FilterItem(_side, _fluid, Main.mouseItem), _entity);
		SoundEngine.PlaySound(SoundID.Grab);
	}
}
