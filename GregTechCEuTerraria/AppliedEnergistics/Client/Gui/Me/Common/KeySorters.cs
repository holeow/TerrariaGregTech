// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.client.gui.me.common.KeySorters), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Me.Common;

internal static class KeySorters
{
	public static readonly IComparer<AEKey> NameAsc = Comparer<AEKey>.Create(
		(a, b) => string.Compare(a.GetDisplayName(), b.GetDisplayName(), StringComparison.OrdinalIgnoreCase));

	public static readonly IComparer<AEKey> NameDesc = Reversed(NameAsc);

	public static readonly IComparer<AEKey> ModAsc = Comparer<AEKey>.Create((a, b) =>
	{
		int m = string.Compare(a.GetModId(), b.GetModId(), StringComparison.OrdinalIgnoreCase);
		return m != 0 ? m : NameAsc.Compare(a, b);
	});

	public static readonly IComparer<AEKey> ModDesc = Reversed(ModAsc);

	public static IComparer<AEKey> GetComparator(SortOrder order, SortDir dir) => order switch
	{
		SortOrder.NAME => dir == SortDir.ASCENDING ? NameAsc : NameDesc,
		SortOrder.MOD  => dir == SortDir.ASCENDING ? ModAsc : ModDesc,
		SortOrder.AMOUNT => throw new NotSupportedException(),
		_ => NameAsc,
	};

	private static IComparer<AEKey> Reversed(IComparer<AEKey> c) =>
		Comparer<AEKey>.Create((a, b) => c.Compare(b, a));
}
