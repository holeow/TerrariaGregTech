#nullable enable
using System;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.Api.Cover.Filter;
using GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIMagnetPhantomSlot : UIElement
{
	public const int NativeUnscaledSize = 22;
	private const float VanillaNativeSlotPixels = 52f;

	private readonly Func<SimpleItemFilter?> _filter;
	private readonly int _index;
	private readonly Item[] _render = { new() };


	public UIMagnetPhantomSlot(Func<SimpleItemFilter?> filter, int index)
	{
		_filter = filter;
		_index = index;
		Width = StyleDimension.FromPixels(NativeUnscaledSize);
		Height = StyleDimension.FromPixels(NativeUnscaledSize);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var filter = _filter();
		_render[0] = filter is not null && _index < filter.Matches.Length
			? filter.Matches[_index] : new Item();

		var bounds = GetDimensions().ToRectangle();

		if (ItemDrag.TryDropItem(bounds, out int dropped) && filter is not null)
		{
			var it = new Item();
			it.SetDefaults(dropped);
			it.stack = 1;
			ItemFilterEdit.MatcherClick(filter, _index, 0, false, it);
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		float oldScale = Main.inventoryScale;
		Main.inventoryScale = bounds.Width / VanillaNativeSlotPixels;
		try
		{
			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				ItemSlot.MouseHover(_render, ItemSlot.Context.ChestItem, 0);
			}
			ItemSlot.Draw(spriteBatch, _render, ItemSlot.Context.ChestItem, 0, new Vector2(bounds.X, bounds.Y));
		}
		finally
		{
			Main.inventoryScale = oldScale;
		}
	}

	public override void LeftMouseDown(UIMouseEvent evt)  { base.LeftMouseDown(evt);  HandleLeft(); }
	public override void RightMouseDown(UIMouseEvent evt) { base.RightMouseDown(evt); HandleClear(); }

	private void HandleLeft()
	{
		var filter = _filter();
		if (filter is null) return;
		Item slot = _index < filter.Matches.Length ? filter.Matches[_index] : new Item();

		if (!Main.mouseItem.IsAir)
		{
			ItemFilterEdit.MatcherClick(filter, _index, 0, false, Main.mouseItem);
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}

		if (slot.IsAir)
		{
			ItemPickerSystem.OpenForItem(it => ItemFilterEdit.MatcherClick(filter, _index, 0, false, it));
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}

		if (filter.MaxStackSize <= 1) return;

		MeCraftSystem.OpenForAmount(AEItemKey.Of(slot)!, slot.stack, 1, "Set Amount", "Set",
			"Amount this slot matches", SetLocalAmount, parentLayer: UILayers.TopModal);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	private void SetLocalAmount(long amt)
	{
		var f = _filter();
		if (f is null || _index >= f.Matches.Length || f.Matches[_index].IsAir) return;
		f.Matches[_index].stack = (int)Math.Clamp(amt, 1, f.MaxStackSize);
		f.OnUpdated();
	}

	private void HandleClear()
	{
		var filter = _filter();
		if (filter is null) return;
		ItemFilterEdit.MatcherClick(filter, _index, 2, false, new Item());
		SoundEngine.PlaySound(SoundID.MenuTick);
	}
}
