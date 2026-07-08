#nullable enable
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public sealed class MeNetworkPushHandler : IItemHandler, IFluidHandler
{
	private readonly MetaMachine _host;
	public MeNetworkPushHandler(MetaMachine host) => _host = host;

	public int SlotCount => 1;
	public Item GetSlot(int slot) => new();
	public Item Insert(int slot, Item item, bool simulate)
	{
		var leftover = MeNetworkSystem.PushItemIntoNet(_host, item, simulate);
		if (!simulate && _host is PatternProviderMachine pp)
		{
			int moved = item.stack - (leftover.IsAir ? 0 : leftover.stack);
			if (moved > 0 && AEItemKey.Of(item) is { } key)
				pp.OnStackReturnedToNetwork(key, moved);
		}
		return leftover;
	}
	public Item Extract(int slot, int maxAmount, bool simulate) => new();

	public int TankCount => 1;
	public FluidStack GetTank(int tank) => FluidStack.Empty;
	public int GetCapacity(int tank) => int.MaxValue;
	public bool IsFluidValid(int tank, FluidStack fluid) => true;
	public int Fill(FluidStack fluid, bool simulate)
	{
		int filled = MeNetworkSystem.FillFluidIntoNet(_host, fluid, simulate);
		if (!simulate && filled > 0 && _host is PatternProviderMachine pp && AEFluidKey.Of(fluid) is { } key)
			pp.OnStackReturnedToNetwork(key, filled);
		return filled;
	}
	public FluidStack Drain(int maxAmount, bool simulate) => FluidStack.Empty;
	public FluidStack Drain(FluidStack fluidStack, bool simulate) => FluidStack.Empty;
}
