#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Questbook;

// Loads Data/Questbook/questlog.json
public sealed class QuestbookSystem : ModSystem
{
	public const string LocRoot = "Mods.GregTechCEuTerraria.Questbook";

	internal static QuestLogData Data { get; private set; } = new();
	internal static Dictionary<string, QuestData> QuestsById { get; private set; } = [];
	internal static Dictionary<string, ResolvedQuest> Resolved { get; private set; } = [];

	public override void PostSetupContent()
	{
		try
		{
			LoadQuestbook();
		}
		catch (Exception e)
		{
			Mod.Logger.Error("Questbook load failed", e);
		}
	}

	private void LoadQuestbook()
	{
		byte[] bytes = Mod.GetFileBytes("Data/Questbook/questlog.json");
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		Data = JsonSerializer.Deserialize<QuestLogData>(bytes, options) ?? new QuestLogData();
		QuestsById = Data.Quests.ToDictionary(q => q.Id);

		Resolved = [];

		foreach (QuestData quest in Data.Quests)
			Resolved[quest.Id] = ResolveQuest(quest);

		int total = Resolved.Count;
		int auto = Resolved.Values.Count(r => r.AutoCheck);
		Mod.Logger.Info($"Questbook: {Data.Chapters.Count} chapters, {total} quests "
			+ $"({auto} auto-check, {total - auto} manual)");
	}

	private static ResolvedQuest ResolveQuest(QuestData quest)
	{
		IngredientResolverImpl resolver = IngredientResolverImpl.Instance;
		var resolved = new ResolvedQuest();
		bool anyItemResolved = false;

		foreach (TaskData task in quest.Tasks)
		{
			if (task.Type == "item")
			{
				var types = new List<int>();
				foreach (string id in task.Items)
				{
					int t = resolver.ResolveItemType(id);
					if (t > 0)
						types.Add(t);
				}
				if (!string.IsNullOrEmpty(task.Tag))
					foreach (int t in resolver.ResolveItemTag(task.Tag))
						if (t > 0)
							types.Add(t);

				int[] accept = types.Distinct().ToArray();
				resolved.Tasks.Add(new ResolvedTask
				{
					IsItem = true,
					AcceptTypes = accept,
					Count = Math.Max(1, task.Count),
					Label = task.Label,
				});
				if (accept.Length > 0)
					anyItemResolved = true;
			}
			else
			{
				resolved.Tasks.Add(new ResolvedTask { IsItem = false });
			}
		}

		resolved.AutoCheck = anyItemResolved;

		resolved.IconType = resolver.ResolveItemType(quest.Icon);
		if (resolved.IconType <= 0)
			foreach (ResolvedTask t in resolved.Tasks)
				if (t.IsItem && t.AcceptTypes.Length > 0)
				{
					resolved.IconType = t.AcceptTypes[0];
					break;
				}

		return resolved;
	}

	internal static void ReResolve(QuestData quest)
		=> Resolved[quest.Id] = ResolveQuest(quest);

	internal static void RebuildIndex()
		=> QuestsById = Data.Quests.ToDictionary(q => q.Id);

	public static bool IsAutoCheck(string questId)
		=> Resolved.TryGetValue(questId, out ResolvedQuest? r) && r.AutoCheck;

	public static string TaskKey(string questId, int taskIndex) => questId + "#" + taskIndex;

	public static bool UpdateTaskProgress(string questId, Player player, HashSet<string> satisfied)
	{
		if (!Resolved.TryGetValue(questId, out ResolvedQuest? r) || !r.AutoCheck)
			return false;

		bool all = true;
		for (int i = 0; i < r.Tasks.Count; i++)
		{
			ResolvedTask task = r.Tasks[i];
			if (!task.IsItem || task.AcceptTypes.Length == 0)
				continue;

			string key = TaskKey(questId, i);
			if (satisfied.Contains(key))
				continue;

			if (IsItemTaskSatisfied(task, player))
				satisfied.Add(key);
			else
				all = false;
		}

		return all;
	}

	internal static bool IsItemTaskSatisfied(ResolvedTask task, Player player)
	{
		int have = 0;
		foreach (int type in task.AcceptTypes)
			have += CountInInventory(player, type);
		return have >= task.Count;
	}

	public static int CountInInventory(Player player, int itemType)
	{
		int total = 0;
		foreach (Item item in player.inventory)
			if (item is { IsAir: false } && item.type == itemType)
				total += item.stack;

		return total;
	}
}

internal sealed class ResolvedQuest
{
	public bool AutoCheck;
	public int IconType;
	public List<ResolvedTask> Tasks = [];
}

internal sealed class ResolvedTask
{
	public bool IsItem;
	public int[] AcceptTypes = [];
	public int Count = 1;
	public string Label = "";
}
