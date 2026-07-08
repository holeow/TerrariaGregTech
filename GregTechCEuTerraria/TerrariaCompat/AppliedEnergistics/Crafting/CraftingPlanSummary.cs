// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.menu.me.crafting.CraftingPlanSummary + CraftingPlanSummaryEntry), Forge 1.20.1.
// LGPL-3.0-only. See AE2 LICENSE.

#nullable enable
using System.Collections.Generic;
using System.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class CraftingPlanSummaryEntry
{
	public readonly AEKey What;
	public readonly long MissingAmount;
	public readonly long StoredAmount;
	public readonly long CraftAmount;

	public CraftingPlanSummaryEntry(AEKey what, long missingAmount, long storedAmount, long craftAmount)
	{
		What = what;
		MissingAmount = missingAmount;
		StoredAmount = storedAmount;
		CraftAmount = craftAmount;
	}

	public static readonly IComparer<CraftingPlanSummaryEntry> Comparator =
		Comparer<CraftingPlanSummaryEntry>.Create((a, b) =>
		{
			int c = b.MissingAmount.CompareTo(a.MissingAmount);
			if (c != 0) return c;
			c = b.CraftAmount.CompareTo(a.CraftAmount);
			if (c != 0) return c;
			return b.StoredAmount.CompareTo(a.StoredAmount);
		});

	public void Write(BinaryWriter buf)
	{
		AEKey.WriteKey(buf, What);
		buf.Write7BitEncodedInt64(MissingAmount);
		buf.Write7BitEncodedInt64(StoredAmount);
		buf.Write7BitEncodedInt64(CraftAmount);
	}

	public static CraftingPlanSummaryEntry? Read(BinaryReader buf)
	{
		var what = AEKey.ReadKey(buf);
		long missing = buf.Read7BitEncodedInt64();
		long stored = buf.Read7BitEncodedInt64();
		long craft = buf.Read7BitEncodedInt64();
		return what == null ? null : new CraftingPlanSummaryEntry(what, missing, stored, craft);
	}
}

public sealed class PlanPattern
{
	public readonly (AEKey what, long amount)[] Inputs;
	public readonly (AEKey what, long amount)[] Outputs;
	public readonly long Times;

	public PlanPattern((AEKey what, long amount)[] inputs, (AEKey what, long amount)[] outputs, long times)
	{
		Inputs = inputs;
		Outputs = outputs;
		Times = times;
	}

	public void Write(BinaryWriter buf)
	{
		WriteStacks(buf, Inputs);
		WriteStacks(buf, Outputs);
		buf.Write7BitEncodedInt64(Times);
	}

	public static PlanPattern? Read(BinaryReader buf)
	{
		var inputs = ReadStacks(buf);
		var outputs = ReadStacks(buf);
		long times = buf.Read7BitEncodedInt64();
		return outputs.Length == 0 ? null : new PlanPattern(inputs, outputs, times);
	}

	private static void WriteStacks(BinaryWriter buf, (AEKey what, long amount)[] stacks)
	{
		buf.Write7BitEncodedInt(stacks.Length);
		foreach (var (what, amount) in stacks) { AEKey.WriteKey(buf, what); buf.Write7BitEncodedInt64(amount); }
	}

	private static (AEKey, long)[] ReadStacks(BinaryReader buf)
	{
		int n = buf.Read7BitEncodedInt();
		var list = new List<(AEKey, long)>(n);
		for (int i = 0; i < n; i++)
		{
			var k = AEKey.ReadKey(buf);
			long amt = buf.Read7BitEncodedInt64();
			if (k != null) list.Add((k, amt));
		}
		return list.ToArray();
	}
}

public sealed class CraftingPlanSummary
{
	public long UsedBytes { get; }
	public bool Simulation { get; }
	public IReadOnlyList<CraftingPlanSummaryEntry> Entries { get; }
	public IReadOnlyList<PlanPattern> Patterns { get; }

