// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.helpers.externalstorage.GenericStackInv), Forge 1.20.1. Original LGPL
// header preserved verbatim below per AE2's license terms.
//
// This file is part of Applied Energistics 2.
// Copyright (c) 2021, TeamAppliedEnergistics, All rights reserved.
//
// Applied Energistics 2 is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Applied Energistics 2 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with Applied Energistics 2.  If not, see <http://www.gnu.org/licenses/lgpl>.

#nullable enable
using System;
using System.Collections.Generic;
using Terraria.ModLoader.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using GregTechCEuTerraria.AppliedEnergistics.Core;

namespace GregTechCEuTerraria.AppliedEnergistics.Helpers.ExternalStorage;

public class GenericStackInv : MEStorage
{
	public enum Mode
	{
		CONFIG_TYPES,
		CONFIG_STACKS,
		STORAGE,
	}

	protected readonly GenericStack?[] stacks;
	private readonly Action? listener;
	private bool suppressOnChange;
	private bool onChangeSuppressed;
	private readonly Dictionary<AEKeyType, long> capacities = new();
	private AEKeyFilter? filter;
	protected readonly Mode mode;
	private string description = "";

	private readonly bool allowOverstacking;

	public GenericStackInv(Action? listener, int size) : this(listener, Mode.STORAGE, size) { }

	public GenericStackInv(Action? listener, Mode mode, int size, bool allowOverstacking = false)
	{
		this.stacks = new GenericStack?[size];
		this.listener = listener;
		this.mode = mode;
		this.allowOverstacking = allowOverstacking;
	}

	protected void SetFilter(AEKeyFilter? filter) => this.filter = filter;

	public AEKeyFilter? GetFilter() => filter;

	public bool IsAllowed(AEKey what) => filter == null || filter.Matches(what);

	public bool IsAllowed(GenericStack? stack) => stack == null || IsAllowed(stack.What);

	public int Size() => stacks.Length;

	public bool IsEmpty()
	{
		foreach (var stack in stacks)
			if (stack != null)
				return false;
		return true;
	}

	public GenericStack? GetStack(int slot)
	{
		var stack = stacks[slot];
		if (stack != null && !IsAllowed(stack.What))
		{
			SetStack(slot, null);
			stack = null;
		}
		return stack;
	}

	public AEKey? GetKey(int slot)
	{
		var key = stacks[slot]?.What;
		if (key == null) return null;
		if (!IsAllowed(key))
		{
			SetStack(slot, null);
			key = null;
		}
		return key;
	}

	public long GetAmount(int slot) => stacks[slot]?.Amount ?? 0;

	public void SetStack(int slot, GenericStack? stack)
	{
		if (stack != null && !IsAllowed(stack.What))
			return;
		if (stack != null)
		{
			bool typesOnly = mode == Mode.CONFIG_TYPES;
			if (typesOnly && stack.Amount != 0)
				stack = new GenericStack(stack.What, 0);
			else if (!typesOnly && stack.Amount <= 0)
			{
				if (mode == Mode.CONFIG_STACKS && GetStack(slot) == null)
					stack = new GenericStack(stack.What, 1);
				else
					stack = null;
			}
		}

		if (stack != null && GetMaxAmount(stack.What) < stack.Amount)
			stack = new GenericStack(stack.What, GetMaxAmount(stack.What));
		if (!Equals(stacks[slot], stack))
		{
			stacks[slot] = stack;
			OnChange();
		}
	}

	public long Insert(int slot, AEKey what, long amount, Actionable mode)
	{
		if (what is null) throw new ArgumentNullException(nameof(what));
		if (amount < 0) throw new ArgumentException("amount >= 0", nameof(amount));

		if (!CanInsert() || !IsAllowed(what))
			return 0;

		var currentWhat = GetKey(slot);
		var currentAmount = GetAmount(slot);
		if (currentWhat == null || currentWhat.Equals(what))
		{
			var newAmount = Math.Min(currentAmount + amount, GetMaxAmount(what));
			if (newAmount > currentAmount)
			{
				if (mode == Actionable.MODULATE)
				{
					SetStack(slot, new GenericStack(what, newAmount));
					newAmount = GetAmount(slot);
				}
				return newAmount - currentAmount;
			}
		}
		return 0;
	}

	public long Extract(int slot, AEKey what, long amount, Actionable mode)
	{
		if (what is null) throw new ArgumentNullException(nameof(what));
		if (amount < 0) throw new ArgumentException("amount >= 0", nameof(amount));

		var currentWhat = GetKey(slot);
		if (!CanExtract() || currentWhat == null || !currentWhat.Equals(what))
			return 0;

		var currentAmount = GetAmount(slot);
		var canExtract = Math.Min(currentAmount, amount);

		if (canExtract > 0)
		{
			if (mode == Actionable.MODULATE)
			{
				var newAmount = currentAmount - canExtract;
				if (newAmount <= 0)
					SetStack(slot, null);
				else
					SetStack(slot, new GenericStack(what, newAmount));
				var reallyExtracted = Math.Max(0, currentAmount - GetAmount(slot));
				if (reallyExtracted != canExtract)
				{
					AELog.Warn(
						"GenericStackInv simulation/modulation extraction mismatch: canExtract=%d, reallyExtracted=%d",
						canExtract, reallyExtracted);
					canExtract = reallyExtracted;
				}
			}
		}
		return canExtract;
	}

