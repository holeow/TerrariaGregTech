#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GregTechCEuTerraria.Config;
using Microsoft.Xna.Framework;
using ReLogic.OS;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Questbook;

internal static class QuestbookEditor
{
	internal static bool Dirty { get; private set; }
	internal static bool AwaitingDep { get; private set; }
	internal static string? DepTarget { get; private set; }
	internal static bool DepPickArmed { get; private set; }

	internal static void StartAddDep(string questId)
	{
		AwaitingDep = true;
		DepTarget = questId;
		DepPickArmed = false;
	}

	internal static void ArmDepPick() => DepPickArmed = true;

	internal static void CancelAddDep()
	{
		AwaitingDep = false;
		DepTarget = null;
		DepPickArmed = false;
	}

	internal static bool Enabled => GTClientConfig.Instance.QuestbookEditMode;

	private static void MarkDirty() => Dirty = true;

	private static string NewId()
		=> Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();

	internal static ChapterData AddChapter()
	{
		int order = QuestbookSystem.Data.Chapters.Count == 0
			? 0
			: QuestbookSystem.Data.Chapters.Max(c => c.Order) + 1;
		var chapter = new ChapterData
		{
			Key = NewId(),
			Order = order,
			Title = "New Chapter",
		};
		QuestbookSystem.Data.Chapters.Add(chapter);
		MarkDirty();
		return chapter;
	}

	internal static void DeleteChapter(ChapterData chapter)
	{
		QuestbookSystem.Data.Chapters.Remove(chapter);
		MarkDirty();
	}

	internal static QuestData AddQuest(ChapterData chapter, float x, float y)
	{
		var quest = new QuestData
		{
			Id = NewId(),
			Title = "New Quest",
		};
		QuestbookSystem.Data.Quests.Add(quest);
		chapter.Nodes.Add(new NodeData { Quest = quest.Id, X = x, Y = y });

		QuestbookSystem.RebuildIndex();
		QuestbookSystem.ReResolve(quest);
		MarkDirty();
		return quest;
	}

	internal static void DeleteQuest(string id)
	{
		QuestbookSystem.Data.Quests.RemoveAll(q => q.Id == id);

		foreach (ChapterData chapter in QuestbookSystem.Data.Chapters)
			chapter.Nodes.RemoveAll(n => n.Quest == id);
		foreach (QuestData q in QuestbookSystem.Data.Quests)
			q.Deps.RemoveAll(d => d == id);

		QuestbookSystem.Resolved.Remove(id);
		QuestbookSystem.RebuildIndex();

		if (DepTarget == id) CancelAddDep();
		MarkDirty();
	}

	internal static void SetField(QuestData quest, Action<QuestData> mutate)
	{
		mutate(quest);
		QuestbookSystem.ReResolve(quest);
		MarkDirty();
	}

	internal static void AddTask(QuestData quest)
	{
		quest.Tasks.Add(new TaskData { Type = "item", Count = 1 });
		QuestbookSystem.ReResolve(quest);
		MarkDirty();
	}

	internal static void RemoveTask(QuestData quest, TaskData task)
	{
		quest.Tasks.Remove(task);
		QuestbookSystem.ReResolve(quest);
		MarkDirty();
	}

	internal static void OnNodeMoved() => MarkDirty();

	internal static void ToggleDep(string fromId, string toId)
	{
		if (fromId == toId)
			return;
		if (!QuestbookSystem.QuestsById.TryGetValue(toId, out QuestData? to))
			return;
		if (!QuestbookSystem.QuestsById.ContainsKey(fromId))
			return;

		if (!to.Deps.Remove(fromId))
			to.Deps.Add(fromId);
		MarkDirty();
	}

	internal static string? OnDepPickClick(string clickedQuestId)
	{
		string? target = DepTarget;
		if (target != null && clickedQuestId != target)
			ToggleDep(clickedQuestId, target);
		CancelAddDep();
		return target;
	}

	private const string GeneratedHeader = "edited in-game via the GregTech questbook editor; "
		+ "re-run tools/scripts/port-questbook.py to regenerate from FTB source";

	private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

	private static string ToJson(QuestLogData doc)
	{
		var body = (JsonObject)JsonSerializer.SerializeToNode(doc, CamelCase)!;
		var ordered = new JsonObject { ["$generated"] = GeneratedHeader };
		foreach (string key in body.Select(kv => kv.Key).ToList())
		{
			JsonNode? value = body[key];
			body.Remove(key);
			ordered[key] = value;
		}
		return ordered.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
	}

	internal static bool ExportBookToClipboard()
	{
		try
		{
			Platform.Get<IClipboard>().Value = ToJson(QuestbookSystem.Data);
			Dirty = false;
			Main.NewText($"[Questbook] Copied the whole book ({QuestbookSystem.Data.Chapters.Count} chapters) "
				+ "to the clipboard", new Color(120, 230, 120));
			return true;
		}
		catch (Exception e)
		{
			Main.NewText($"[Questbook] Export failed: {e.Message}", Color.Red);
			return false;
		}
	}

	internal static bool ExportChapterToClipboard(ChapterData chapter)
	{
		try
		{
			var ids = new HashSet<string>(chapter.Nodes.Select(n => n.Quest));
			List<QuestData> quests = QuestbookSystem.Data.Quests.Where(q => ids.Contains(q.Id)).ToList();
			var doc = new QuestLogData
			{
				Pack = QuestbookSystem.Data.Pack,
				Chapters = { chapter },
				Quests = quests,
			};
			Platform.Get<IClipboard>().Value = ToJson(doc);
			Main.NewText($"[Questbook] Copied chapter '{chapter.Title}' ({quests.Count} quests) "
				+ "to the clipboard", new Color(120, 230, 120));
			return true;
		}
		catch (Exception e)
		{
			Main.NewText($"[Questbook] Export failed: {e.Message}", Color.Red);
			return false;
		}
	}

	internal static bool ExportQuestToClipboard(QuestData quest)
	{
		try
		{
			var doc = new QuestLogData { Pack = QuestbookSystem.Data.Pack, Quests = { quest } };
			Platform.Get<IClipboard>().Value = ToJson(doc);
			string name = string.IsNullOrEmpty(quest.Title) ? quest.Id : quest.Title;
			Main.NewText($"[Questbook] Copied quest '{name}' to the clipboard", new Color(120, 230, 120));
			return true;
		}
		catch (Exception e)
		{
			Main.NewText($"[Questbook] Export failed: {e.Message}", Color.Red);
			return false;
		}
	}
}
