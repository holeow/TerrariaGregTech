#nullable enable
using System;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Helpers.ExternalStorage;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public sealed class GenericStackInvItemHandler : IItemHandler
{
	private readonly GenericStackInv _inv;
	public GenericStackInvItemHandler(GenericStackInv inv) => _inv = inv;

	public int SlotCount => _inv.Size();

	public Item GetSlot(int slot) =>
		_inv.GetKey(slot) is AEItemKey ik
			? ik.ToStack((int)Math.Min(_inv.GetAmount(slot), int.MaxValue))
			: new Item();

	public Item Insert(int slot, Item item, bool simulate)
	{
		if (item.IsAir) return new Item();
		var key = AEItemKey.Of(item);
		if (key is null) return item;
		long inserted = _inv.Insert(slot, key, item.stack, simulate ? Actionable.SIMULATE : Actionable.MODULATE);
		if (inserted >= item.stack) return new Item();
		var left = item.Clone();
		left.stack = item.stack - (int)inserted;
		return left;
	}

	public Item Extract(int slot, int maxAmount, bool simulate)
	{
		if (_inv.GetKey(slot) is not AEItemKey ik) return new Item();
		long extracted = _inv.Extract(slot, ik, maxAmount, simulate ? Actionable.SIMULATE : Actionable.MODULATE);
		return extracted <= 0 ? new Item() : ik.ToStack((int)extracted);
	}
}

public sealed class GenericStackInvFluidHandler : IFluidHandler
{
	private readonly GenericStackInv _inv;
	public GenericStackInvFluidHandler(GenericStackInv inv) => _inv = inv;

	public int TankCount => _inv.Size();

	public FluidStack GetTank(int tank) =>
		_inv.GetKey(tank) is AEFluidKey fk
			? fk.ToStack((int)Math.Min(_inv.GetAmount(tank), int.MaxValue))
			: FluidStack.Empty;

	public int GetCapacity(int tank) => int.MaxValue;

	public int Fill(FluidStack fluid, bool simulate)
	{
		if (fluid.IsEmpty) return 0;
		var key = AEFluidKey.Of(fluid);
		if (key is null) return 0;
		var mode = simulate ? Actionable.SIMULATE : Actionable.MODULATE;
		long filled = 0;
		for (int s = 0; s < _inv.Size() && filled < fluid.Amount; s++)
			filled += _inv.Insert(s, key, fluid.Amount - filled, mode);
		return (int)filled;
	}

	public FluidStack Drain(int maxAmount, bool simulate)
	{
		for (int s = 0; s < _inv.Size(); s++)
			if (_inv.GetKey(s) is AEFluidKey fk)
				return Drain(fk.ToStack(maxAmount), simulate);
		return FluidStack.Empty;
	}

	public FluidStack Drain(FluidStack fluidStack, bool simulate)
	{
		if (fluidStack.IsEmpty) return FluidStack.Empty;
		var key = AEFluidKey.Of(fluidStack);
		if (key is null) return FluidStack.Empty;
		var mode = simulate ? Actionable.SIMULATE : Actionable.MODULATE;
		long drained = 0;
		for (int s = 0; s < _inv.Size() && drained < fluidStack.Amount; s++)
			if (_inv.GetKey(s) is AEFluidKey fk && fk.Equals(key))
				drained += _inv.Extract(s, key, fluidStack.Amount - drained, mode);
		return drained <= 0 ? FluidStack.Empty : key.ToStack((int)drained);
	}
}
