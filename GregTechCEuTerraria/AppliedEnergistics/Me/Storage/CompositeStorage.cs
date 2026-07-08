// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.me.storage.CompositeStorage), Forge 1.20.1. Original is unheadered; AE2
// is LGPL-3.0-only (older API files MIT). See AE2's LICENSE.
//
// TODO: port the periodic refresh when the ME storage tick loop lands. AE2's grid
// types (ITickingMonitor / onTick / TickRateModulation) aren't used (no grid), so
// for now the cache only rebuilds on our own ops (on-demand in GetAvailableStacks)
// - external changes to the underlying storages won't be seen until then. Update()
// already returns the `changed` diff to drive the eventual notify/throttle.

#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Terraria.Localization;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using GregTechCEuTerraria.AppliedEnergistics.Core;

namespace GregTechCEuTerraria.AppliedEnergistics.Me.Storage;

public class CompositeStorage : MEStorage
{
	private readonly InventoryCache _cache;
	private Dictionary<AEKeyType, MEStorage> _storages;
	private bool _forceCacheRebuild = true;

	public CompositeStorage(Dictionary<AEKeyType, MEStorage> storages)
	{
		_storages = storages;
		_cache = new InventoryCache(this);
	}

	public void SetStorages(Dictionary<AEKeyType, MEStorage> storages) =>
		_storages = storages ?? throw new ArgumentNullException(nameof(storages));

	public bool IsPreferredStorageFor(AEKey what, IActionSource source) =>
		_storages.TryGetValue(what.KeyType, out var storage) && storage.IsPreferredStorageFor(what, source);

	public long Insert(AEKey what, long amount, Actionable mode, IActionSource source)
	{
		var inserted = _storages.TryGetValue(what.KeyType, out var storage)
			? storage.Insert(what, amount, mode, source) : 0;
		if (inserted > 0 && mode == Actionable.MODULATE)
			_forceCacheRebuild = true;
		return inserted;
	}

	public long Extract(AEKey what, long amount, Actionable mode, IActionSource source)
	{
		var extracted = _storages.TryGetValue(what.KeyType, out var storage)
			? storage.Extract(what, amount, mode, source) : 0;
		if (extracted > 0 && mode == Actionable.MODULATE)
			_forceCacheRebuild = true;
		return extracted;
	}

	public string GetDescription()
	{
		var types = new StringBuilder();
		bool first = true;
		foreach (var keyType in _storages.Keys)
		{
			if (!first) types.Append(", ");
			else first = false;
			types.Append(keyType.GetDescription());
		}
		return $"{Language.GetTextValue(AELocale.StorageExternal)} ({types})";
	}

	public void GetAvailableStacks(KeyCounter @out)
	{
		if (_forceCacheRebuild)
		{
			_forceCacheRebuild = false;
			_cache.Update();
		}
		_cache.GetAvailableKeys(@out);
	}

	private sealed class InventoryCache
	{
		private readonly CompositeStorage _owner;
		private KeyCounter _frontBuffer = new();
		private KeyCounter _backBuffer = new();

		public InventoryCache(CompositeStorage owner) => _owner = owner;

		public bool Update()
		{
			(_backBuffer, _frontBuffer) = (_frontBuffer, _backBuffer);
			_frontBuffer.Reset();

			foreach (var storage in _owner._storages.Values)
				storage.GetAvailableStacks(_frontBuffer);

			bool changed = false;
			foreach (var entry in _frontBuffer)
			{
				var old = _backBuffer.Get(entry.Key);
				if (old == 0 || old != entry.Value)
					changed = true;
			}
			foreach (var oldEntry in _backBuffer)
			{
				if (_frontBuffer.Get(oldEntry.Key) == 0)
					changed = true;
			}

			_frontBuffer.RemoveZeros();
			return changed;
		}

		public void GetAvailableKeys(KeyCounter @out) => @out.AddAll(_frontBuffer);
	}
}
