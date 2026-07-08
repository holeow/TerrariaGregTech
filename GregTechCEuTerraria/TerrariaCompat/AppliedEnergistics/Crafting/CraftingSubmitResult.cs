// Adapted for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.execution.CraftingSubmitResult + ICraftingSubmitResult), Forge 1.20.1.
// LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Core;
using Terraria.Localization;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public enum CraftingSubmitCode
{
	Ok,
	IncompletePlan,
	NoCpuFound,
	CpuBusy,
	CpuOffline,
	CpuTooSmall,
	MissingIngredient
}

public readonly record struct CraftingSubmitResult(CraftingSubmitCode Code, GenericStack? MissingIngredient, CraftingLink? Link = null)
{
	public static readonly CraftingSubmitResult IncompletePlan = new(CraftingSubmitCode.IncompletePlan, null);
	public static readonly CraftingSubmitResult NoCpuFound = new(CraftingSubmitCode.NoCpuFound, null);
	public static readonly CraftingSubmitResult CpuBusy = new(CraftingSubmitCode.CpuBusy, null);
	public static readonly CraftingSubmitResult CpuOffline = new(CraftingSubmitCode.CpuOffline, null);
	public static readonly CraftingSubmitResult CpuTooSmall = new(CraftingSubmitCode.CpuTooSmall, null);
	public static CraftingSubmitResult Successful(CraftingLink? link = null) => new(CraftingSubmitCode.Ok, null, link);
	public static CraftingSubmitResult Missing(GenericStack ingredient)
		=> new(CraftingSubmitCode.MissingIngredient, ingredient);

	public bool IsSuccess => Code == CraftingSubmitCode.Ok;

	public string Describe() => Code switch
	{
		CraftingSubmitCode.Ok             => Language.GetTextValue(AELocale.CraftSubmitOk),
		CraftingSubmitCode.IncompletePlan => Language.GetTextValue(AELocale.CraftSubmitIncompletePlan),
		CraftingSubmitCode.NoCpuFound     => Language.GetTextValue(AELocale.CraftSubmitNoCpu),
		CraftingSubmitCode.CpuBusy        => Language.GetTextValue(AELocale.CraftSubmitCpuBusy),
		CraftingSubmitCode.CpuOffline     => Language.GetTextValue(AELocale.CraftSubmitCpuOffline),
		CraftingSubmitCode.CpuTooSmall    => Language.GetTextValue(AELocale.CraftSubmitCpuTooSmall),
		CraftingSubmitCode.MissingIngredient =>
			MissingIngredient != null
				? Language.GetTextValue(AELocale.CraftSubmitMissing, MissingIngredient.Amount, MissingIngredient.What.GetDisplayName())
				: Language.GetTextValue(AELocale.CraftSubmitMissingGeneric),
		_ => "",
	};
}
