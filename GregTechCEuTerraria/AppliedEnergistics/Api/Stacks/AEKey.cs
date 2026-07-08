// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.stacks.AEKey), Forge 1.20.1. Original is unheadered; AE2 is
// LGPL-3.0-only (older API files MIT). See AE2's LICENSE.

#nullable enable
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;
using GregTechCEuTerraria.AppliedEnergistics.Core;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

// Uniquely identifies something that "stacks" within an ME inventory.
public abstract class AEKey
{
	private volatile string? _cachedDisplayName;

	public static AEKey? FromTagGeneric(TagCompound tag)
	{
		var channelId = tag.ContainsKey("#c") ? tag.GetString("#c") : "";
		if (string.IsNullOrEmpty(channelId))
		{
			AELog.Warn("Cannot deserialize generic key from %s because key '#c' is missing.", tag);
			return null;
		}

		AEKeyType channel;
		try
		{
			channel = AEKeyTypes.Get(channelId);
		}
		catch (System.ArgumentException)
		{
			AELog.Warn("Cannot deserialize generic key from %s because channel '%s' is missing.", tag, channelId);
			return null;
		}

		return channel.LoadKeyFromTag(tag);
	}

	public static void WriteOptionalKey(BinaryWriter buffer, AEKey? key)
	{
		buffer.Write(key != null);
		if (key != null)
			WriteKey(buffer, key);
	}

	public static void WriteKey(BinaryWriter buffer, AEKey key)
	{
		var id = key.KeyType.GetRawId();
		buffer.Write7BitEncodedInt(id);
		key.WriteToPacket(buffer);
	}

	public static AEKey? ReadOptionalKey(BinaryReader buffer)
	{
		if (!buffer.ReadBoolean())
			return null;
		return ReadKey(buffer);
	}

	public static AEKey? ReadKey(BinaryReader buffer)
	{
		var id = buffer.Read7BitEncodedInt();
		var type = AEKeyType.FromRawId(id);
		if (type == null)
		{
			AELog.Error("Received unknown key space id %d", id);
			return null;
		}
		return type.ReadFromPacket(buffer);
	}

	public TagCompound ToTagGeneric()
	{
		var tag = ToTag();
		tag["#c"] = KeyType.Id;
		return tag;
	}

	public int GetAmountPerUnit() => KeyType.GetAmountPerUnit();

	public string? GetUnitSymbol() => KeyType.GetUnitSymbol();

	public int GetAmountPerOperation() => KeyType.GetAmountPerOperation();

	public int GetAmountPerByte() => KeyType.GetAmountPerByte();

	public string FormatAmount(long amount, AmountFormat format) => KeyType.FormatAmount(amount, format);

	public abstract AEKeyType KeyType { get; }

	public abstract AEKey DropSecondary();

	public abstract TagCompound ToTag();

	public abstract object GetPrimaryKey();

	public abstract string Id { get; }

	public abstract void WriteToPacket(BinaryWriter data);

	public bool Matches(GenericStack? stack) => stack != null && stack.What.Equals(this);

	public virtual Item WrapForDisplayOrFilter() => GenericStack.WrapInItemStack(this, 0);

	public virtual string GetModId()
	{
		var id = Id;
		int i = id.IndexOf(':');
		return i >= 0 ? id.Substring(0, i) : id;
	}

	public string GetDisplayName()
	{
		var ret = _cachedDisplayName;
		if (ret == null)
			_cachedDisplayName = ret = ComputeDisplayName();
		return ret;
	}

	protected abstract string ComputeDisplayName();

	public abstract void AddDrops(long amount, List<Item> drops);

	public virtual bool IsTagged(string tag) => false;
}
