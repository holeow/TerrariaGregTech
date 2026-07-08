// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.CraftingCalculation), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.

#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public enum CalculationStrategy
{
	ReportMissingItems,
	CraftLess
}

public sealed class CraftingCalculation
{
	private const long MaxOps = 2_000_000;

	private readonly NetworkCraftingSimulationState _networkInv;
	private readonly KeyCounter _missing = new();
	private readonly CraftingTreeNode _tree;
	private readonly AEKey _output;
	private readonly long _requestedAmount;
	private readonly CalculationStrategy _strategy;
	private bool _simulate = false;
	private long _ops = 0;

	public CraftingCalculation(MeNetwork net, GenericStack output, CalculationStrategy strategy, IActionSource src)
	{
		_output = output.What;
		_requestedAmount = output.Amount;
		_strategy = strategy;

		_networkInv = new NetworkCraftingSimulationState(net.GetStorage(), src);
		_tree = new CraftingTreeNode(net, this, _output, 1, null, -1);
	}

	internal void AddMissing(AEKey what, long amount) => _missing.Add(what, amount);

	public CraftingPlan Run()
	{
		try
		{
			return ComputePlan();
		}
		catch (CraftingTooComplexException)
		{
			_simulate = true;
			var miss = new KeyCounter();
			miss.Add(_output, _requestedAmount);
			return new CraftingPlan(new GenericStack(_output, _requestedAmount), 0, true, false,
				new KeyCounter(), new KeyCounter(), miss, new());
		}
	}

	private CraftingPlan ComputePlan()
	{
		var fullAmountPlan = RunCraftAttempt(false, _requestedAmount);
		if (fullAmountPlan != null)
			return fullAmountPlan;

		if (_strategy == CalculationStrategy.CraftLess)
		{
			long successfulAmount = 0;
			CraftingPlan? successfulPlan = null;
			for (long increment = HighestOneBit(_requestedAmount); increment > 0; increment /= 2)
			{
				long testAmount = successfulAmount + increment;
				if (testAmount < _requestedAmount)
				{
					var plan = RunCraftAttempt(false, testAmount);
					if (plan != null)
					{
						successfulAmount = testAmount;
						successfulPlan = plan;
					}
				}
			}
			if (successfulPlan != null)
				return successfulPlan;
		}

		return RunCraftAttempt(true, _requestedAmount)!;
	}

	private CraftingPlan? RunCraftAttempt(bool simulate, long amount)
	{
		_simulate = simulate;

		var craftingInventory = new ChildCraftingSimulationState(_networkInv);
		craftingInventory.Ignore(_output);

		try
		{
			_tree.Request(craftingInventory, amount, null);
		}
		catch (CraftBranchFailure)
		{
			return null;
		}

		craftingInventory.AddBytes(_tree.GetNodeCount() * 8);

		return CraftingSimulationState.BuildCraftingPlan(craftingInventory, this, amount);
	}

	internal void HandlePausing()
	{
		if (++_ops > MaxOps)
			throw new CraftingTooComplexException();
	}

	public bool IsSimulation => _simulate;
	public AEKey Output => _output;
	public KeyCounter GetMissingItems() => _missing;
	public bool HasMultiplePaths => _tree.HasMultiplePaths();

	private static long HighestOneBit(long value)
	{
		if (value <= 0) return 0;
		long bit = 1;
		while ((bit << 1) > 0 && (bit << 1) <= value)
			bit <<= 1;
		return bit;
	}
}
