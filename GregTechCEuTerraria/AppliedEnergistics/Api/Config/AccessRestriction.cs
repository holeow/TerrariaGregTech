// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.config.AccessRestriction), Forge 1.20.1. LGPL-3.0-only / unheadered
// API. See AE2 LICENSE.

#nullable enable
namespace GregTechCEuTerraria.AppliedEnergistics.Api.Config;

public enum AccessRestriction
{
	NO_ACCESS,
	READ,
	WRITE,
	READ_WRITE,
}

public static class AccessRestrictions
{
	public static bool IsAllowExtraction(this AccessRestriction a) =>
		a == AccessRestriction.READ || a == AccessRestriction.READ_WRITE;

	public static bool IsAllowInsertion(this AccessRestriction a) =>
		a == AccessRestriction.WRITE || a == AccessRestriction.READ_WRITE;
}