	public long GetCapacity(AEKeyType space) => capacities.TryGetValue(space, out var c) ? c : long.MaxValue;

	public bool CanInsert() => true;

	public bool CanExtract() => true;

	public void SetCapacity(AEKeyType space, long capacity) => capacities[space] = capacity;

	public long GetMaxAmount(AEKey key)
	{
		if (allowOverstacking)
			return GetCapacity(key.KeyType);
		if (key is AEItemKey itemKey)
			return Math.Min(itemKey.GetMaxStackSize(), GetCapacity(key.KeyType));
		return GetCapacity(key.KeyType);
	}

	public void OnChange()
	{
		if (!suppressOnChange)
			NotifyListener();
		else
			onChangeSuppressed = true;
	}

	protected void NotifyListener() => listener?.Invoke();

	public List<TagCompound> WriteToTag()
	{
		var tag = new List<TagCompound>();
		foreach (var stack in stacks)
			tag.Add(GenericStack.WriteTag(stack));

		for (int i = tag.Count - 1; i >= 0; i--)
		{
			if (tag[i].Count == 0)
				tag.RemoveAt(i);
			else
				break;
		}
		return tag;
	}

	public void WriteToChildTag(TagCompound tag, string name)
	{
		bool isEmpty = true;
		foreach (var stack in stacks)
			if (stack != null) { isEmpty = false; break; }

		if (!isEmpty)
			tag[name] = WriteToTag();
		else
			tag.Remove(name);
	}

	public void ReadFromTag(IList<TagCompound> tag)
	{
		bool changed = false;
		for (int i = 0; i < Math.Min(Size(), tag.Count); i++)
		{
			var stack = GenericStack.ReadTag(tag[i]);
			if (!Equals(stack, stacks[i]))
			{
				stacks[i] = stack;
				changed = true;
			}
		}
		for (int i = tag.Count; i < Size(); i++)
		{
			if (stacks[i] != null)
			{
				stacks[i] = null;
				changed = true;
			}
		}

		if (changed)
			OnChange();
	}

	public void Clear()
	{
		bool changed = false;
		for (int i = 0; i < stacks.Length; i++)
		{
			changed |= stacks[i] != null;
			stacks[i] = null;
		}
		if (changed)
			OnChange();
	}

	public void ReadFromChildTag(TagCompound tag, string name)
	{
		if (tag.ContainsKey(name))
			ReadFromTag(tag.GetList<TagCompound>(name));
		else
			Clear();
	}

	public void BeginBatch()
	{
		if (suppressOnChange)
			throw new InvalidOperationException("beginBatch was called without endBatch");
		suppressOnChange = true;
	}

	public void EndBatch()
	{
		if (!suppressOnChange)
			throw new InvalidOperationException("endBatch was called without beginBatch");
		suppressOnChange = false;
		if (onChangeSuppressed)
		{
			onChangeSuppressed = false;
			OnChange();
		}
	}

	public void EndBatchSuppressed()
	{
		if (!suppressOnChange)
			throw new InvalidOperationException("endBatch was called without beginBatch");
		suppressOnChange = false;
		onChangeSuppressed = false;
	}

	public Mode GetMode() => mode;

	public long Insert(AEKey what, long amount, Actionable mode, IActionSource source)
	{
		if (what is null) throw new ArgumentNullException(nameof(what));
		if (amount < 0) throw new ArgumentException("amount >= 0", nameof(amount));
		if (!IsAllowed(what))
			return 0;

		if (this.mode == Mode.CONFIG_TYPES)
		{
			int freeSlot = -1;
			for (int i = 0; i < stacks.Length; i++)
			{
				var key = GetKey(i);
				if (key == what)
					return 0;
				if (key == null && freeSlot == -1)
					freeSlot = i;
			}
			if (freeSlot != -1 && mode == Actionable.MODULATE)
				SetStack(freeSlot, new GenericStack(what, 0));
			return 0;
		}

		var inserted = 0L;
		for (int i = 0; i < stacks.Length && inserted < amount; i++)
			inserted += Insert(i, what, amount - inserted, mode);
		return inserted;
	}

	public long Extract(AEKey what, long amount, Actionable mode, IActionSource source)
	{
		if (what is null) throw new ArgumentNullException(nameof(what));
		if (amount < 0) throw new ArgumentException("amount >= 0", nameof(amount));

		var extracted = 0L;
		for (int i = 0; i < stacks.Length && extracted < amount; i++)
			extracted += Extract(i, what, amount - extracted, mode);
		return extracted;
	}

	public void GetAvailableStacks(KeyCounter @out)
	{
		foreach (var stack in stacks)
			if (stack != null)
				@out.Add(stack.What, stack.Amount);
	}

	public string GetDescription() => description;

	public void SetDescription(string description) => this.description = description;
}
