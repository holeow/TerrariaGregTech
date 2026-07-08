// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.storage.NoOpKeyFilter), Forge 1.20.1. Original is unheadered; AE2
// is LGPL-3.0-only (older API files MIT). See AE2's LICENSE.

#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

internal sealed class NoOpKeyFilter : AEKeyFilter
{
	internal static readonly NoOpKeyFilter INSTANCE = new();

	public bool Matches(AEKey what) => true;
}
