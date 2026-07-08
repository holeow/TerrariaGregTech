// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.stacks.AEItemKeys), Forge 1.20.1. Original LGPL header preserved
// verbatim below per AE2's license terms.
//
// This file is part of Applied Energistics 2.
// Copyright (c) 2021, TeamAppliedEnergistics, All rights reserved.
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
using System.Collections.Generic;
using System.IO;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using GregTechCEuTerraria.Api.Cover.Filter;
using GregTechCEuTerraria.AppliedEnergistics.Core;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

internal sealed class AEItemKeys : AEKeyType
{
	internal static readonly AEItemKeys INSTANCE = new();

	private AEItemKeys() : base("ae2:i", typeof(AEItemKey), Language.GetText(AELocale.KeyTypeItems))
	{
	}

	public override AEKey? ReadFromPacket(BinaryReader input) => AEItemKey.FromPacket(input);

	public override AEKey? LoadKeyFromTag(TagCompound tag) => AEItemKey.FromTag(tag);

	public override IReadOnlyCollection<string> GetTagNames() => TagSource.AllItemTagNames();
}
