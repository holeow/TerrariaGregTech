// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.execution.ExecutingCraftingJob), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.

#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class ExecutingCraftingJob
{
	public sealed class TaskProgress { public long Value; }

	public readonly CraftingLink Link;
	public readonly ListCraftingInventory WaitingFor;
	public readonly Dictionary<MePattern, TaskProgress> Tasks = new();
	public readonly ElapsedTimeTracker TimeTracker;
	public GenericStack FinalOutput;
	public long RemainingAmount;
	public int? PlayerId;

	public ExecutingCraftingJob(CraftingPlan plan, Action<AEKey> postCraftingDifference, CraftingLink link,
		int? playerId)
	{
		FinalOutput = plan.FinalOutput;
		RemainingAmount = FinalOutput.Amount;
		WaitingFor = new ListCraftingInventory(postCraftingDifference);

		TimeTracker = new ElapsedTimeTracker();
		foreach (var entry in plan.EmittedItems)
		{
			WaitingFor.Insert(entry.Key, entry.Value, Actionable.MODULATE);
			TimeTracker.AddMaxItems(entry.Value, entry.Key.KeyType);
		}
		foreach (var entry in plan.PatternTimes)
		{
			if (!Tasks.TryGetValue(entry.Key, out var tp)) Tasks[entry.Key] = tp = new TaskProgress();
			tp.Value += entry.Value;
			foreach (var (what, amount) in entry.Key.Outputs)
				TimeTracker.AddMaxItems(amount * entry.Value * what.GetAmountPerUnit(), what.KeyType);
		}
		Link = link;
		PlayerId = playerId;
	}

	public ExecutingCraftingJob(TagCompound data, Action<AEKey> postCraftingDifference, CraftingCpuLogic cpu)
	{
		Link = new CraftingLink(data.GetCompound("link"), cpu);
		FinalOutput = GenericStack.ReadTag(data.GetCompound("finalOutput"))
			?? throw new InvalidOperationException("Crafting job has no final output");
		RemainingAmount = data.GetLong("remainingAmount");
		WaitingFor = new ListCraftingInventory(postCraftingDifference);
		WaitingFor.ReadFromNBT(data.GetList<TagCompound>("waitingFor"));
		TimeTracker = new ElapsedTimeTracker(data.GetCompound("timeTracker"));
		PlayerId = data.ContainsKey("playerId") ? data.GetInt("playerId") : null;

		foreach (var item in data.GetList<TagCompound>("tasks"))
		{
			var details = MePattern.Decode(item.GetCompound("pattern"));
			if (details != null)
				Tasks[details] = new TaskProgress { Value = item.GetLong("progress") };
		}
	}

	public TagCompound WriteToNBT()
	{
		var data = new TagCompound();
		var linkData = new TagCompound();
		Link.WriteToNBT(linkData);
		data["link"] = linkData;
		data["finalOutput"] = GenericStack.WriteTag(FinalOutput);
		data["waitingFor"] = WaitingFor.WriteToNBT();
		data["timeTracker"] = TimeTracker.WriteToNBT();
		data["remainingAmount"] = RemainingAmount;
		if (PlayerId != null) data["playerId"] = PlayerId.Value;

		var list = new List<TagCompound>();
		foreach (var e in Tasks)
			list.Add(new TagCompound { ["pattern"] = e.Key.Encode(), ["progress"] = e.Value.Value });
		data["tasks"] = list;
		return data;
	}
}
