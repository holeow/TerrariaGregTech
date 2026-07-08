// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.client.gui.me.common.PendingCraftingJobs), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public static class PendingCraftingJobs
{
	public enum Status { STARTED, CANCELLED, FINISHED }

	private readonly record struct PendingJob(Guid JobId, AEKey What, long RequestedAmount, long RemainingAmount);
	private static readonly Dictionary<Guid, PendingJob> _jobs = new();

	public static bool HasPendingJob(AEKey what)
	{
		foreach (var j in _jobs.Values)
			if (j.What.Equals(what)) return true;
		return false;
	}

	public static void ClearPendingJobs() => _jobs.Clear();

	public static void JobStatus(Guid id, AEKey what, long requestedAmount, long remainingAmount, Status status)
	{
		switch (status)
		{
			case Status.STARTED:
				if (!_jobs.ContainsKey(id))
					_jobs[id] = new PendingJob(id, what, requestedAmount, remainingAmount);
				break;
			case Status.CANCELLED:
			case Status.FINISHED:
				_jobs.Remove(id);
				break;
		}
	}
}