	public CraftingPlanSummary(long usedBytes, bool simulation,
		IReadOnlyList<CraftingPlanSummaryEntry> entries, IReadOnlyList<PlanPattern> patterns)
	{
		UsedBytes = usedBytes;
		Simulation = simulation;
		Entries = entries;
		Patterns = patterns;
	}

	public static CraftingPlanSummary FromPlan(CraftingPlan plan, MEStorage storage, IActionSource src)
	{
		var map = new Dictionary<AEKey, (long stored, long crafting)>();
		void AddStored(AEKey k, long v) { map.TryGetValue(k, out var e); map[k] = (e.stored + v, e.crafting); }
		void AddCraft(AEKey k, long v) { map.TryGetValue(k, out var e); map[k] = (e.stored, e.crafting + v); }

		foreach (var used in plan.UsedItems) AddStored(used.Key, used.Value);
		foreach (var missing in plan.MissingItems) AddStored(missing.Key, missing.Value);
		foreach (var emitted in plan.EmittedItems) { AddStored(emitted.Key, emitted.Value); AddCraft(emitted.Key, emitted.Value); }
		foreach (var pt in plan.PatternTimes)
			foreach (var (what, amount) in pt.Key.Outputs)
				AddCraft(what, amount * pt.Value);

		var entries = new List<CraftingPlanSummaryEntry>();
		foreach (var kv in map)
		{
			long missingAmount, storedAmount;
			if (plan.Simulation)
			{
				storedAmount = storage.Extract(kv.Key, kv.Value.stored, Actionable.SIMULATE, src);
				missingAmount = kv.Value.stored - storedAmount;
			}
			else
			{
				storedAmount = kv.Value.stored;
				missingAmount = 0;
			}
			entries.Add(new CraftingPlanSummaryEntry(kv.Key, missingAmount, storedAmount, kv.Value.crafting));
		}
		entries.Sort(CraftingPlanSummaryEntry.Comparator);

		var patterns = new List<PlanPattern>(plan.PatternTimes.Count);
		foreach (var pt in plan.PatternTimes)
		{
			var inputs = pt.Key.Inputs;
			var outputs = pt.Key.Outputs;
			var inArr = new (AEKey, long)[inputs.Count];
			for (int i = 0; i < inputs.Count; i++) inArr[i] = inputs[i];
			var outArr = new (AEKey, long)[outputs.Count];
			for (int i = 0; i < outputs.Count; i++) outArr[i] = outputs[i];
			patterns.Add(new PlanPattern(inArr, outArr, pt.Value));
		}

		return new CraftingPlanSummary(plan.Bytes, plan.Simulation, entries, patterns);
	}

	public void Write(BinaryWriter buf)
	{
		buf.Write7BitEncodedInt64(UsedBytes);
		buf.Write(Simulation);
		buf.Write7BitEncodedInt(Entries.Count);
		foreach (var e in Entries) e.Write(buf);
		buf.Write7BitEncodedInt(Patterns.Count);
		foreach (var p in Patterns) p.Write(buf);
	}

	public static CraftingPlanSummary Read(BinaryReader buf)
	{
		long usedBytes = buf.Read7BitEncodedInt64();
		bool simulation = buf.ReadBoolean();
		int count = buf.Read7BitEncodedInt();
		var entries = new List<CraftingPlanSummaryEntry>(count);
		for (int i = 0; i < count; i++)
		{
			var e = CraftingPlanSummaryEntry.Read(buf);
			if (e != null) entries.Add(e);
		}
		int pc = buf.Read7BitEncodedInt();
		var patterns = new List<PlanPattern>(pc);
		for (int i = 0; i < pc; i++)
		{
			var p = PlanPattern.Read(buf);
			if (p != null) patterns.Add(p);
		}
		return new CraftingPlanSummary(usedBytes, simulation, entries, patterns);
	}
}
