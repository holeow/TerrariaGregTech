#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public static class MeCraftingService
{
	public static CraftingPlan? Plan(MeNetwork? net, AEKey? what, long amount)
	{
		if (net is null || what is null || amount <= 0) return null;
		return new CraftingCalculation(net, new GenericStack(what, amount),
			CalculationStrategy.ReportMissingItems, IActionSource.Empty()).Run();
	}

	public static CraftingSubmitResult Request(MeNetwork? net, AEKey what, long amount,
		IMeCraftingRequester? requester = null, int? playerId = null)
	{
		if (net is null) return CraftingSubmitResult.NoCpuFound;
		var plan = Plan(net, what, amount);
		if (plan is null || plan.Simulation) return CraftingSubmitResult.IncompletePlan;
		return Submit(net, plan, requester, playerId);
	}

	public static CraftingSubmitResult Submit(MeNetwork net, CraftingPlan plan,
		IMeCraftingRequester? requester, int? playerId)
	{
		if (plan.Simulation) return CraftingSubmitResult.IncompletePlan;

		QuantumComputerMachine? chosen = null;
		bool anyBusy = false, anyTooSmall = false, anyOffline = false;
		foreach (var dev in net.Devices)
		{
			if (dev is not QuantumComputerMachine cpu) continue;
			if (!cpu.IsOnline) { anyOffline = true; continue; }
			if (cpu.Logic.HasJob) { anyBusy = true; continue; }
			if (cpu.AvailableStorage < plan.Bytes) { anyTooSmall = true; continue; }
			if (chosen == null || cpu.AvailableStorage < chosen.AvailableStorage) chosen = cpu;
		}

		if (chosen == null)
		{
			if (anyBusy) return CraftingSubmitResult.CpuBusy;
			if (anyTooSmall) return CraftingSubmitResult.CpuTooSmall;
			if (anyOffline) return CraftingSubmitResult.CpuOffline;
			return CraftingSubmitResult.NoCpuFound;
		}

		return chosen.Logic.TrySubmitJob(plan, IActionSource.Empty(), requester, playerId);
	}

	public static string? UnfulfillableReason(MeNetwork net, MePattern p)
	{
		if (net.CanFulfill(p)) return null;
		if (p.Type == MePatternType.Crafting && p.StationTile >= 0)
			return $"No Pattern Provider beside a {PatternProviderMachine.StationDisplayName(p.StationTile)}";
		if (p.Type == MePatternType.Crafting)
			return $"No Pattern Provider can craft {p.PrimaryOutput.GetDisplayName()}";
		return $"No machine beside a Pattern Provider to make {p.PrimaryOutput.GetDisplayName()}";
	}

	public static List<(AEKey what, string reason)> CollectUnfulfillableByOutput(MeNetwork net, CraftingPlan plan)
	{
		var result = new List<(AEKey, string)>();
		var seen = new HashSet<AEKey>();
		foreach (var pattern in plan.PatternTimes.Keys)
		{
			var reason = UnfulfillableReason(net, pattern);
			if (reason == null) continue;
			foreach (var (what, _) in pattern.Outputs)
				if (what != null && seen.Add(what)) result.Add((what, reason));
		}
		return result;
	}
}
