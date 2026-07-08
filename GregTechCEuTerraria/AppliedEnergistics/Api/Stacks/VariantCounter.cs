// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.stacks.VariantCounter), Forge 1.20.1. Original is unheadered; AE2 is
// LGPL-3.0-only (older API files MIT). See AE2's LICENSE.

#nullable enable
using System.Collections.Generic;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

internal sealed class VariantCounter
{
	private bool _dropZeros;
	private readonly Dictionary<AEKey, long> _records = new();

	public bool IsDropZeros() => _dropZeros;

	public void SetDropZeros(bool dropZeros) => _dropZeros = dropZeros;

	public long Get(AEKey key) => _records.TryGetValue(key, out var v) ? v : 0;

	public void Add(AEKey key, long amount)
	{
		_records.TryGetValue(key, out var current);
		_records[key] = current + amount;
	}

	public void Set(AEKey key, long amount)
	{
		if (_dropZeros && amount == 0)
			_records.Remove(key);
		else
			_records[key] = amount;
	}

	public long Remove(AEKey key) => _records.Remove(key, out var v) ? v : 0;

	public void AddAll(VariantCounter other)
	{
		foreach (var entry in other._records)
			Add(entry.Key, entry.Value);
	}

	public void RemoveAll(VariantCounter other)
	{
		foreach (var entry in other._records)
			Add(entry.Key, -entry.Value);
	}

	public int Size()
	{
		if (!_dropZeros)
			return _records.Count;

		var size = 0;
		foreach (var value in _records.Values)
			if (value != 0)
				size++;
		return size;
	}

	public bool IsEmpty()
	{
		if (!_dropZeros)
			return _records.Count == 0;

		foreach (var value in _records.Values)
			if (value != 0)
				return false;
		return true;
	}

	public IEnumerable<KeyValuePair<AEKey, long>> Entries()
	{
		if (!_dropZeros)
		{
			foreach (var kv in _records)
				yield return kv;
			yield break;
		}

		foreach (var key in new List<AEKey>(_records.Keys))
		{
			if (!_records.TryGetValue(key, out var value))
				continue;
			if (value == 0)
				_records.Remove(key);
			else
				yield return new KeyValuePair<AEKey, long>(key, value);
		}
	}

	public void Reset()
	{
		if (_dropZeros)
		{
			_records.Clear();
		}
		else
		{
			foreach (var key in new List<AEKey>(_records.Keys))
				_records[key] = 0L;
		}
	}

	public void Clear() => _records.Clear();

	public VariantCounter Copy()
	{
		var result = new VariantCounter { _dropZeros = _dropZeros };
		foreach (var kv in _records)
			result._records[kv.Key] = kv.Value;
		return result;
	}

	public void Invert()
	{
		foreach (var key in new List<AEKey>(_records.Keys))
			_records[key] = -_records[key];
	}

	public void RemoveZeros()
	{
		var toRemove = new List<AEKey>();
		foreach (var kv in _records)
			if (kv.Value == 0)
				toRemove.Add(kv.Key);
		foreach (var key in toRemove)
			_records.Remove(key);
	}
}
