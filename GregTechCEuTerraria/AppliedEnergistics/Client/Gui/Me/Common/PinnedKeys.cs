// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.client.gui.me.common.PinnedKeys), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Me.Common;

public static class PinnedKeys
{
	public const int MaxPinned = 9;

	private static readonly IComparer<KeyValuePair<AEKey, PinInfo>> TimeComparator =
		Comparer<KeyValuePair<AEKey, PinInfo>>.Create((a, b) => a.Value.Since.CompareTo(b.Value.Since));

	private static readonly Dictionary<AEKey, PinInfo> Pinned = new(MaxPinned);

	public static bool IsEmpty() => Pinned.Count == 0;

	public static ISet<AEKey> GetPinnedKeys() => new HashSet<AEKey>(Pinned.Keys);

	public static PinInfo? GetPinInfo(AEKey key) => Pinned.TryGetValue(key, out var info) ? info : null;

	public static void ClearPinnedKeys() => Pinned.Clear();

	public static void PinKey(AEKey key, PinReason reason)
	{
		if (Pinned.TryGetValue(key, out var info))
		{
			info.Since = DateTime.UtcNow;
		}
		else
		{
			Pinned[key] = new PinInfo(reason);
		}

		if (Pinned.Count > MaxPinned)
		{
			var toRemove = new List<KeyValuePair<AEKey, PinInfo>>(Pinned);
			toRemove.Sort(TimeComparator);
			foreach (var entry in toRemove.GetRange(0, toRemove.Count - MaxPinned))
				Pinned.Remove(entry.Key);
		}
	}

	public static void Unpin(AEKey what) => Pinned.Remove(what);

	public static bool IsPinned(AEKey what) => Pinned.ContainsKey(what);

	public static void MarkCraftingPrunable(Func<AEKey, bool> stillPending)
	{
		foreach (var kv in Pinned)
			if (kv.Value.Reason == PinReason.CRAFTING && !stillPending(kv.Key))
				kv.Value.CanPrune = true;
	}

	public static void Prune()
	{
		var stale = new List<AEKey>();
		foreach (var (key, info) in Pinned)
			if (info.CanPrune)
				stale.Add(key);
		foreach (var key in stale)
			Pinned.Remove(key);
	}

	public sealed class PinInfo
	{
		public DateTime Since;
		public PinReason Reason;
		public bool CanPrune;

		public PinInfo(PinReason reason)
		{
			Reason = reason;
			Since = DateTime.UtcNow;
		}
	}

	public enum PinReason
	{
		CRAFTING
	}
}
