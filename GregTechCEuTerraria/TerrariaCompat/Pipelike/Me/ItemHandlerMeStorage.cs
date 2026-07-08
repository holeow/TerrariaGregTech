#nullable enable
using System;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public sealed class ItemHandlerMeStorage : MEStorage
{
	private readonly Func<IItemHandler?> _resolver;
	private readonly AccessRestriction _access;
	private readonly Func<AEKey, bool>? _partition;
	private readonly Func<AEKey, bool>? _partitionListed;
	private readonly bool _filterOnExtraction;
	private readonly bool _filterAvailableContents;

	public ItemHandlerMeStorage(Func<IItemHandler?> resolver,
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

	public string GetDescription() => "External Inventory";

	public bool IsPreferredStorageFor(AEKey what, IActionSource source)
	{
		if (_partitionListed != null && _partitionListed(what)) return true;
		return what is AEItemKey ik && Contains(ik);
	}

	private bool Contains(AEItemKey ik)
	{
		var h = _resolver();
		if (h is null) return false;
		for (int s = 0; s < h.SlotCount; s++)
		{
			var slot = h.GetSlot(s);
			if (!slot.IsAir && ik.Matches(slot)) return true;
		}
		return false;
	}

	public long Insert(AEKey what, long amount, Actionable mode, IActionSource source)
	{
		if (!_access.IsAllowInsertion()) return 0;
		if (_partition != null && !_partition(what)) return 0;
		if (what is not AEItemKey ik) return 0;
		var h = _resolver();
		if (h is null) return 0;

		bool simulate = mode == Actionable.SIMULATE;
		long inserted = 0;
		long remaining = amount;
		for (int s = 0; s < h.SlotCount && remaining > 0; s++)
		{
			int chunk = (int)Math.Min(remaining, ik.GetMaxStackSize());
			var stack = ik.ToStack(chunk);
			if (!h.IsItemValid(s, stack)) continue;
			var leftover = h.Insert(s, stack, simulate);
			int moved = chunk - (leftover.IsAir ? 0 : leftover.stack);
			if (moved <= 0) continue;
			inserted += moved;
			remaining -= moved;
		}
		return inserted;
	}

	public long Extract(AEKey what, long amount, Actionable mode, IActionSource source)
	{
		if (_filterOnExtraction && !CanExtract(what)) return 0;
		if (what is not AEItemKey ik) return 0;
		var h = _resolver();
		if (h is null) return 0;

		bool simulate = mode == Actionable.SIMULATE;
		long extracted = 0;
		long remaining = amount;
		for (int s = 0; s < h.SlotCount && remaining > 0; s++)
		{
			var slot = h.GetSlot(s);
			if (slot.IsAir || !ik.Matches(slot)) continue;
			int want = (int)Math.Min(remaining, int.MaxValue);
			var got = h.Extract(s, want, simulate);
			if (got.IsAir) continue;
			long take = Math.Min(got.stack, want);
			extracted += take;
			remaining -= take;
		}
		return extracted;
	}

	public void GetAvailableStacks(KeyCounter @out)
	{
		var h = _resolver();
		if (h is null) return;

		if (!_filterAvailableContents)
		{
			for (int s = 0; s < h.SlotCount; s++)
			{
				var slot = h.GetSlot(s);
				if (slot.IsAir) continue;
				var key = AEItemKey.Of(slot);
				if (key is not null) @out.Add(key, slot.stack);
			}
			return;
		}

		if (!_access.IsAllowExtraction()) return;
		for (int s = 0; s < h.SlotCount; s++)
		{
			var slot = h.GetSlot(s);
			if (slot.IsAir) continue;
			var key = AEItemKey.Of(slot);
			if (key is null) continue;
			if (_partition != null && !_partition(key)) continue;
			@out.Add(key, slot.stack);
		}
	}
}
