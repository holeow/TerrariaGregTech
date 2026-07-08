#nullable enable
using System;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public sealed class FluidHandlerMeStorage : MEStorage
{
	private readonly Func<IFluidHandler?> _resolver;
	private readonly AccessRestriction _access;
	private readonly Func<AEKey, bool>? _partition;
	private readonly Func<AEKey, bool>? _partitionListed;
	private readonly bool _filterOnExtraction;
	private readonly bool _filterAvailableContents;

	public FluidHandlerMeStorage(Func<IFluidHandler?> resolver,
		AccessRestriction access = AccessRestriction.READ_WRITE,
		Func<AEKey, bool>? partition = null,
		Func<AEKey, bool>? partitionListed = null,
		bool filterOnExtraction = true,
		bool filterAvailableContents = true)
	{
		_resolver = resolver;
		_access = access;
		_partition = partition;
		_partitionListed = partitionListed;
		_filterOnExtraction = filterOnExtraction;
		_filterAvailableContents = filterAvailableContents;
	}

	private bool CanExtract(AEKey what) => _access.IsAllowExtraction() && (_partition == null || _partition(what));

	public string GetDescription() => "External Fluid Tank";

	public string DebugDetail(AEKey what)
	{
		var sb = new System.Text.StringBuilder();
		sb.Append($"access={_access} allowIns={_access.IsAllowInsertion()}");
		sb.Append($" partition={( _partition == null ? "null" : _partition(what).ToString())}");
		var h = _resolver();
		if (h is null) { sb.Append(" resolver=NULL"); return sb.ToString(); }
		sb.Append($" handler={h.GetType().Name} tanks={h.TankCount}");
		for (int t = 0; t < h.TankCount; t++)
		{
			var st = h.GetTank(t);
			sb.Append($" [{t}]={(st.IsEmpty ? "empty" : (st.Type?.Id ?? "?") + "x" + st.Amount)}/{h.GetCapacity(t)}");
		}
		if (what is AEFluidKey fk)
			sb.Append($" fillSim={h.Fill(fk.ToStack((int)System.Math.Min(24000, int.MaxValue)), true)}");
		return sb.ToString();
	}

	public bool IsPreferredStorageFor(AEKey what, IActionSource source)
	{
		if (_partitionListed != null && _partitionListed(what)) return true;
		return what is AEFluidKey fk && Contains(fk);
	}

	private bool Contains(AEFluidKey fk)
	{
		var h = _resolver();
		if (h is null) return false;
		for (int t = 0; t < h.TankCount; t++)
		{
			var stack = h.GetTank(t);
			if (!stack.IsEmpty && fk.Equals(AEFluidKey.Of(stack))) return true;
		}
		return false;
	}

	public long Insert(AEKey what, long amount, Actionable mode, IActionSource source)
	{
		if (!_access.IsAllowInsertion()) return 0;
		if (_partition != null && !_partition(what)) return 0;
		if (what is not AEFluidKey fk) return 0;
		var h = _resolver();
		if (h is null) return 0;

		int amt = (int)Math.Min(amount, int.MaxValue);
		if (amt <= 0) return 0;
		return h.Fill(fk.ToStack(amt), mode == Actionable.SIMULATE);
	}

	public long Extract(AEKey what, long amount, Actionable mode, IActionSource source)
	{
		if (_filterOnExtraction && !CanExtract(what)) return 0;
		if (what is not AEFluidKey fk) return 0;
		var h = _resolver();
		if (h is null) return 0;

		int amt = (int)Math.Min(amount, int.MaxValue);
		if (amt <= 0) return 0;
		var drained = h.Drain(fk.ToStack(amt), mode == Actionable.SIMULATE);
		return drained.IsEmpty ? 0 : drained.Amount;
	}

	public void GetAvailableStacks(KeyCounter @out)
	{
		var h = _resolver();
		if (h is null) return;

		if (!_filterAvailableContents)
		{
			for (int t = 0; t < h.TankCount; t++)
			{
				var stack = h.GetTank(t);
				if (stack.IsEmpty) continue;
				var key = AEFluidKey.Of(stack);
				if (key is not null) @out.Add(key, stack.Amount);
			}
			return;
		}

		if (!_access.IsAllowExtraction()) return;
		for (int t = 0; t < h.TankCount; t++)
		{
			var stack = h.GetTank(t);
			if (stack.IsEmpty) continue;
			var key = AEFluidKey.Of(stack);
			if (key is null) continue;
			if (_partition != null && !_partition(key)) continue;
			if (h.Drain(stack, simulate: true).IsEmpty) continue;
			@out.Add(key, stack.Amount);
		}
	}
}
