// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.menu.me.common.IClientRepo), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System.Collections.Generic;

namespace GregTechCEuTerraria.AppliedEnergistics.Menu.Me.Common;

public interface IClientRepo
{
	void HandleUpdate(bool fullUpdate, List<GridInventoryEntry> entries);

	ICollection<GridInventoryEntry> GetAllEntries();
}
