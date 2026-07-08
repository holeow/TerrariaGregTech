// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.config.TypeFilter), Forge 1.20.1. Original is unheadered; AE2 is
// LGPL-3.0-only. See AE2 LICENSE.

#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Config;

public enum TypeFilter
{
	ALL,
	ITEMS,
	FLUIDS,
}

public static class TypeFilters
{
	public static AEKeyFilter GetFilter(this TypeFilter filter) => filter switch
	{
		TypeFilter.ITEMS  => AEKeyType.Items().Filter(),
		TypeFilter.FLUIDS => AEKeyType.Fluids().Filter(),
		_                 => AEKeyFilter.None(),
	};
}
