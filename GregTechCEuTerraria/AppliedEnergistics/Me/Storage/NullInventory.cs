// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.me.storage.NullInventory), Forge 1.20.1. Original LGPL header preserved
// verbatim below per AE2's license terms.
//
// This file is part of Applied Energistics 2.
// Copyright (c) 2013 - 2014, AlgorithmX2, All rights reserved.
//
// Applied Energistics 2 is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Applied Energistics 2 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with Applied Energistics 2.  If not, see <http://www.gnu.org/licenses/lgpl>.

#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

namespace GregTechCEuTerraria.AppliedEnergistics.Me.Storage;

public class NullInventory : MEStorage
{
	private static readonly NullInventory NULL_INVENTORY = new();

	public static MEStorage Of() => NULL_INVENTORY;

	public void GetAvailableStacks(KeyCounter @out) { }

	public string GetDescription() => "";
}
