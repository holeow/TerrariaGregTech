// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.storage.SupplierStorage), Forge 1.20.1. Original is unheadered; AE2
// is LGPL-3.0-only (older API files MIT). See AE2's LICENSE.

#nullable enable
using System;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Me.Storage;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

public sealed class SupplierStorage : MEStorage
{
	private readonly Func<MEStorage?> _supplier;

	public SupplierStorage(Func<MEStorage?> supplier) => _supplier = supplier;

	private MEStorage GetDelegate() => _supplier() ?? NullInventory.Of();

	public bool IsPreferredStorageFor(AEKey what, IActionSource source) =>
		GetDelegate().IsPreferredStorageFor(what, source);

	public long Insert(AEKey what, long amount, Actionable mode, IActionSource source) =>
		GetDelegate().Insert(what, amount, mode, source);

	public long Extract(AEKey what, long amount, Actionable mode, IActionSource source) =>
		GetDelegate().Extract(what, amount, mode, source);

	public void GetAvailableStacks(KeyCounter @out) => GetDelegate().GetAvailableStacks(@out);

	public string GetDescription() => GetDelegate().GetDescription();

	public KeyCounter GetAvailableStacks() => GetDelegate().GetAvailableStacks();
}
