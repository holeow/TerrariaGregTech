#nullable enable
using System.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MeInterfaceAction : IMachineAction
{
	public enum Op : byte
	{
		SetConfig          = 0,
		SetPriority        = 1,
		ToggleCraftMissing = 2,
		Pickup             = 3,
		SplitOrPlaceSingle = 4,
	}

	public PacketType Type => PacketType.MeInterfaceAction;

	private Op _op;
	private int _slot;
	private AEKey? _key;
	private long _amount;
	private int _value;
	private Item _cursor = new();

	public MeInterfaceAction() { }

	public static MeInterfaceAction SetConfig(int slot, AEKey? key, long amount) =>
		new() { _op = Op.SetConfig, _slot = slot, _key = key, _amount = amount };
	public static MeInterfaceAction SetPriority(int value) =>
		new() { _op = Op.SetPriority, _value = value };
	public static MeInterfaceAction ToggleCraft() =>
		new() { _op = Op.ToggleCraftMissing };
	public static MeInterfaceAction Pickup(int slot, Item cursor) =>
		new() { _op = Op.Pickup, _slot = slot, _cursor = cursor.Clone() };
	public static MeInterfaceAction SplitOrPlaceSingle(int slot, Item cursor) =>
		new() { _op = Op.SplitOrPlaceSingle, _slot = slot, _cursor = cursor.Clone() };

	public void Write(BinaryWriter w)
	{
		w.Write((byte)_op);
		switch (_op)
		{
			case Op.SetConfig:
				w.Write((byte)_slot);
				AEKey.WriteOptionalKey(w, _key);
				w.Write(_amount);
				break;
			case Op.SetPriority:
				w.Write(_value);
				break;
			case Op.Pickup:
			case Op.SplitOrPlaceSingle:
				w.Write((byte)_slot);
				w.WriteItem(_cursor);
				break;
		}
	}

	public void Read(BinaryReader r)
	{
		_op = (Op)r.ReadByte();
		switch (_op)
		{
			case Op.SetConfig:
				_slot = r.ReadByte();
				_key = AEKey.ReadOptionalKey(r);
				_amount = r.ReadInt64();
				break;
			case Op.SetPriority:
				_value = r.ReadInt32();
				break;
			case Op.Pickup:
			case Op.SplitOrPlaceSingle:
				_slot = r.ReadByte();
				_cursor = r.ReadItem();
				break;
		}
	}

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is not MeInterfaceMachine iface) return;
		switch (_op)
		{
			case Op.SetConfig:
				iface.ApplySetConfig(_slot, _key, _amount);
				break;
			case Op.SetPriority:
				iface.ApplySetPriority(_value);
				break;
			case Op.ToggleCraftMissing:
				iface.ApplyToggleCraftMissing();
				break;
			case Op.Pickup:
				if (byWhoAmI >= 0 && byWhoAmI < Main.maxPlayers)
					iface.ApplyPickup(_slot, _cursor, byWhoAmI);
				break;
			case Op.SplitOrPlaceSingle:
				if (byWhoAmI >= 0 && byWhoAmI < Main.maxPlayers)
					iface.ApplySplitOrPlaceSingle(_slot, _cursor, byWhoAmI);
				break;
		}
	}
}
