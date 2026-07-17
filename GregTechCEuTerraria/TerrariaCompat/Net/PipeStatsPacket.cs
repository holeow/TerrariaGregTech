#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.ItemPipe;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class PipeStatsPacket
{
	public static void Broadcast()
	{
		if (Main.netMode != NetmodeID.Server) return;

		int n = 0;
		foreach (var kv in ItemPipeLayerSystem.AllSides)
			if (kv.Value.TransferredItems > 0) n++;

		LargePacket.Send(PacketType.PipeStats, p =>
		{
			p.Write(n);
			foreach (var kv in ItemPipeLayerSystem.AllSides)
			{
				int v = kv.Value.TransferredItems;
				if (v <= 0) continue;
				p.Write((short)kv.Key.x);
				p.Write((short)kv.Key.y);
				p.Write(v);
			}
		});
	}

	public static void HandleOnClient(BinaryReader r)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		int n = r.ReadInt32();
		var cache = ItemPipeNetSystem.ClientTransferStats;
		cache.Clear();
		for (int i = 0; i < n; i++)
		{
			int x = r.ReadInt16();
			int y = r.ReadInt16();
			int v = r.ReadInt32();
			cache[(x, y)] = v;
		}
	}
}
