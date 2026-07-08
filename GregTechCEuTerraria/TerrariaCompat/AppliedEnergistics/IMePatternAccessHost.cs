#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;

public interface IMePatternAccessHost
{
	MetaMachine Machine { get; }
	MeNetwork? Network { get; }
	IReadOnlyList<IMePatternProvider> Providers { get; }
}
