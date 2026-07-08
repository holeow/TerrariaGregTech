// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.CraftingPlan + appeng.api.networking.crafting.ICraftingPlan),
// Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed record CraftingPlan(
	GenericStack FinalOutput,
	long Bytes,
	bool Simulation,
	bool MultiplePaths,
	KeyCounter UsedItems,
	KeyCounter EmittedItems,
	KeyCounter MissingItems,
	Dictionary<MePattern, long> PatternTimes);
