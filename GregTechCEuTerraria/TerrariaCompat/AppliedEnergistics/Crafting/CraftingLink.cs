// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.CraftingLink), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class CraftingLink
{
	private readonly IMeCraftingRequester? _req;
	private readonly CraftingCpuLogic? _cpu;
	private readonly Guid _craftId;
	private readonly bool _standalone;
	private bool _canceled;
	private bool _done;
	private CraftingLinkNexus? _tie;

	public CraftingLink(Guid craftId, bool standalone, IMeCraftingRequester req)
	{
		_craftId = craftId;
		_standalone = standalone;
		_req = req;
		_cpu = null;
	}

	public CraftingLink(Guid craftId, bool standalone, CraftingCpuLogic cpu)
	{
		_craftId = craftId;
		_standalone = standalone;
		_cpu = cpu;
		_req = null;
	}

	public CraftingLink(TagCompound data, IMeCraftingRequester req)
	{
		_craftId = Guid.TryParse(data.GetString("craftId"), out var g) ? g : Guid.NewGuid();
		_canceled = data.GetBool("canceled");
		_done = data.GetBool("done");
		_standalone = data.GetBool("standalone");
		if (!data.ContainsKey("req") || !data.GetBool("req"))
			throw new InvalidOperationException("Invalid Crafting Link for Object");
		_req = req;
		_cpu = null;
	}

	public CraftingLink(TagCompound data, CraftingCpuLogic cpu)
	{
		_craftId = Guid.TryParse(data.GetString("craftId"), out var g) ? g : Guid.NewGuid();
		_canceled = data.GetBool("canceled");
		_done = data.GetBool("done");
		_standalone = data.GetBool("standalone");
		if (!data.ContainsKey("req") || data.GetBool("req"))
			throw new InvalidOperationException("Invalid Crafting Link for Object");
		_cpu = cpu;
		_req = null;
	}

	public Guid CraftId => _craftId;
	public IMeCraftingRequester? Requester => _req;
	public CraftingCpuLogic? Cpu => _cpu;
	public bool IsStandalone => _standalone;

	public bool IsCanceled
	{
		get
		{
			if (_canceled) return true;
			if (_done) return false;
			return _tie != null && _tie.IsCanceled;
		}
	}

	public bool IsDone
	{
		get
		{
			if (_done) return true;
			if (_canceled) return false;
			return _tie != null && _tie.IsDone;
		}
	}

	public void Cancel()
	{
		if (_done) return;
		_canceled = true;
		_tie?.Cancel();
		_tie = null;
	}

	public long Insert(AEKey what, long amount, Actionable mode)
	{
		if (_tie == null || _tie.Request == null || _tie.Request.Requester == null)
			return 0;
		if (_tie.IsCanceled)
			return 0;
		return _tie.Request.Requester.InsertCraftedItems(_tie.Request, what, amount, mode);
	}

	public void MarkDone() => _tie?.MarkDone();

	public void SetCanceled(bool canceled) => _canceled = canceled;
	public void SetDone(bool done) => _done = done;

	public void SetNexus(CraftingLinkNexus? n)
	{
		_tie?.Remove(this);

		if (IsCanceled && n != null)
		{
			n.Cancel();
			_tie = null;
			return;
		}

		_tie = n;
		n?.Add(this);
	}

	public void WriteToNBT(TagCompound tag)
	{
		tag["craftId"] = _craftId.ToString();
		tag["canceled"] = IsCanceled;
		tag["done"] = IsDone;
		tag["standalone"] = _standalone;
		tag["req"] = _req != null;
	}
}
