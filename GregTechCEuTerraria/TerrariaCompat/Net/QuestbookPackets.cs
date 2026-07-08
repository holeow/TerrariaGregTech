#nullable enable
using System.Collections.Generic;
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Questbook;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class QuestbookPackets
{
	public static void SendStateRequest()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		NetRouter.NewPacket(PacketType.QuestbookStateRequest).Send();
	}

	public static void HandleStateRequest(BinaryReader r, int whoAmI)
	{
		if (Main.netMode != NetmodeID.Server) return;
		BuildSync(initial: true).Send(toClient: whoAmI);
	}

	public static void BroadcastSync()
	{
		if (Main.netMode != NetmodeID.Server) return;
		BuildSync(initial: false).Send();
	}

	private static ModPacket BuildSync(bool initial)
	{
		var p = NetRouter.NewPacket(PacketType.QuestbookSync);
		p.Write(initial);

		p.Write(QuestbookWorldProgress.Completed.Count);
		foreach (string id in QuestbookWorldProgress.Completed)
			p.Write(id);

		p.Write(QuestbookWorldProgress.SatisfiedTasks.Count);
		foreach (string key in QuestbookWorldProgress.SatisfiedTasks)
			p.Write(key);

		return p;
	}

	public static void HandleSync(BinaryReader r)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;

		bool initial = r.ReadBoolean();

		int nc = r.ReadInt32();
		var completed = new HashSet<string>(nc);
		for (int i = 0; i < nc; i++)
			completed.Add(r.ReadString());

		int nt = r.ReadInt32();
		var tasks = new HashSet<string>(nt);
		for (int i = 0; i < nt; i++)
			tasks.Add(r.ReadString());

		QuestbookWorldProgress.ApplySync(completed, tasks, initial);
	}

	public static void SendCompleteRequest(string questId)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		var p = NetRouter.NewPacket(PacketType.QuestbookComplete);
		p.Write(questId);
		p.Send();
	}

	public static void HandleCompleteRequest(BinaryReader r, int whoAmI)
	{
		if (Main.netMode != NetmodeID.Server) return;
		string questId = r.ReadString();

		if (!QuestbookSystem.QuestsById.ContainsKey(questId) || QuestbookSystem.IsAutoCheck(questId))
			return;

		QuestbookWorldProgress.Instance.CompleteQuest(questId, whoAmI);
	}

	public static void SendTaskCompleteRequest(string questId, int taskIndex)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		var p = NetRouter.NewPacket(PacketType.QuestbookTaskComplete);
		p.Write(questId);
		p.Write(taskIndex);
		p.Send();
	}

	public static void HandleTaskCompleteRequest(BinaryReader r, int whoAmI)
	{
		if (Main.netMode != NetmodeID.Server) return;
		string questId = r.ReadString();
		int taskIndex = r.ReadInt32();

		QuestbookWorldProgress.Instance.CompleteTask(questId, taskIndex, whoAmI);
	}
}
