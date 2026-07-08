#nullable enable
using System.IO;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class FluidSlotAction : IMachineAction
{
	public PacketType Type => PacketType.FluidSlotAction;

	private byte _tankIndex;
	private Item _cursor = new();

	public FluidSlotAction() { }
	public FluidSlotAction(int tankIndex, Item cursor)
	{
		_tankIndex = (byte)tankIndex;
		_cursor = cursor.Clone();
	}

	public void Write(BinaryWriter w)
	{
		w.Write(_tankIndex);
		w.WriteItem(_cursor);
	}

	public void Read(BinaryReader r)
	{
		_tankIndex = r.ReadByte();
		_cursor = r.ReadItem();
	}

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is not IFluidHandler handler) return;
		if (_cursor.IsAir) return;
		if (_tankIndex >= handler.TankCount) return;

		var tank = handler.GetTankAccess(_tankIndex);
		var caps = handler.GetTankClickCaps(_tankIndex);

		if (FluidContainerTransfer.TryTransfer(tank, ref _cursor, caps, out _extraDelivery))
			DeliverCursor(byWhoAmI);
	}

	private Item? _extraDelivery;

	private void DeliverCursor(int byWhoAmI)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			CursorUpdatePacket.SendTo(byWhoAmI, _cursor, CursorUpdatePacket.Delivery.Cursor);
			if (_extraDelivery is { IsAir: false } extra)
				CursorUpdatePacket.SendTo(byWhoAmI, extra, CursorUpdatePacket.Delivery.PlayerInventory);
			return;
		}
		Main.mouseItem = _cursor;
		if (_extraDelivery is { IsAir: false } e)
			global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(
				Main.LocalPlayer, Main.LocalPlayer.GetSource_Misc("gtceu_bucket_overflow"), e);
	}
}
