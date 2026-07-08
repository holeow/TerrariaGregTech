// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.stacks.KeyCounter), Forge 1.20.1. Original MIT header preserved
// verbatim below per AE2's license terms.
//
// The MIT License (MIT)
//
// Copyright (c) 2021 TeamAppliedEnergistics
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#nullable enable
using System.Collections;
using System.Collections.Generic;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

public sealed class KeyCounter : IEnumerable<KeyValuePair<AEKey, long>>
{
	private readonly Dictionary<object, VariantCounter> _lists = new();

	public void RemoveZeros()
	{
		foreach (var primaryKey in new List<object>(_lists.Keys))
		{
			var variantList = _lists[primaryKey];
			variantList.RemoveZeros();
			if (variantList.IsEmpty())
				_lists.Remove(primaryKey);
		}
	}

	public void RemoveEmptySubmaps()
	{
		foreach (var primaryKey in new List<object>(_lists.Keys))
			if (_lists[primaryKey].IsEmpty())
				_lists.Remove(primaryKey);
	}

	public void AddAll(KeyCounter other)
	{
		foreach (var entry in other._lists)
		{
			if (!_lists.TryGetValue(entry.Key, out var ourSubIndex))
				_lists[entry.Key] = entry.Value.Copy();
			else
				ourSubIndex.AddAll(entry.Value);
		}
	}

	public void RemoveAll(KeyCounter other)
	{
		foreach (var entry in other._lists)
		{
			if (!_lists.TryGetValue(entry.Key, out var ourSubIndex))
			{
				var copied = entry.Value.Copy();
				copied.Invert();
				_lists[entry.Key] = copied;
			}
			else
			{
				ourSubIndex.RemoveAll(entry.Value);
			}
		}
	}

	public void Add(AEKey key, long amount) => GetSubIndex(key).Add(key, amount);

	public void Remove(AEKey key, long amount) => Add(key, -amount);

	public long Remove(AEKey key)
	{
		var subIndex = GetSubIndex(key);
		var ret = subIndex.Remove(key);
		if (subIndex.IsEmpty())
			_lists.Remove(key.GetPrimaryKey());
		return ret;
	}

	public void Set(AEKey key, long amount) => GetSubIndex(key).Set(key, amount);

	public long Get(AEKey key)
	{
		if (!_lists.TryGetValue(key.GetPrimaryKey(), out var subIndex))
			return 0;
		return subIndex.Get(key);
	}

	public void Reset()
	{
		foreach (var list in _lists.Values)
			list.Reset();
	}

	public void Clear()
	{
		foreach (var list in _lists.Values)
			list.Clear();
	}

	public bool IsEmpty()
	{
		foreach (var list in _lists.Values)
			if (!list.IsEmpty())
				return false;
		return true;
	}

	public int Size()
	{
		int tot = 0;
		foreach (var list in _lists.Values)
			tot += list.Size();
		return tot;
	}

	public IEnumerator<KeyValuePair<AEKey, long>> GetEnumerator()
	{
		foreach (var value in _lists.Values)
			foreach (var entry in value.Entries())
				yield return entry;
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private VariantCounter GetSubIndex(AEKey key)
	{
		var primaryKey = key.GetPrimaryKey();
		if (!_lists.TryGetValue(primaryKey, out var subIndex))
		{
			subIndex = new VariantCounter();
			_lists[primaryKey] = subIndex;
		}
		return subIndex;
	}

	private VariantCounter? GetSubIndexOrNull(AEKey key) =>
		_lists.TryGetValue(key.GetPrimaryKey(), out var s) ? s : null;

	public AEKey? GetFirstKey()
	{
		var e = GetFirstEntry();
		return e?.Key;
	}

	public T? GetFirstKey<T>() where T : AEKey
	{
		var e = GetFirstEntry<T>();
		return e?.Key as T;
	}

	public KeyValuePair<AEKey, long>? GetFirstEntry()
	{
		foreach (var value in _lists.Values)
			foreach (var entry in value.Entries())
				return entry;
		return null;
	}

	public KeyValuePair<AEKey, long>? GetFirstEntry<T>() where T : AEKey
	{
		foreach (var value in _lists.Values)
			foreach (var entry in value.Entries())
				if (entry.Key is T)
					return entry;
		return null;
	}

	public ISet<AEKey> KeySet()
	{
		var keys = new HashSet<AEKey>(Size());
		foreach (var list in _lists.Values)
			foreach (var entry in list.Entries())
				keys.Add(entry.Key);
		return keys;
	}
}
