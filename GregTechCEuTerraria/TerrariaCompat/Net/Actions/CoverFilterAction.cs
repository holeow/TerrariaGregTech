#nullable enable
using System;
using System.IO;
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.Api.Cover.Filter;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class CoverFilterAction : ICoverAction
{
	public enum Op : byte
	{
		MatcherClick    = 0,
		ToggleBlacklist = 1,
		ToggleIgnoreNbt = 2,
		FilterSlot      = 3,
		SetTagExpr      = 4,
		SetFilterType   = 5,
		MatcherSetFluid = 6,
		MatcherSetAmount = 7,
	}

	public PacketType Type => PacketType.CoverFilter;

	private CoverSide _side;
	private Op _op;
	private bool _fluid;
	private int _index;
	private byte _button;
	private bool _shift;
	private Item _held = new();
	private string _text = "";
	private long _amount;

	public CoverFilterAction() { }

	public static CoverFilterAction Matcher(CoverSide side, bool fluid, int index, int button, bool shift, Item held) =>
		new() { _side = side, _op = Op.MatcherClick, _fluid = fluid, _index = index,
		        _button = (byte)button, _shift = shift, _held = held.Clone() };

	public static CoverFilterAction Toggle(CoverSide side, bool fluid, Op toggleOp) =>
		new() { _side = side, _op = toggleOp, _fluid = fluid };

	public static CoverFilterAction FilterItem(CoverSide side, bool fluid, Item held) =>
		new() { _side = side, _op = Op.FilterSlot, _fluid = fluid, _held = held.Clone() };

	public static CoverFilterAction TagExpr(CoverSide side, bool fluid, string expr) =>
		new() { _side = side, _op = Op.SetTagExpr, _fluid = fluid, _text = expr ?? "" };

	public static CoverFilterAction SetType(CoverSide side, bool fluid, int type) =>
		new() { _side = side, _op = Op.SetFilterType, _fluid = fluid, _index = type };

	public static CoverFilterAction MatcherSetFluid(CoverSide side, int index, string fluidId) =>
		new() { _side = side, _op = Op.MatcherSetFluid, _fluid = true, _index = index, _text = fluidId ?? "" };

	public static CoverFilterAction MatcherSetAmount(CoverSide side, bool fluid, int index, long amount) =>
		new() { _side = side, _op = Op.MatcherSetAmount, _fluid = fluid, _index = index, _amount = amount };

	public void Write(BinaryWriter w)
	{
		w.Write((byte)_side);
		w.Write((byte)_op);
		w.Write(_fluid);
		w.Write(_index);
		w.Write(_button);
		w.Write(_shift);
		w.WriteItem(_held);
		w.Write(_text);
		w.Write(_amount);
	}

	public void Read(BinaryReader r)
	{
		_side = (CoverSide)r.ReadByte();
		_op = (Op)r.ReadByte();
		_fluid = r.ReadBoolean();
		_index = r.ReadInt32();
		_button = r.ReadByte();
		_shift = r.ReadBoolean();
		_held = r.ReadItem();
		_text = r.ReadString();
		_amount = r.ReadInt64();
	}

	public void Apply(ICoverable target, int byWhoAmI)
	{
		if (_op == Op.SetFilterType)
		{
			if (target is TerrariaCompat.Pipelike.PipeCoverable pipe)
			{
				var t = (TerrariaCompat.Pipelike.PipeCoverable.PipeFilterType)
					System.Math.Clamp(_index, 0, 2);
				pipe.SetFilterType(_side, t);
			}
			return;
		}

		var cover = target.GetCoverAtSide(_side);
		if (cover is null) return;

		switch (_op)
		{
			case Op.MatcherSetFluid:
				if (cover.UiFluidFilter is { } fset && _index >= 0 && _index < fset.Matches.Length)
				{
					var fluid = FluidRegistry.Get(_text);
					if (fluid is not null) fset.Matches[_index] = new FluidStack(fluid, 1000);
				}
				break;

			case Op.MatcherSetAmount:
				if (_fluid)
				{
					if (cover.UiFluidFilter is { } fa && _index >= 0 && _index < fa.Matches.Length && !fa.Matches[_index].IsEmpty)
					{
						fa.Matches[_index] = fa.Matches[_index].WithAmount((int)Math.Clamp(_amount, 1, fa.MaxStackSize));
						fa.OnUpdated();
					}
				}
				else if (cover.UiItemFilter is { } ia && _index >= 0 && _index < ia.Matches.Length && !ia.Matches[_index].IsAir)
				{
					ia.Matches[_index].stack = (int)Math.Clamp(_amount, 1, ia.MaxStackSize);
					ia.OnUpdated();
				}
				break;

			case Op.MatcherClick:
				if (_fluid)
				{
					if (cover.UiFluidFilter is { } ff) FluidMatcherClick(ff, _index, _button, _shift, HeldFluid(_held));
				}
				else if (cover.UiItemFilter is { } itf)
				{
					ItemFilterEdit.MatcherClick(itf, _index, _button, _shift, _held);
				}
				break;

			case Op.ToggleBlacklist:
				if (_fluid) cover.UiFluidFilter?.SetBlackList(!cover.UiFluidFilter.IsBlackList);
				else        cover.UiItemFilter?.SetBlackList(!cover.UiItemFilter.IsBlackList);
				break;

			case Op.ToggleIgnoreNbt:
				if (_fluid) cover.UiFluidFilter?.SetIgnoreNbt(!cover.UiFluidFilter.IgnoreNbt);
				else        cover.UiItemFilter?.SetIgnoreNbt(!cover.UiItemFilter.IgnoreNbt);
				break;

			case Op.FilterSlot:
				if (_fluid) FilterSlotSwap(cover.UiFluidFilterHandler, byWhoAmI);
				else        FilterSlotSwap(cover.UiItemFilterHandler, byWhoAmI);
				break;

			case Op.SetTagExpr:
			{
				TagFilter? tag = _fluid ? (TagFilter?)cover.UiTagFluidFilter
				                        : (TagFilter?)cover.UiTagItemFilter;
				tag?.SetOreDict(_text);
				break;
			}
		}
	}

	private static void FluidMatcherClick(SimpleFluidFilter filter, int index, int button, bool shift, FluidStack held)
	{
		if (index < 0 || index >= filter.Matches.Length) return;
		FluidStack slot = filter.Matches[index];

		if (button == 2)
			filter.Matches[index] = FluidStack.Empty;
		else if (button == 0 || button == 1)
		{
			if (slot.IsEmpty)
			{
				if (!held.IsEmpty) filter.Matches[index] = FluidFill(held, button, filter.MaxStackSize);
			}
			else if (held.IsEmpty)
				filter.Matches[index] = FluidAdjust(slot, button, shift, filter.MaxStackSize);
			else
				filter.Matches[index] = FluidFill(held, button, filter.MaxStackSize);
		}
		filter.OnUpdated();
	}

	private static FluidStack FluidFill(FluidStack held, int button, int maxStack)
	{
		int amount = Math.Clamp(button == 0 ? held.Amount : 1, 1, maxStack);
		return held.WithAmount(amount);
	}

	private static FluidStack FluidAdjust(FluidStack slot, int button, bool shift, int maxStack)
	{
		int cur = slot.Amount;
		int next = shift ? (button == 0 ? (cur + 1) / 2 : cur * 2)
		                 : (button == 0 ? cur - 1 : cur + 1);
		next = Math.Min(next, maxStack);
		return next <= 0 ? FluidStack.Empty : slot.WithAmount(next);
	}

	private void FilterSlotSwap(ItemFilterHandler? handler, int byWhoAmI) => FilterSlotSwapImpl(handler, byWhoAmI);
	private void FilterSlotSwap(FluidFilterHandler? handler, int byWhoAmI) => FilterSlotSwapImpl(handler, byWhoAmI);

	private void FilterSlotSwapImpl<TR, TF>(FilterHandler<TR, TF>? handler, int byWhoAmI)
		where TF : class, IFilter<TR>
	{
		if (handler is null) return;
		if (handler.FilterItem.IsAir)
		{
			if (_held.IsAir || !handler.CanInsertFilterItem(_held)) return;
			var one = _held.Clone();
			one.stack = 1;
			handler.SetFilterItem(one);
			var remainder = _held.Clone();
			if (--remainder.stack <= 0) remainder.TurnToAir();
			WriteBackCursor(byWhoAmI, remainder);
		}
		else if (_held.IsAir)
		{
			var removed = handler.FilterItem;
			handler.SetFilterItem(new Item());
			WriteBackCursor(byWhoAmI, removed);
		}
	}

	private static void WriteBackCursor(int byWhoAmI, Item cursor)
	{
		if (Main.netMode == NetmodeID.Server)
			CursorUpdatePacket.SendTo(byWhoAmI, cursor, CursorUpdatePacket.Delivery.Cursor);
		else
			Main.mouseItem = cursor;
	}

	private static FluidStack HeldFluid(Item held)
	{
		if (held is null || held.IsAir) return FluidStack.Empty;
		var vanilla = VanillaFluidBridge.StackFor(held.type);
		if (!vanilla.IsEmpty) return vanilla;
		if (held.ModItem is FluidBucketItem bucket && bucket.Fluid is { } gf)
			return new FluidStack(gf, VanillaFluidBridge.BucketAmount);
		if (held.ModItem is IFluidHandlerItem container)
			return container.GetTank(0);
		return FluidStack.Empty;
	}
}
