// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.menu.me.common.IncrementalUpdateHelper), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.AppliedEnergistics.Menu.Me.Common;

public sealed class IncrementalUpdateHelper
{
	private readonly Dictionary<AEKey, long> _mapping = new();
	private readonly Dictionary<long, AEKey> _inverse = new();

	private readonly HashSet<AEKey> _changes = new();

	private long _serial;
	private bool _fullUpdate = true;

	public long? GetSerial(AEKey stack) =>
		_mapping.TryGetValue(stack, out var s) ? s : null;

	public long GetOrAssignSerial(AEKey key)
	{
		if (_mapping.TryGetValue(key, out var existing))
			return existing;
		var serial = ++_serial;
		_mapping[key] = serial;
		_inverse[serial] = key;
		return serial;
	}

	public AEKey? GetBySerial(long serial) =>
		_inverse.TryGetValue(serial, out var k) ? k : null;

	public void Clear()
	{
		_changes.Clear();
		_fullUpdate = true;
	}

	public void Reset()
	{
		Clear();
		_serial = 0;
		_mapping.Clear();
		_inverse.Clear();
	}

	public void AddChange(AEKey entry)
	{
		if (!_changes.Add(entry))
		{
			_changes.Remove(entry);
			_changes.Add(entry);
		}
	}

	public void RemoveSerial(AEKey what)
	{
		if (_mapping.TryGetValue(what, out var s))
		{
			_mapping.Remove(what);
			_inverse.Remove(s);
		}
	}

	public void CommitChanges()
	{
		_changes.Clear();
		_fullUpdate = false;
	}

	public bool HasChanges() => _fullUpdate || _changes.Count > 0;

	public bool IsFullUpdate() => _fullUpdate;

	public IEnumerable<AEKey> Changes => _changes;
}
