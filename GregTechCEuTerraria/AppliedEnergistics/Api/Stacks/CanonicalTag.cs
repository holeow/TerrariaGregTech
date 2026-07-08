// NOT an AE2 type - Terraria-side adaptation helper for GregTechCEuTerraria.
//
// MC's CompoundTag has value-based equals()/hashCode(); tML's TagCompound does
// not. AE2's AEItemKey.InternedTag and AEFluidKey rely on that value-equality to
// dedup/hash keys by their NBT. This provides the structural equality + hash they
// need (order-independent over keys; ordered over lists, matching MC's
// CompoundTag.equals). Replaces AE2's WeakHashMap interning with cached
// hash/equality stored on each key.

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

internal static class CanonicalTag
{
	public static bool Equal(TagCompound? a, TagCompound? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		if (a.Count != b.Count) return false;

		var bd = new Dictionary<string, object>(b.Count);
		foreach (var kv in b) bd[kv.Key] = kv.Value;

		foreach (var kv in a)
		{
			if (!bd.TryGetValue(kv.Key, out var bv)) return false;
			if (!ValueEqual(kv.Value, bv)) return false;
		}
		return true;
	}

	public static int Hash(TagCompound? tag)
	{
		if (tag is null) return 0;
		int h = 0;
		foreach (var kv in tag)
			h += kv.Key.GetHashCode() ^ ValueHash(kv.Value);
		return h;
	}

	private static bool ValueEqual(object? x, object? y)
	{
		if (ReferenceEquals(x, y)) return true;
		if (x is null || y is null) return false;
		if (x is TagCompound tx && y is TagCompound ty) return Equal(tx, ty);
		if (x is byte[] bx && y is byte[] by) return bx.Length == by.Length && bx.AsSpan().SequenceEqual(by);
		if (x is int[] ix && y is int[] iy) return ix.AsSpan().SequenceEqual(iy);
		if (x is IList lx && y is IList ly)
		{
			if (lx.Count != ly.Count) return false;
			for (int i = 0; i < lx.Count; i++)
				if (!ValueEqual(lx[i], ly[i])) return false;
			return true;
		}
		return x.Equals(y);
	}

	private static int ValueHash(object? v)
	{
		switch (v)
		{
			case null: return 0;
			case TagCompound t: return Hash(t);
			case byte[] b:
			{
				int h = 17;
				foreach (var x in b) h = h * 31 + x;
				return h;
			}
			case int[] ia:
			{
				int h = 17;
				foreach (var x in ia) h = h * 31 + x;
				return h;
			}
			case IList l:
			{
				int h = 17;
				foreach (var e in l) h = h * 31 + ValueHash(e);
				return h;
			}
			default: return v.GetHashCode();
		}
	}
}
