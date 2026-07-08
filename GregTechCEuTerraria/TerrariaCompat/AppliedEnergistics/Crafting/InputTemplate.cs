// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.execution.InputTemplate), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public readonly record struct InputTemplate(AEKey Key, long Amount);
