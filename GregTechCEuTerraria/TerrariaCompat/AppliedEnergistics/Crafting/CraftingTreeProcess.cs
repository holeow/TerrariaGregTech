// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.CraftingTreeProcess), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.

#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class CraftingTreeProcess
{
	private readonly CraftingTreeNode _parent;
	internal readonly MePattern Details;
	private readonly CraftingCalculation _job;
	private readonly List<(CraftingTreeNode node, long multiplier)> _nodes = new();
	internal bool Possible = true;
	private bool _limitQty;
	private bool _containerItems;

	public CraftingTreeProcess(MeNetwork net, CraftingCalculation job, MePattern details, CraftingTreeNode parentNode)
	{
		_parent = parentNode;
		Details = details;
		_job = job;

		UpdateLimitQty();

		var inputs = details.Inputs;
		for (int x = 0; x < inputs.Count; x++)
		{
			var (what, amount) = inputs[x];
			_nodes.Add((new CraftingTreeNode(net, job, what, amount, this, x, details.InputTag(x)), 1));
		}
	}

	internal bool NotRecursive(MePattern details)
		=> _parent == null || _parent.NotRecursive(details);

	private void UpdateLimitQty()
	{
		foreach (var input in Details.Inputs)
		{
			bool isAnInput = false;
			foreach (var output in Details.Outputs)
				if (output.what.Equals(input.what))
				{
					isAnInput = true;
					break;
				}
			if (isAnInput)
				_limitQty = true;

			if (CraftingCpuHelper.RemainingKey(input.what) != null)
				_limitQty = _containerItems = true;
		}
	}

	internal bool LimitsQuantity() => _limitQty;

	public void Request(CraftingSimulationState inv, long times)
	{
		_job.HandlePausing();

		var containerItems = _containerItems ? new KeyCounter() : null;

		foreach (var (node, multiplier) in _nodes)
			node.Request(inv, multiplier * times, containerItems);

		if (containerItems != null)
			foreach (var stack in containerItems)
			{
				inv.Insert(stack.Key, stack.Value, Actionable.MODULATE);
				inv.AddStackBytes(stack.Key, stack.Value, 1);
			}

		foreach (var (what, amount) in Details.Outputs)
			inv.Insert(what, amount * times, Actionable.MODULATE);

		inv.AddCrafting(Details, times);
		inv.AddBytes(times);
	}

	internal long GetNodeCount()
	{
		long tot = 0;
		foreach (var (node, _) in _nodes)
			tot += node.GetNodeCount();
		return tot;
	}

	internal long GetOutputCount(AEKey what)
	{
		long tot = 0;
		foreach (var (w, amount) in Details.Outputs)
			if (what.Equals(w))
				tot += amount;
		return tot;
	}

	internal bool HasMultiplePaths()
	{
		foreach (var (node, _) in _nodes)
			if (node.HasMultiplePaths())
				return true;
		return false;
	}
}
