// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.inv.ICraftingInventory + ICraftingSimulationState), Forge 1.20.1.
// LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public interface ICraftingInventory
{
	void Insert(AEKey what, long amount, Actionable mode);

	long Extract(AEKey what, long amount, Actionable mode);
}

public interface ICraftingSimulationState : ICraftingInventory
{
	void EmitItems(AEKey what, long amount);

	void AddBytes(double bytes);

	void AddStackBytes(AEKey key, long amount, long multiplier)
		=> AddBytes((double)amount * multiplier / key.GetAmountPerByte() * 8);

	void AddCrafting(MePattern details, long crafts);
}
