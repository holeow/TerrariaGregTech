// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.execution.CraftingCpuHelper), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public static partial class CraftingCpuHelper
{
	public static GenericStack? TryExtractInitialItems(CraftingPlan plan, MEStorage storage,
		ListCraftingInventory cpuInventory, IActionSource src)
	{
		foreach (var entry in plan.UsedItems)
		{
			var what = entry.Key;
			var toExtract = entry.Value;
			var extracted = storage.Extract(what, toExtract, Actionable.MODULATE, src);
			cpuInventory.Insert(what, extracted, Actionable.MODULATE);

			if (extracted < toExtract)
			{
				foreach (var stored in cpuInventory.List)
					storage.Insert(stored.Key, stored.Value, Actionable.MODULATE, src);
				cpuInventory.Clear();
				return new GenericStack(what, toExtract - extracted);
			}
		}
		return null;
	}

	public static KeyCounter[]? ExtractPatternInputs(MePattern details, ICraftingInventory sourceInv,
		KeyCounter expectedOutputs, KeyCounter expectedContainerItems)
	{
		var inputs = details.Inputs;
		var inputHolder = new KeyCounter[inputs.Count];
		bool found = true;

		for (int x = 0; x < inputs.Count; x++)
		{
			var list = inputHolder[x] = new KeyCounter();
			long remainingMultiplier = 1;
			foreach (var template in GetValidItemTemplates(inputs[x].what, inputs[x].amount, details.InputTag(x)))
			{
				long extracted = ExtractTemplates(sourceInv, template, remainingMultiplier);
				if (extracted <= 0) continue;

				list.Add(template.Key, extracted * template.Amount);

				var containerItem = RemainingKey(template.Key);
				if (containerItem != null)
					expectedContainerItems.Add(containerItem, extracted);

				remainingMultiplier -= extracted;
				if (remainingMultiplier == 0) break;
			}
			if (remainingMultiplier > 0) { found = false; break; }
		}

		if (!found)
		{
			ReinjectPatternInputs(sourceInv, inputHolder);
			return null;
		}

		foreach (var (what, amount) in details.Outputs)
			expectedOutputs.Add(what, amount);

		return inputHolder;
	}

	internal static AEKey? RemainingKey(AEKey key) => null;

	public static void ReinjectPatternInputs(ICraftingInventory sourceInv, KeyCounter[] inputHolder)
	{
		foreach (var list in inputHolder)
			if (list != null)
				foreach (var entry in list)
					sourceInv.Insert(entry.Key, entry.Value, Actionable.MODULATE);
	}

	public static IEnumerable<InputTemplate> GetValidItemTemplates(AEKey what, long amount) =>
		GetValidItemTemplates(what, amount, null);

	public static IEnumerable<InputTemplate> GetValidItemTemplates(AEKey what, long amount, string? tag)
	{
		if (tag != null)
		{
			var resolver = Api.Recipe.Ingredient.IIngredientResolver.Default;
			if (what is AEFluidKey)
			{
				var fluids = resolver?.ResolveFluidTag(tag);
				if (fluids is { Count: > 0 })
				{
					foreach (var f in fluids)
						yield return new InputTemplate(AEFluidKey.Of(f), amount);
					yield break;
				}
			}
			else
			{
				var members = resolver?.ResolveItemTag(tag);
				if (members is { Count: > 0 })
				{
					foreach (int type in members)
						yield return new InputTemplate(AEItemKey.OfType(type), amount);
					yield break;
				}
			}
		}
		yield return new InputTemplate(what, amount);
	}

	public static long ExtractTemplates(ICraftingInventory inv, InputTemplate template, long multiplier)
	{
		long maxTotal = template.Amount * multiplier;
		var extracted = inv.Extract(template.Key, maxTotal, Actionable.SIMULATE);
		if (extracted == 0)
			return 0;
		multiplier = extracted / template.Amount;
		maxTotal = template.Amount * multiplier;
		if (maxTotal == 0)
			return 0;
		extracted = inv.Extract(template.Key, maxTotal, Actionable.MODULATE);
		if (extracted == 0 || extracted != maxTotal)
			throw new InvalidOperationException("Failed to correctly extract whole number. Invalid simulation!");
		return multiplier;
	}
}
