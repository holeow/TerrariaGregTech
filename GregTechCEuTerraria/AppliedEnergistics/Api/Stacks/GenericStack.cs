// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.stacks.GenericStack), Forge 1.20.1. Original is unheadered; AE2 is
// LGPL-3.0-only (older API files MIT). See AE2's LICENSE.

#nullable enable
using System;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;
using GregTechCEuTerraria.Api.Fluids;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

public sealed record GenericStack(AEKey What, long Amount)
{
	public AEKey What { get; } = What ?? throw new ArgumentNullException(nameof(What));

	public static GenericStack? ReadBuffer(BinaryReader buffer)
	{
		if (!buffer.ReadBoolean())
			return null;

		var what = AEKey.ReadKey(buffer);
		if (what == null)
			return null;

		return new GenericStack(what, buffer.Read7BitEncodedInt64());
	}

	public static void WriteBuffer(GenericStack? stack, BinaryWriter buffer)
	{
		if (stack == null)
		{
			buffer.Write(false);
		}
		else
		{
			buffer.Write(true);
			AEKey.WriteKey(buffer, stack.What);
			buffer.Write7BitEncodedInt64(stack.Amount);
		}
	}

	public static GenericStack? ReadTag(TagCompound tag)
	{
		if (tag.Count == 0)
			return null;
		var key = AEKey.FromTagGeneric(tag);
		if (key == null)
			return null;
		return new GenericStack(key, tag.GetLong("#"));
	}

	public static TagCompound WriteTag(GenericStack? stack)
	{
		if (stack == null)
			return new TagCompound();
		var tag = stack.What.ToTagGeneric();
		tag["#"] = stack.Amount;
		return tag;
	}

	public static GenericStack? FromItemStack(Item stack)
	{
		var unwrapped = UnwrapItemStack(stack);
		if (unwrapped != null)
			return unwrapped;
		var key = AEItemKey.Of(stack);
		if (key == null)
			return null;
		return new GenericStack(key, stack.stack);
	}

	public static GenericStack? FromFluidStack(FluidStack stack)
	{
		var key = AEFluidKey.Of(stack);
		if (key == null)
			return null;
		return new GenericStack(key, stack.Amount);
	}

	public static long GetStackSizeOrZero(GenericStack? stack) => stack == null ? 0 : stack.Amount;

	public static GenericStack Sum(GenericStack left, GenericStack right)
	{
		if (!left.What.Equals(right.What))
			throw new ArgumentException("Cannot sum generic stacks of " + left.What + " and " + right.What);
		return new GenericStack(left.What, left.Amount + right.Amount);
	}

	public static Func<AEKey, long, Item>? WrapFactory;

	public static Item WrapInItemStack(AEKey what, long amount) =>
		WrapFactory?.Invoke(what, amount) ?? new Item();

	public static Item WrapInItemStack(GenericStack? stack) =>
		stack != null ? WrapInItemStack(stack.What, stack.Amount) : new Item();

	public static bool IsWrapped(Item stack) =>
		!stack.IsAir && stack.ModItem is IWrappedGenericStack;

	public static GenericStack? UnwrapItemStack(Item stack)
	{
		if (!stack.IsAir && stack.ModItem is IWrappedGenericStack w && w.What != null)
			return new GenericStack(w.What, w.Amount);
		return null;
	}
}
