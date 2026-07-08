#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Net;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MeStorageAction : IMachineAction
{
	public enum Op : byte
	{
		Insert = 0,
		Dump = 1,
		ShiftInsert = 2,
	}

	public PacketType Type => PacketType.MeStorageAction;

	private Op _op;
	private Item _cursor = new();

	public MeStorageAction() { }
	public MeStorageAction(Op op) { _op = op; }
	public MeStorageAction(Op op, Item cursor) { _op = op; _cursor = cursor.Clone(); }

	private bool CarriesItem => _op == Op.Insert || _op == Op.ShiftInsert;

	public void Write(BinaryWriter w)
	{
		w.Write((byte)_op);
		if (CarriesItem) w.WriteItem(_cursor);
	}

	public void Read(BinaryReader r)
	{
		_op = (Op)r.ReadByte();
		if (CarriesItem) _cursor = r.ReadItem();
	}

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is not MeStorageMachine store) return;
		switch (_op)
		{
			case Op.Insert:
				var leftover = store.InsertCursor(_cursor);
				if (Main.netMode == NetmodeID.Server)
					CursorUpdatePacket.SendTo(byWhoAmI, leftover, CursorUpdatePacket.Delivery.Cursor);
				else
					Main.mouseItem = leftover;
				break;
			case Op.ShiftInsert:
				var shiftLeft = store.InsertCursor(_cursor);
				if (shiftLeft.IsAir) break;
				if (Main.netMode == NetmodeID.Server)
					CursorUpdatePacket.SendTo(byWhoAmI, shiftLeft, CursorUpdatePacket.Delivery.PlayerInventory);
				else if (byWhoAmI >= 0 && byWhoAmI < Main.maxPlayers)
					global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(
						Main.player[byWhoAmI], Main.player[byWhoAmI].GetSource_OpenItem(shiftLeft.type), shiftLeft);
				break;
			case Op.Dump:
				if (byWhoAmI >= 0 && byWhoAmI < Main.maxPlayers)
					store.DumpFirstTo(Main.player[byWhoAmI]);
				break;
		}
	}
}
