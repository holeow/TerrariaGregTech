// Adapted for GregTechCEuTerraria from Applied Energistics 2's crafting provider surface
// (appeng.api.networking.crafting.ICraftingProvider), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public interface IMeCraftingProvider
{
	bool IsBusy { get; }

	bool PushPattern(MePattern details, KeyCounter[] inputHolder);

	bool CanFulfill(MePattern details);
}

public interface IMeCraftingRequester
{
	System.Collections.Generic.IReadOnlyCollection<CraftingLink> GetRequestedJobs();
	long InsertCraftedItems(CraftingLink link, AEKey what, long amount, Actionable mode);
	void JobStateChange(CraftingLink link);
	Pipelike.Me.MeNetwork? RequesterNetwork { get; }
}
