#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

// Authoritative cursor (Main.mouseItem) after a server-side SlotAction
public static class CursorUpdatePacket
{
	public enum Delivery : byte
	{
		Cursor          = 0,
		PlayerInventory = 1,
		CursorMerge     = 2,
	}

	public static void SendTo(int toClient, Item item, Delivery delivery)
	{
		if (Main.netMode != NetmodeID.Server) return;
		var p = NetRouter.NewPacket(PacketType.CursorUpdate);
		p.Write((byte)delivery);
		p.WriteItem(item);
		p.Send(toClient: toClient);
	}

	public static void HandleOnClient(BinaryReader r)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		var delivery = (Delivery)r.ReadByte();
		var item = r.ReadItem();
		switch (delivery)
		{
			case Delivery.Cursor:
				Main.mouseItem = item;
				break;
			case Delivery.CursorMerge:
				global::GregTechCEuTerraria.TerrariaCompat.Net.Actions.SlotAction.MergeOntoCursor(item);
				break;
			case Delivery.PlayerInventory:
				if (item.IsAir) return;
				global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(
					Main.LocalPlayer, Main.LocalPlayer.GetSource_Misc("gtceu_cursor_overflow"), item);
				break;
		}
	}
}
