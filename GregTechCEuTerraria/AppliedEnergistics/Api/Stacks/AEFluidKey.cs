// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.stacks.AEFluidKey), Forge 1.20.1. Original is unheadered; AE2 is
// LGPL-3.0-only (older API files MIT). See AE2's LICENSE.

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;
using GregTechCEuTerraria.Api.Cover.Filter;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using GregTechCEuTerraria.AppliedEnergistics.Core;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

// TODO: can optimize traffic by converting fluid id string to int id
public sealed class AEFluidKey : AEKey
{
	public const int AMOUNT_BUCKET = 1000;
	public const int AMOUNT_BLOCK = 1000;

	private readonly FluidType _fluid;
	private readonly TagCompound? _tag;
	private readonly int _hashCode;

	private AEFluidKey(FluidType fluid, TagCompound? tag)
	{
		_fluid = fluid;
		_tag = tag;
		_hashCode = HashCode.Combine(fluid.Id, CanonicalTag.Hash(tag));
	}

	public static AEFluidKey Of(FluidType fluid, TagCompound? tag) =>
		new(fluid, tag != null ? (TagCompound)tag.Clone() : null);

	public static AEFluidKey Of(FluidType fluid) => Of(fluid, null);

	public static AEFluidKey? Of(FluidStack fluidVariant)
	{
		if (fluidVariant.IsEmpty)
			return null;
		return Of(fluidVariant.Type!, fluidVariant.Nbt);
	}

	public static bool Matches(AEKey what, FluidStack fluid) =>
		what is AEFluidKey fluidKey && fluidKey.Matches(fluid);

	public static bool Is(AEKey what) => what is AEFluidKey;

	public static bool Is(GenericStack? stack) => stack != null && stack.What is AEFluidKey;

	public static AEKeyFilter Filter() => FluidFilter.INSTANCE;

	public bool Matches(FluidStack variant) =>
		!variant.IsEmpty && _fluid.Id == variant.Type!.Id && CanonicalTag.Equal(_tag, variant.Nbt);

	public override AEKeyType KeyType => AEKeyType.Fluids();

	public override AEFluidKey DropSecondary() => Of(_fluid, null);

	public override bool Equals(object? o)
	{
		if (ReferenceEquals(this, o))
			return true;
		if (o is not AEFluidKey k)
			return false;
		return _hashCode == k._hashCode && _fluid.Id == k._fluid.Id && CanonicalTag.Equal(_tag, k._tag);
	}

	public override int GetHashCode() => _hashCode;

	public static AEFluidKey? FromTag(TagCompound tag)
	{
		try
		{
			if (!FluidRegistry.TryGet(tag.GetString("id"), out var fluid))
				throw new ArgumentException("Unknown fluid id.");
			var extraTag = tag.ContainsKey("tag") ? tag.GetCompound("tag") : null;
			return Of(fluid, extraTag);
		}
		catch (Exception e)
		{
			AELog.Debug("Tried to load an invalid fluid key from NBT: %s", tag, e);
			return null;
		}
	}

	public override TagCompound ToTag()
	{
		var result = new TagCompound { ["id"] = _fluid.Id };
		if (_tag != null)
			result["tag"] = (TagCompound)_tag.Clone();
		return result;
	}

	public override object GetPrimaryKey() => _fluid;

	public override string Id => _fluid.Id;

	public override void AddDrops(long amount, List<Item> drops)
	{
		// Fluids are voided.
	}

	protected override string ComputeDisplayName() => _fluid.DisplayName;

	public override bool IsTagged(string tag) => TagSource.TagsOf(ToStack(1)).Contains(tag);

	public FluidStack ToStack(int amount) => new(_fluid, amount, _tag);

	public FluidType GetFluid() => _fluid;

	public TagCompound? GetTag() => _tag;

	public TagCompound? CopyTag() => _tag != null ? (TagCompound)_tag.Clone() : null;

	public bool HasTag() => _tag != null;

	public override void WriteToPacket(BinaryWriter data)
	{
		data.Write(_fluid.Id);
		bool has = _tag != null;
		data.Write(has);
		if (has)
			TagIO.Write(_tag!, data);
	}

	public static AEFluidKey? FromPacket(BinaryReader data)
	{
		var id = data.ReadString();
		bool has = data.ReadBoolean();
		TagCompound? tag = has ? TagIO.Read(data) : null;
		return FluidRegistry.TryGet(id, out var fluid) ? new AEFluidKey(fluid, tag) : null;
	}

	public override string ToString() => _tag == null ? _fluid.Id : _fluid.Id + " (+tag)";

	private sealed class FluidFilter : AEKeyFilter
	{
		internal static readonly FluidFilter INSTANCE = new();
		public bool Matches(AEKey what) => Is(what);
	}
}
