// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.config.LockCraftingMode), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
namespace GregTechCEuTerraria.AppliedEnergistics.Api.Config;

public enum LockCraftingMode
{
	None,
	LockWhileHigh,
	LockWhileLow,
	LockUntilPulse,
	LockUntilResult,
}
