// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.inv.ListCraftingInventory), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class ListCraftingInventory : ICraftingInventory
{
	public readonly KeyCounter List = new();
	private readonly Action<AEKey> _listener;

	public ListCraftingInventory(Action<AEKey> listener) => _listener = listener;

	public void Insert(AEKey what, long amount, Actionable mode)
	{
		if (mode == Actionable.MODULATE)
		{
			List.Add(what, amount);
			_listener(what);
		}
	}

	public long Extract(AEKey what, long amount, Actionable mode)
	{
		var available = List.Get(what);
		var extracted = Math.Min(available, amount);
		if (mode == Actionable.MODULATE)
		{
			if (available > extracted) List.Remove(what, extracted);
			else List.Remove(what);
			_listener(what);
		}
		return extracted;
	}

	public void Clear()
	{
		foreach (var stack in List)
		{
			List.Set(stack.Key, 0);
			_listener(stack.Key);
		}
		List.RemoveZeros();
	}

	public void ReadFromNBT(IList<TagCompound> data)
	{
		List.Clear();
		if (data == null) return;
		foreach (var compound in data)
		{
			var key = AEKey.FromTagGeneric(compound);
			if (key != null)
				Insert(key, compound.GetLong("#"), Actionable.MODULATE);
		}
	}

	public List<TagCompound> WriteToNBT()
	{
		var tag = new List<TagCompound>();
		foreach (var entry in List)
		{
			var entryTag = entry.Key.ToTagGeneric();
			entryTag["#"] = entry.Value;
			tag.Add(entryTag);
		}
		return tag;
	}
}
