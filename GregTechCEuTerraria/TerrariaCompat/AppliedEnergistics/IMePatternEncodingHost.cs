#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;

public interface IMePatternEncodingHost
{
	MetaMachine Machine { get; }
	PatternEncodingState Encoding { get; }
	MeNetwork? Network { get; }
	SlotGroup BlankSlotGroup { get; }
	SlotGroup EncodedSlotGroup { get; }
	bool IsEncodingActive { get; }
}
