#nullable enable
using System.Collections.Generic;
using System.IO;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Fluid;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class FluidPipeStatsPacket
{
	private const float SyncRangePx = 2500f;

	private static readonly List<string> _palette = new();
	private static readonly Dictionary<string, int> _paletteIdx = new();

	public static void Broadcast()
	{
		if (Main.netMode != NetmodeID.Server) return;

		const float rangeSq = SyncRangePx * SyncRangePx;

		for (int pid = 0; pid < Main.maxPlayers; pid++)
		{
			var plr = Main.player[pid];
			if (plr is null || !plr.active) continue;
			float cx = plr.Center.X, cy = plr.Center.Y;

			_palette.Clear();
			_paletteIdx.Clear();
			int n = 0;
			foreach (var kv in FluidPipeLayerSystem.AllStates)
			{
				float dx = (kv.Key.x * 16f + 8f) - cx, dy = (kv.Key.y * 16f + 8f) - cy;
				if (dx * dx + dy * dy > rangeSq) continue;
				bool any = false;
				foreach (var f in kv.Value.GetContainedFluids())
				{
					if (f.IsEmpty || f.Type is null) continue;
					any = true;
					if (!_paletteIdx.ContainsKey(f.Type.Id))
					{
						_paletteIdx[f.Type.Id] = _palette.Count;
						_palette.Add(f.Type.Id);
					}
				}
				if (any) n++;
			}

			int count = n;
			LargePacket.Send(PacketType.FluidPipeStats, p =>
			{
				p.Write((ushort)_palette.Count);
				foreach (var id in _palette) p.Write(id);

				p.Write(count);
				foreach (var kv in FluidPipeLayerSystem.AllStates)
				{
					float dx = (kv.Key.x * 16f + 8f) - cx, dy = (kv.Key.y * 16f + 8f) - cy;
					if (dx * dx + dy * dy > rangeSq) continue;
					var fluids = kv.Value.GetContainedFluids();
					bool any = false;
					foreach (var f in fluids) if (!f.IsEmpty && f.Type != null) { any = true; break; }
					if (!any) continue;

					int cap = kv.Value.CapacityPerTank;
					p.Write((short)kv.Key.x);
					p.Write((short)kv.Key.y);
					p.Write((byte)fluids.Length);
					foreach (var f in fluids)
					{
						if (f.IsEmpty || f.Type is null)
						{
							p.Write((ushort)0xFFFF);
							p.Write((byte)0);
						}
						else
						{
							p.Write((ushort)_paletteIdx[f.Type.Id]);
							int fill = cap > 0 ? (int)((long)f.Amount * 255 / cap) : 255;
							p.Write((byte)System.Math.Clamp(fill, 1, 255));
						}
					}
				}
			}, toClient: pid);
		}
	}

	public static void HandleOnClient(BinaryReader r)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;

		int paletteCount = r.ReadUInt16();
		var palette = new string[paletteCount];
		for (int i = 0; i < paletteCount; i++) palette[i] = r.ReadString();

		int n = r.ReadInt32();
		var cache = FluidPipeLayerSystem.ClientTankSnapshots;
		cache.Clear();
		for (int i = 0; i < n; i++)
		{
			int x = r.ReadInt16();
			int y = r.ReadInt16();
			int channels = r.ReadByte();
			int cap = ClientCapacity(x, y);
			var stacks = new FluidStack[channels];
			for (int c = 0; c < channels; c++)
			{
				int pi   = r.ReadUInt16();
				int fill = r.ReadByte();
				if (pi >= paletteCount || fill <= 0 || !FluidRegistry.TryGet(palette[pi], out var ft))
					stacks[c] = FluidStack.Empty;
				else
				{
					int amount = System.Math.Max(1, (int)((long)fill * cap / 255));
					stacks[c] = new FluidStack(ft, amount);
				}
			}
			cache[(x, y)] = stacks;
		}
	}

	private static int ClientCapacity(int x, int y)
	{
		var c = FluidPipeLayerSystem.Pipes.CellAt(x, y);
		return c.HasValue ? System.Math.Max(1, c.Value.Throughput * 20) : 1;
	}
}
