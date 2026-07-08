#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIPhantomItemSlot : UIElement
{
	public const int NativeUnscaledSize = 22;
	private const float VanillaNativeSlotPixels = 52f;

	private readonly ICoverable _entity;
	private readonly CoverSide _side;
	private readonly int _index;
	private readonly Item[] _render = { new() };

	private bool AmountMatters => (_entity.GetCoverAtSide(_side)?.UiItemFilter?.MaxStackSize ?? 1) > 1;


	public UIPhantomItemSlot(ICoverable entity, CoverSide side, int index)
	{
		_entity = entity;
		_side = side;
		_index = index;
		Width = StyleDimension.FromPixels(NativeUnscaledSize);
		Height = StyleDimension.FromPixels(NativeUnscaledSize);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var filter = _entity.GetCoverAtSide(_side)?.UiItemFilter;
		var src = filter is not null && _index < filter.Matches.Length
			? filter.Matches[_index] : new Item();
		if (!AmountMatters && !src.IsAir && src.stack != 1)
		{
			var clone = src.Clone();
			clone.stack = 1;
			_render[0] = clone;
		}
		else _render[0] = src;

		var bounds = GetDimensions().ToRectangle();

		if (ItemDrag.TryDropItem(bounds, out int dropped))
		{
			var it = new Item();
			it.SetDefaults(dropped);
			it.stack = 1;
			CoverActions.Send(CoverFilterAction.Matcher(_side, fluid: false, _index, 0, false, it), _entity);
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		float oldScale = Main.inventoryScale;
		Main.inventoryScale = bounds.Width / VanillaNativeSlotPixels;
		try
		{
			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				ShowTooltip();
			}
			ItemSlot.Draw(spriteBatch, _render, ItemSlot.Context.ChestItem, 0, new Vector2(bounds.X, bounds.Y));
		}
		finally
		{
			Main.inventoryScale = oldScale;
		}
	}

	private void ShowTooltip()
	{
		var slot = _render[0];
		bool amountShown = AmountMatters;
		string tip = slot.IsAir
			? PhantomSlotChrome.EmptyTooltip(PhantomSlotChrome.Kind.Item)
			: PhantomSlotChrome.FilledTooltip(slot.Name, amountShown ? "amount: " + slot.stack : null, amountShown);
		Main.instance.MouseText(tip);
	}

	public override void LeftMouseDown(UIMouseEvent evt)  { base.LeftMouseDown(evt);  HandleLeft(); }
	public override void RightMouseDown(UIMouseEvent evt) { base.RightMouseDown(evt); HandleClear(); }

	private void HandleLeft()
	{
		var filter = _entity.GetCoverAtSide(_side)?.UiItemFilter;
		Item slot = filter is not null && _index < filter.Matches.Length ? filter.Matches[_index] : new Item();

		if (!Main.mouseItem.IsAir)
		{
			CoverActions.Send(
				CoverFilterAction.Matcher(_side, fluid: false, _index, 0, false, Main.mouseItem), _entity);
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}

		if (slot.IsAir)
		{
			ItemPickerSystem.OpenForItem(
				it => CoverActions.Send(CoverFilterAction.Matcher(_side, fluid: false, _index, 0, false, it), _entity));
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}

		if (!AmountMatters) return;

		MeCraftSystem.OpenForAmount(AEItemKey.Of(slot)!, slot.stack, 1, "Set Amount", "Set",
			"Amount this slot matches",
			amt => CoverActions.Send(CoverFilterAction.MatcherSetAmount(_side, fluid: false, _index, amt), _entity),
			parentLayer: UILayers.TopModal);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	private void HandleClear()
	{
		CoverActions.Send(
			CoverFilterAction.Matcher(_side, fluid: false, _index, 2, false, new Item()), _entity);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}
}
