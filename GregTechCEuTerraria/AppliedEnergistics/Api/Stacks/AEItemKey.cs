// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.stacks.AEItemKey), Forge 1.20.1. Original is unheadered; AE2 is
// LGPL-3.0-only (older API files MIT). See AE2's LICENSE.

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using GregTechCEuTerraria.Api.Cover.Filter;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using GregTechCEuTerraria.AppliedEnergistics.Core;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

public sealed class AEItemKey : AEKey
{
	private readonly int _item;
	private readonly TagCompound? _tag;
	private readonly int _hashCode;

	private Item? _readOnlyStack;
	private int _maxStackSize = -1;

	private AEItemKey(int item, TagCompound? tag)
	{
		_item = item;
		_tag = tag;
		_hashCode = item;
	}

	public static AEItemKey? Of(Item stack)
	{
		if (stack is null || stack.IsAir)
			return null;
		var ret = new AEItemKey(stack.type, ExtractIdentityTag(stack));
		ret._maxStackSize = stack.maxStack;
		return ret;
	}

	public static AEItemKey OfType(int itemType) => new(itemType, null);

	private static TagCompound? ExtractIdentityTag(Item stack)
	{
		var full = ItemIO.Save(stack);
		var t = new TagCompound();
		if (full.ContainsKey("data"))
			t["data"] = full.GetCompound("data");
		if (full.ContainsKey("globalData"))
		{
			var gd = full.GetList<TagCompound>("globalData");
			if (gd.Count > 0)
				t["globalData"] = gd;
		}
		if (full.ContainsKey("prefix"))
			t["prefix"] = full.GetByte("prefix");
		if (full.ContainsKey("modPrefixMod"))
		{
			t["modPrefixMod"] = full.GetString("modPrefixMod");
			t["modPrefixName"] = full.GetString("modPrefixName");
		}
		return t.Count > 0 ? t : null;
	}

	public static bool Matches(AEKey what, Item itemStack) =>
		what is AEItemKey itemKey && itemKey.Matches(itemStack);

	public static bool Is(AEKey what) => what is AEItemKey;

	public static AEKeyFilter Filter() => ItemFilter.INSTANCE;

	public override AEKeyType KeyType => AEKeyType.Items();

	public override AEItemKey DropSecondary() => OfType(_item);

	public override bool Equals(object? o)
	{
		if (ReferenceEquals(this, o))
			return true;
		if (o is not AEItemKey k)
			return false;
		return _item == k._item && ItemLoader.CanStack(GetReadOnlyStack(), k.GetReadOnlyStack());
	}

	public override int GetHashCode() => _hashCode;

	public bool Matches(Item stack) =>
		stack is not null && !stack.IsAir && stack.type == _item
		&& ItemLoader.CanStack(GetReadOnlyStack(), stack);

	public bool Matches(Ingredient ingredient) => ingredient.Test(GetReadOnlyStack());

	public Item GetReadOnlyStack()
	{
		if (_readOnlyStack == null)
		{
			_readOnlyStack = ToStack(1);
		}
		else if (_readOnlyStack.IsAir)
		{
			AELog.Error("Something destroyed the read-only itemstack of %s", this);
			_readOnlyStack = null;
			return GetReadOnlyStack();
		}
		return _readOnlyStack;
	}

	public Item ToStack() => ToStack(1);

	public override Item WrapForDisplayOrFilter() => ToStack();

	public Item ToStack(int count)
	{
		if (count <= 0)
			return new Item();

		var sample = new Item();
		sample.SetDefaults(_item);

		var full = sample.ModItem is Terraria.ModLoader.Default.UnloadedItem
			? new TagCompound { ["mod"] = sample.ModItem.Mod.Name, ["name"] = sample.ModItem.Name }
			: ItemIO.Save(sample);
		if (_tag != null)
			foreach (var kv in _tag)
				full[kv.Key] = kv.Value;

		var result = new Item();
		ItemIO.Load(result, full);
		result.stack = count;
		return result;
	}

	public int GetItem() => _item;

	public static AEItemKey? FromTag(TagCompound tag)
	{
		try
		{
			var item = new Item();
			ItemIO.Load(item, tag);
			return Of(item);
		}
		catch (Exception e)
		{
			AELog.Error(e, "Dropping a corrupt item key from NBT: " + tag);
			return null;
		}
	}

	public override TagCompound ToTag() => ItemIO.Save(ToStack(1));

	public override object GetPrimaryKey() => _item;

	public override string Id => IIngredientResolver.Default?.StableItemId(_item) ?? "";

	public override string GetModId() =>
		GetReadOnlyStack().ModItem?.Mod.Name ?? "terraria";

	public TagCompound? GetTag() => _tag;

	public TagCompound? CopyTag() => _tag != null ? (TagCompound)_tag.Clone() : null;

	public bool HasTag() => _tag != null;

	public override void AddDrops(long amount, List<Item> drops)
	{
		while (amount > 0)
		{
			if (drops.Count > 1000)
			{
				AELog.Warn("Tried dropping an excessive amount of items, ignoring %s %ss", amount, _item);
				break;
			}

			var taken = Math.Min(amount, GetMaxStackSize());
			amount -= taken;
			drops.Add(ToStack((int)taken));
		}
	}

	protected override string ComputeDisplayName() => GetReadOnlyStack().Name;

	public override bool IsTagged(string tag) => TagSource.TagsOf(GetReadOnlyStack()).Contains(tag);

	public int GetMaxStackSize()
	{
		var ret = _maxStackSize;
		if (ret == -1)
			_maxStackSize = ret = GetReadOnlyStack().maxStack;
		return ret;
	}

	public override void WriteToPacket(BinaryWriter data)
	{
		data.Write7BitEncodedInt(_item);
		bool has = _tag != null;
		data.Write(has);
		if (has)
			TagIO.Write(_tag!, data);
	}

	public static AEItemKey FromPacket(BinaryReader data)
	{
		int item = data.Read7BitEncodedInt();
		bool has = data.ReadBoolean();
		TagCompound? tag = has ? TagIO.Read(data) : null;
		return new AEItemKey(item, tag);
	}

	public override string ToString()
	{
		string idString;
		try { idString = Id; } catch { idString = _item + "(unresolved)"; }
		return _tag == null ? idString : idString + " (+tag)";
	}

	private sealed class ItemFilter : AEKeyFilter
	{
		internal static readonly ItemFilter INSTANCE = new();
		public bool Matches(AEKey what) => Is(what);
	}
}
