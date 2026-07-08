// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.helpers.InventoryAction), Forge 1.20.1. LGPL-3.0-only (see AE2 LICENSE).

#nullable enable
namespace GregTechCEuTerraria.AppliedEnergistics.Helpers;

public enum InventoryAction
{
	PICKUP_OR_SET_DOWN, SPLIT_OR_PLACE_SINGLE, CREATIVE_DUPLICATE, SHIFT_CLICK,

	CRAFT_STACK, CRAFT_ITEM, CRAFT_SHIFT, CRAFT_ALL,

	FILL_ITEM, EMPTY_ITEM,

	MOVE_REGION, PICKUP_SINGLE, ROLL_UP, ROLL_DOWN, AUTO_CRAFT, PLACE_SINGLE,

	SET_FILTER,
}
