#nullable enable
using System.Collections.Generic;
using System.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Menu.Me.Common;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class MeTerminalContentPacket
{
	public static void SendTo(int toClient, Point16 pos, bool fullUpdate, List<GridInventoryEntry> entries)
	{
		if (Main.netMode != NetmodeID.Server) return;
		var p = NetRouter.NewPacket(PacketType.MeTerminalContent);
		p.Write(pos.X);
		p.Write(pos.Y);
		p.Write(fullUpdate);
		p.Write7BitEncodedInt(entries.Count);
		foreach (var e in entries)
			WriteEntry(p, e);
		p.Send(toClient: toClient);
	}

	private static void WriteEntry(BinaryWriter w, GridInventoryEntry e)
	{
		w.Write7BitEncodedInt64(e.Serial);
		AEKey.WriteOptionalKey(w, e.What);
		w.Write7BitEncodedInt64(e.StoredAmount);
		w.Write7BitEncodedInt64(e.RequestableAmount);
		w.Write(e.Craftable);
	}

	private static GridInventoryEntry ReadEntry(BinaryReader r)
	{
		long serial = r.Read7BitEncodedInt64();
		var what = AEKey.ReadOptionalKey(r);
		long stored = r.Read7BitEncodedInt64();
		long requestable = r.Read7BitEncodedInt64();
		bool craftable = r.ReadBoolean();
		return new GridInventoryEntry(serial, what, stored, requestable, craftable);
	}

	public static void HandleOnClient(BinaryReader r)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		var pos = new Point16(r.ReadInt16(), r.ReadInt16());
		bool fullUpdate = r.ReadBoolean();
		int count = r.Read7BitEncodedInt();
		var entries = new List<GridInventoryEntry>(count);
		for (int i = 0; i < count; i++)
			entries.Add(ReadEntry(r));

		MeTerminalClient.RepoFor(pos)?.HandleUpdate(fullUpdate, entries);
	}
}
