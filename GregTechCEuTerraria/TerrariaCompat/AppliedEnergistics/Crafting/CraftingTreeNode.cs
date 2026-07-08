// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.CraftingTreeNode), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.

#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class CraftingTreeNode
{
	private readonly MeNetwork _net;
	private readonly CraftingCalculation _job;
	private readonly CraftingTreeProcess? _parent;
	private readonly bool _isTopLevel;
	internal readonly AEKey What;
	private readonly long _amount;
	private List<CraftingTreeProcess>? _nodes;

	private readonly string? _tag;

	public CraftingTreeNode(MeNetwork net, CraftingCalculation job, AEKey what, long amount,
		CraftingTreeProcess? par, int slot, string? tag = null)
	{
		_net = net;
		_parent = par;
		_isTopLevel = slot == -1;
		_job = job;
		_tag = tag;
		_amount = amount;
		What = ChooseCraftedStack(net, what, tag);
	}

	private static AEKey ChooseCraftedStack(MeNetwork net, AEKey what, string? tag)
	{
		if (tag == null) return what;
		var resolver = Api.Recipe.Ingredient.IIngredientResolver.Default;
		if (what is AEFluidKey)
		{
			var fluids = resolver?.ResolveFluidTag(tag);
			if (fluids is not { Count: > 0 }) return what;
			foreach (var f in fluids)
			{
				var key = AEFluidKey.Of(f);
				if (net.GetCraftingFor(key).Count > 0) return key;
			}
			return AEFluidKey.Of(fluids[0]);
		}
		var members = resolver?.ResolveItemTag(tag);
		if (members is not { Count: > 0 }) return what;
		foreach (int type in members)
		{
			var key = AEItemKey.OfType(type);
			if (net.GetCraftingFor(key).Count > 0) return key;
		}
		return AEItemKey.OfType(members[0]);
	}

	internal bool NotRecursive(MePattern details)
	{
		foreach (var output in details.Outputs)
			if (What.Equals(output.what))
				return false;

		foreach (var input in details.Inputs)
			if (What.Equals(input.what))
				return false;

		if (_parent == null)
			return true;

		return _parent.NotRecursive(details);
	}

	private void BuildChildPatterns()
	{
		if (_nodes == null)
		{
			_nodes = new List<CraftingTreeProcess>();
			foreach (var details in _net.GetCraftingFor(What))
				if (_parent == null || _parent.NotRecursive(details))
					_nodes.Add(new CraftingTreeProcess(_net, _job, details, this));
		}
	}

	public void Request(CraftingSimulationState inv, long requestedAmount, KeyCounter? containerItems)
	{
		_job.HandlePausing();

		inv.AddStackBytes(What, _amount, requestedAmount);

		foreach (var template in GetCollectTemplates())
		{
			long extracted = CraftingCpuHelper.ExtractTemplates(inv, template, requestedAmount);
			if (extracted > 0)
			{
				requestedAmount -= extracted;
				AddContainerItems(template.Key, extracted, containerItems);
				if (requestedAmount == 0)
					return;
			}
		}

		AddContainerItems(What, requestedAmount, containerItems);

		BuildChildPatterns();
		long totalRequestedItems = requestedAmount * _amount;
		if (_nodes!.Count == 1)
		{
			var pro = _nodes[0];
			var craftedPerPattern = pro.GetOutputCount(What);

			while (pro.Possible && totalRequestedItems > 0)
			{
				long times = pro.LimitsQuantity()
					? 1
					: (totalRequestedItems + craftedPerPattern - 1) / craftedPerPattern;
				pro.Request(inv, times);

				var available = inv.Extract(What, totalRequestedItems, Actionable.MODULATE);
				if (available != 0)
				{
					totalRequestedItems -= available;
					if (totalRequestedItems <= 0)
						return;
				}
				else
				{
					throw new System.InvalidOperationException(
						$"Unexpected error in the crafting calculation: can't find created items ({What}).");
				}
			}
		}
		else if (_nodes.Count > 1)
		{
			foreach (var pro in _nodes)
			{
				try
				{
					while (pro.Possible && totalRequestedItems > 0)
					{
						var child = new ChildCraftingSimulationState(inv);
						pro.Request(child, 1);

						var available = child.Extract(What, totalRequestedItems, Actionable.MODULATE);
						if (available != 0)
						{
							child.ApplyDiff(inv);
							totalRequestedItems -= available;
							if (totalRequestedItems <= 0)
								return;
						}
						else
						{
							pro.Possible = false;
						}
					}
				}
				catch (CraftBranchFailure)
				{
					pro.Possible = true;
				}
			}
		}

		if (_job.IsSimulation)
			_job.AddMissing(What, totalRequestedItems);
		else
			throw new CraftBranchFailure(What, totalRequestedItems);
	}

	private static void AddContainerItems(AEKey key, long count, KeyCounter? containerItems)
	{
		if (containerItems == null) return;
		var rem = CraftingCpuHelper.RemainingKey(key);
		if (rem != null) containerItems.Add(rem, count);
	}

	private IEnumerable<InputTemplate> GetCollectTemplates()
	{
		if (_isTopLevel)
			return new[] { new InputTemplate(What, 1) };
		return CraftingCpuHelper.GetValidItemTemplates(What, _amount, _tag);
	}

	internal long GetNodeCount()
	{
		long tot = 1;
		if (_nodes != null)
			foreach (var pro in _nodes)
				tot += pro.GetNodeCount();
		return tot;
	}

	internal bool HasMultiplePaths()
	{
		if (_nodes == null)
			return false;
		if (_nodes.Count > 1)
			return true;
		foreach (var pro in _nodes)
			if (pro.HasMultiplePaths())
				return true;
		return false;
	}
}
