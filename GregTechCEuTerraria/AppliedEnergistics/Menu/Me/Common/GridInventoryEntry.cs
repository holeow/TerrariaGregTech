// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.menu.me.common.GridInventoryEntry), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.AppliedEnergistics.Menu.Me.Common;

public sealed class GridInventoryEntry
{
	public long Serial { get; }
	public AEKey? What { get; }
	public long StoredAmount { get; }
	public long RequestableAmount { get; }
	public bool Craftable { get; }

	public GridInventoryEntry(long serial, AEKey? what, long storedAmount, long requestableAmount, bool craftable)
	{
		Serial = serial;
		What = what;
		StoredAmount = storedAmount;
		RequestableAmount = requestableAmount;
		Craftable = craftable;
	}

	public bool IsMeaningful() => StoredAmount > 0 || RequestableAmount > 0 || Craftable;
}
