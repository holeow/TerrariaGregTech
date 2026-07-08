#nullable enable
using System;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public sealed class DelegatingMeStorage : MEStorage
{
	private readonly Func<MEStorage?> _resolve;
	private readonly string _description;

	public DelegatingMeStorage(Func<MEStorage?> resolve, string description = "ME Interface")
	{
		_resolve = resolve;
		_description = description;
	}

	public long Insert(AEKey what, long amount, Actionable mode, IActionSource source) =>
		_resolve()?.Insert(what, amount, mode, source) ?? 0;

	public long Extract(AEKey what, long amount, Actionable mode, IActionSource source) =>
		_resolve()?.Extract(what, amount, mode, source) ?? 0;

	public void GetAvailableStacks(KeyCounter @out) => _resolve()?.GetAvailableStacks(@out);

	public string GetDescription() => _description;
}
