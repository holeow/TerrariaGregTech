// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2 Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public enum CraftingJobStatus { Started, Finished, Cancelled }

public interface ICraftingCpuHost
{
	bool IsOnline { get; }
	long AvailableStorage { get; }
	int CoProcessors { get; }
	MeNetwork? Network { get; }
	IActionSource Src { get; }

	void UpdateOutput(GenericStack? what);
	void MarkDirty();
	void NotifyJobStatus(ExecutingCraftingJob job, CraftingJobStatus status) { }
}
