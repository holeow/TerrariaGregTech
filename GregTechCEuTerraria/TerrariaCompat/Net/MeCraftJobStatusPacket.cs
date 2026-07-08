#nullable enable
using System;
using System.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Me.Common;
using GregTechCEuTerraria.Config;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class MeCraftJobStatusPacket
{
	public static void Notify(Guid jobId, AEKey what, long requested, long remaining,
		PendingCraftingJobs.Status status, int playerId)
	{
		if (Main.netMode != NetmodeID.Server)
		{
			Apply(jobId, what, requested, remaining, status);
			return;
		}
		var p = NetRouter.NewPacket(PacketType.MeCraftJobStatus);
		p.Write(jobId.ToByteArray());
		AEKey.WriteKey(p, what);
		p.Write7BitEncodedInt64(requested);
		p.Write7BitEncodedInt64(remaining);
		p.Write((byte)status);
		p.Send(toClient: playerId);
	}

	public static void HandleOnClient(BinaryReader r)
	{
		var jobId = new Guid(r.ReadBytes(16));
		var what = AEKey.ReadKey(r);
		long requested = r.Read7BitEncodedInt64();
		long remaining = r.Read7BitEncodedInt64();
		var status = (PendingCraftingJobs.Status)r.ReadByte();
		if (what != null) Apply(jobId, what, requested, remaining, status);
	}

	private static void Apply(Guid jobId, AEKey what, long requested, long remaining,
		PendingCraftingJobs.Status status)
	{
		PendingCraftingJobs.JobStatus(jobId, what, requested, remaining, status);
		if (status == PendingCraftingJobs.Status.STARTED && GTClientConfig.Instance.PinAutoCraftedItems)
			PinnedKeys.PinKey(what, PinnedKeys.PinReason.CRAFTING);
	}
}
