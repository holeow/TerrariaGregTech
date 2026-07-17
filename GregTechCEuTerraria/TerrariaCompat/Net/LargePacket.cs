#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

// Keeps network under Terraria's hard 65535-byte per-packet limit
public static class LargePacket
{
	private const int ChunkBytes = 60000;

	private static int _nextStreamId;

	public static void Send(PacketType type, Action<BinaryWriter> writePayload,
		int toClient = -1, int ignoreClient = -1)
	{
		byte[] data;
		using (var ms = new MemoryStream())
		using (var bw = new BinaryWriter(ms))
		{
			writePayload(bw);
			bw.Flush();
			data = ms.ToArray();
		}

		if (data.Length <= ChunkBytes)
		{
			var p = NetRouter.NewPacket(type);
			p.Write(data);
			p.Send(toClient, ignoreClient);
			return;
		}

		Terraria.ModLoader.ModContent.GetInstance<global::GregTechCEuTerraria.GregTechCEuTerraria>()
			.Logger.Warn($"[Net] {type} payload {data.Length} bytes exceeds the {ChunkBytes}-byte packet " +
			             $"budget - fragmenting. Consider optimizing this packet");

		int streamId = _nextStreamId++;
		int total = (data.Length + ChunkBytes - 1) / ChunkBytes;
		for (int i = 0; i < total; i++)
		{
			int off = i * ChunkBytes;
			int len = Math.Min(ChunkBytes, data.Length - off);
			var p = NetRouter.NewPacket(PacketType.Fragment);
			p.Write((byte)type);
			p.Write(streamId);
			p.Write((ushort)total);
			p.Write((ushort)i);
			p.Write((ushort)len);
			p.Write(data, off, len);
			p.Send(toClient, ignoreClient);
		}
	}

	private sealed class Assembly
	{
		public byte OrigType;
		public int Total;
		public int Received;
		public byte[]?[] Chunks = Array.Empty<byte[]?>();
	}

	private static readonly Dictionary<(int who, int stream), Assembly> _pending = new();

	public static void HandleFragment(BinaryReader r, int whoAmI)
	{
		byte origType = r.ReadByte();
		int streamId  = r.ReadInt32();
		int total     = r.ReadUInt16();
		int index     = r.ReadUInt16();
		int len       = r.ReadUInt16();
		byte[] chunk  = r.ReadBytes(len);

		if (total == 0 || index >= total) return;

		var key = (whoAmI, streamId);
		if (!_pending.TryGetValue(key, out var a))
		{
			a = new Assembly { OrigType = origType, Total = total, Chunks = new byte[total][] };
			_pending[key] = a;
		}
		if (a.Chunks[index] is null)
		{
			a.Chunks[index] = chunk;
			a.Received++;
		}
		if (a.Received < a.Total) return;
		_pending.Remove(key);

		int size = 0;
		for (int i = 0; i < a.Total; i++) size += a.Chunks[i]!.Length;
		var full = new byte[1 + size];
		full[0] = a.OrigType;
		int pos = 1;
		for (int i = 0; i < a.Total; i++)
		{
			var c = a.Chunks[i]!;
			Buffer.BlockCopy(c, 0, full, pos, c.Length);
			pos += c.Length;
		}

		using var ms = new MemoryStream(full, writable: false);
		using var br = new BinaryReader(ms);
		NetRouter.Handle(br, whoAmI);
	}
}
