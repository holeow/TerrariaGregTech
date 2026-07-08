#nullable enable
using System.IO;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class MeBusPackets
{
	private static void WriteAttachment(BinaryWriter p, MeBusAttachment? a)
	{
		p.Write((byte)(a?.Kind ?? MeBusKind.None));
		p.Write((byte)(a?.Access ?? AccessRestriction.READ_WRITE));
		p.Write(a?.Priority ?? 0);
		p.Write((byte)(a?.Speed ?? 0));
		p.Write(a?.CraftMissing ?? false);
		p.Write(a?.CraftOnly ?? false);
		p.Write((byte)(a?.Scheduling ?? MeBusSchedulingMode.Default));
		p.Write(a?.FilterOnExtract ?? true);
		p.Write(a?.ExtractableOnly ?? false);
		for (int i = 0; i < MeBusAttachment.FilterSize; i++)
			AEKey.WriteOptionalKey(p, a != null && i < a.Filter.Length ? a.Filter[i] : null);
	}

	private static MeBusAttachment? ReadAttachment(BinaryReader r)
	{
		var kind = (MeBusKind)r.ReadByte();
		var access = (AccessRestriction)r.ReadByte();
		int priority = r.ReadInt32();
		int speed = r.ReadByte();
		bool craftMissing = r.ReadBoolean();
		bool craftOnly = r.ReadBoolean();
		var sched = (MeBusSchedulingMode)r.ReadByte();
		bool filterExt = r.ReadBoolean();
		bool extractOnly = r.ReadBoolean();
		var filter = new AEKey?[MeBusAttachment.FilterSize];
		for (int i = 0; i < MeBusAttachment.FilterSize; i++)
			filter[i] = AEKey.ReadOptionalKey(r);
		if (kind == MeBusKind.None) return null;
		var att = new MeBusAttachment(kind, access, priority, speed)
		{
			CraftMissing = craftMissing,
			CraftOnly = craftOnly,
			Scheduling = sched,
			FilterOnExtract = filterExt,
			ExtractableOnly = extractOnly,
		};
		System.Array.Copy(filter, att.Filter, MeBusAttachment.FilterSize);
		return att;
	}

	public static void SetSide(int x, int y, IODirection side, MeBusAttachment? att)
	{
		MeBusLayerSystem.Buses.Set(x, y, side, att);
		if (Main.netMode == NetmodeID.SinglePlayer) return;
		var p = NetRouter.NewPacket(PacketType.MeBusSet);
		p.Write((short)x);
		p.Write((short)y);
		p.Write((byte)MeBusLayer.SideIndex(side));
		WriteAttachment(p, att);
		p.Send();
	}

	public static void HandleSet(BinaryReader r, int whoAmI)
	{
		int x = r.ReadInt16();
		int y = r.ReadInt16();
		var side = MeBusLayer.SideFromIndex(r.ReadByte());
		var att = ReadAttachment(r);

		if (Main.netMode == NetmodeID.Server)
		{
			MeBusLayerSystem.Buses.Set(x, y, side, att);
			var p = NetRouter.NewPacket(PacketType.MeBusSet);
			p.Write((short)x);
			p.Write((short)y);
			p.Write((byte)MeBusLayer.SideIndex(side));
			WriteAttachment(p, att);
			p.Send(ignoreClient: whoAmI);
			return;
		}
		if (Main.netMode == NetmodeID.MultiplayerClient)
			MeBusLayerSystem.Buses.Set(x, y, side, att);
	}

	public static void SendLayerRequest()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		NetRouter.NewPacket(PacketType.MeBusLayerRequest).Send();
	}

	public static void HandleLayerRequest(BinaryReader r, int whoAmI)
	{
		if (Main.netMode != NetmodeID.Server) return;
		var p = NetRouter.NewPacket(PacketType.MeBusLayerFull);
		p.Write(MeBusLayerSystem.Buses.All.Count);
		foreach (var kv in MeBusLayerSystem.Buses.All)
		{
			p.Write((short)kv.Key.x);
			p.Write((short)kv.Key.y);
			for (int i = 0; i < 4; i++)
				WriteAttachment(p, kv.Value[i]);
		}
		p.Send(toClient: whoAmI);
	}

	public static void HandleLayerFull(BinaryReader r)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		MeBusLayerSystem.Buses.Clear();
		int n = r.ReadInt32();
		for (int c = 0; c < n; c++)
		{
			int x = r.ReadInt16();
			int y = r.ReadInt16();
			for (int i = 0; i < 4; i++)
			{
				var att = ReadAttachment(r);
				if (att != null)
					MeBusLayerSystem.Buses.Set(x, y, MeBusLayer.SideFromIndex(i), att);
			}
		}
	}
}
