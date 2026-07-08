#nullable enable
using System;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public sealed class MeStorageFluidTank : IFluidHandler
{
	private readonly MEStorage _storage;
	private readonly IActionSource _source;
	private readonly AEFluidKey? _bound;

	public MeStorageFluidTank(MEStorage storage, IActionSource source, AEFluidKey? bound = null)
	{
		_storage = storage;
		_source = source;
		_bound = bound;
	}

	public int TankCount => 1;
	public int GetCapacity(int tank) => int.MaxValue;

	public FluidStack GetTank(int tank)
	{
		if (_bound is null) return FluidStack.Empty;
		long avail = _storage.Extract(_bound, int.MaxValue, Actionable.SIMULATE, _source);
		return avail <= 0 ? FluidStack.Empty : _bound.ToStack((int)Math.Min(avail, int.MaxValue));
	}

	public int Fill(FluidStack fluid, bool simulate)
	{
		if (fluid.IsEmpty) return 0;
		var key = AEFluidKey.Of(fluid);
		if (key is null) return 0;
		long inserted = _storage.Insert(key, fluid.Amount,
			simulate ? Actionable.SIMULATE : Actionable.MODULATE, _source);
		return (int)Math.Min(inserted, int.MaxValue);
	}

	public FluidStack Drain(int maxAmount, bool simulate)
	{
		if (_bound is null || maxAmount <= 0) return FluidStack.Empty;
		long got = _storage.Extract(_bound, maxAmount,
			simulate ? Actionable.SIMULATE : Actionable.MODULATE, _source);
		return got <= 0 ? FluidStack.Empty : _bound.ToStack((int)Math.Min(got, int.MaxValue));
	}

	public FluidStack Drain(FluidStack fluidStack, bool simulate)
	{
		if (fluidStack.IsEmpty) return FluidStack.Empty;
		var key = AEFluidKey.Of(fluidStack);
		if (key is null) return FluidStack.Empty;
		long got = _storage.Extract(key, fluidStack.Amount,
			simulate ? Actionable.SIMULATE : Actionable.MODULATE, _source);
		return got <= 0 ? FluidStack.Empty : key.ToStack((int)Math.Min(got, int.MaxValue));
	}
}
