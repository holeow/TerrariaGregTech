#nullable enable
using System.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Util;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class MeCablePackets
{
	private static void WriteCell(BinaryWriter w, MeCableCell cell) => w.Write((byte)cell.Color);

	private static MeCableCell ReadCell(BinaryReader r)
	{
		byte c = r.ReadByte();
		if (c > (byte)AEColor.TRANSPARENT) c = (byte)AEColor.TRANSPARENT;
		return new MeCableCell((AEColor)c);
	}

	public static void SendSet(int x, int y, MeCableCell cell)
	{
		if (Main.netMode == NetmodeID.SinglePlayer) return;
		var p = NetRouter.NewPacket(PacketType.MeCableSet);
		p.Write((short)x);
		p.Write((short)y);
		WriteCell(p, cell);
		p.Send();
	}

	public static void HandleSet(BinaryReader r, int whoAmI)
	{
		int x = r.ReadInt16();
		int y = r.ReadInt16();
		var cell = ReadCell(r);

		if (Main.netMode == NetmodeID.Server)
		{
			MeCableLayerSystem.Cables.Set(x, y, cell);
			var p = NetRouter.NewPacket(PacketType.MeCableSet);
			p.Write((short)x);
			p.Write((short)y);
			WriteCell(p, cell);
			p.Send(ignoreClient: whoAmI);
			return;
		}
		if (Main.netMode == NetmodeID.MultiplayerClient)
			MeCableLayerSystem.Cables.Set(x, y, cell);
	}

	public static void SendRemove(int x, int y)
	{
		if (Main.netMode == NetmodeID.SinglePlayer) return;
		var p = NetRouter.NewPacket(PacketType.MeCableRemove);
		p.Write((short)x);
		p.Write((short)y);
		p.Send();
	}

	public static void HandleRemove(BinaryReader r, int whoAmI)
	{
		int x = r.ReadInt16();
		int y = r.ReadInt16();

		if (Main.netMode == NetmodeID.Server)
		{
			MeCableLayerSystem.Cables.Remove(x, y);
			var p = NetRouter.NewPacket(PacketType.MeCableRemove);
			p.Write((short)x);
			p.Write((short)y);
			p.Send(ignoreClient: whoAmI);
			return;
		}
		if (Main.netMode == NetmodeID.MultiplayerClient)
			MeCableLayerSystem.Cables.Remove(x, y);
	}

	public static void SendLayerRequest()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		NetRouter.NewPacket(PacketType.MeCableLayerRequest).Send();
	}

	public static void HandleLayerRequest(BinaryReader r, int whoAmI)
	{
		if (Main.netMode != NetmodeID.Server) return;
		LargePacket.Send(PacketType.MeCableLayerFull, p =>
		{
			p.Write(MeCableLayerSystem.Cables.Count);
			foreach (var kv in MeCableLayerSystem.Cables.All)
			{
				p.Write((short)kv.Key.x);
				p.Write((short)kv.Key.y);
				WriteCell(p, kv.Value);
			}
		}, toClient: whoAmI);
	}

	public static void HandleLayerFull(BinaryReader r)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		MeCableLayerSystem.Cables.Clear();
		int n = r.ReadInt32();
		for (int i = 0; i < n; i++)
		{
			int x = r.ReadInt16();
			int y = r.ReadInt16();
			var cell = ReadCell(r);
			MeCableLayerSystem.Cables.Set(x, y, cell);
		}
	}
}
