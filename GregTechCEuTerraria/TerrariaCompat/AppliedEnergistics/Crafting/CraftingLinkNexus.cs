// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.CraftingLinkNexus), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class CraftingLinkNexus
{
	public Guid CraftId { get; }
	private bool _canceled;
	private bool _done;
	private int _tickOfDeath;
	private CraftingLink? _req;
	private CraftingLink? _cpu;

	public CraftingLinkNexus(Guid craftId) => CraftId = craftId;

	public bool IsDead(CraftingLinkManager manager)
	{
		if (_canceled || _done) return true;

		if (_req == null || _cpu == null)
			_tickOfDeath++;
		else
		{
			bool hasCpu = manager.HasCpu(_cpu.Cpu);
			bool hasMachine = _req.Requester?.RequesterNetwork != null
				&& _cpu.Cpu != null
				&& ReferenceEquals(_req.Requester.RequesterNetwork, _cpu.Cpu.Network);

			if (hasCpu && hasMachine) _tickOfDeath = 0;
			else _tickOfDeath += 60;
		}

		if (_tickOfDeath > 60)
		{
			Cancel();
			return true;
		}
		return false;
	}

	public void Cancel()
	{
		_canceled = true;
		if (_req != null)
		{
			_req.SetCanceled(true);
			_req.Requester?.JobStateChange(_req);
		}
		if (_cpu != null)
			_cpu.SetCanceled(true);
	}

	public void Remove(CraftingLink link)
	{
		if (_req == link) _req = null;
		else if (_cpu == link) _cpu = null;
	}

	public void Add(CraftingLink link)
	{
		if (link.Cpu != null) _cpu = link;
		else if (link.Requester != null) _req = link;
	}

	public bool IsCanceled => _canceled;
	public bool IsDone => _done;

	public void MarkDone()
	{
		_done = true;
		if (_req != null)
		{
			_req.SetDone(true);
			_req.Requester?.JobStateChange(_req);
		}
		if (_cpu != null)
			_cpu.SetDone(true);
	}

	public bool IsRequester(IMeCraftingRequester requester) =>
		_req != null && _req.Requester == requester;

	public void RemoveNode()
	{
		_req?.SetNexus(null);
		_req = null;
		_tickOfDeath = 0;
	}

	public CraftingLink? Request => _req;
}
