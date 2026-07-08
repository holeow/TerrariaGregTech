#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using Terraria.DataStructures;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public interface IMePatternProvider : IMeNetworkConnected
{
	IReadOnlyList<MePattern> Patterns { get; }

	string ProviderName { get; }

	Point16 ProviderPos { get; }

	bool IsVisibleInTerminal { get; }

	int TerminalIconItemType { get; }
}
