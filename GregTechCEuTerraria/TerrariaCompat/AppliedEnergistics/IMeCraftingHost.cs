#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;

public interface IMeCraftingHost
{
	MetaMachine Machine { get; }
	CraftingStationState Crafting { get; }
	MeNetwork? Network { get; }
	SlotGroup StationSlotGroup { get; }
	bool IsCraftingActive { get; }
}
