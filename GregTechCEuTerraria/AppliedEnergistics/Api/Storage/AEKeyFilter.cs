// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.storage.AEKeyFilter), Forge 1.20.1. Original is unheadered; AE2 is
// LGPL-3.0-only (older API files MIT). See AE2's LICENSE.

#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

public interface AEKeyFilter
{
	static AEKeyFilter None() => NoOpKeyFilter.INSTANCE;

	bool Matches(AEKey what);
}
