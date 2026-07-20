#nullable enable
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class FavoritesPlayer : ModPlayer
{
	public readonly record struct Entry(int ItemType, string? FluidId, string? FluidLabel,
		string? TagLabel = null, int[]? TagMembers = null);

	private const string Key = "gtFavorites";
	private const string HistoryKey = "gtHistory";
	private const int HistoryCap = 64;

	private readonly List<Entry> _entries = new();
	private readonly List<Entry> _history = new();

	public static FavoritesPlayer Local => Main.LocalPlayer.GetModPlayer<FavoritesPlayer>();

	public IReadOnlyList<Entry> Entries => _entries;
	public IReadOnlyList<Entry> History => _history;

	public void RecordItem(int itemType)
	{
		if (itemType <= 0) return;
		RecordEntry(new Entry(itemType, null, null), e => e.ItemType == itemType);
	}

	public void RecordFluid(string fluidId, string? label)
	{
		if (string.IsNullOrEmpty(fluidId)) return;
		RecordEntry(new Entry(0, fluidId, label), e => e.FluidId == fluidId);
	}

	public void RecordTag(string tagLabel, IEnumerable<int> members)
	{
		if (string.IsNullOrEmpty(tagLabel)) return;
		RecordEntry(new Entry(0, null, null, tagLabel, new List<int>(members).ToArray()),
			e => e.TagLabel == tagLabel);
	}

	private void RecordEntry(Entry entry, System.Predicate<Entry> match)
	{
		int idx = _history.FindIndex(match);
		if (idx >= 0) _history.RemoveAt(idx);
		_history.Insert(0, entry);
		if (_history.Count > HistoryCap) _history.RemoveRange(HistoryCap, _history.Count - HistoryCap);
	}

	public bool IsItemFavorite(int itemType) =>
		itemType > 0 && IndexOfItem(itemType) >= 0;

	public bool IsFluidFavorite(string fluidId) =>
		!string.IsNullOrEmpty(fluidId) && IndexOfFluid(fluidId) >= 0;

	public bool IsTagFavorite(string tagLabel) =>
		!string.IsNullOrEmpty(tagLabel) && IndexOfTag(tagLabel) >= 0;

	public void BringItemToFront(int itemType)
	{
		if (itemType <= 0) return;
		int idx = IndexOfItem(itemType);
		var entry = idx >= 0 ? _entries[idx] : new Entry(itemType, null, null);
		if (idx >= 0) _entries.RemoveAt(idx);
		_entries.Insert(0, entry);
	}

	public void BringFluidToFront(string fluidId, string? fluidLabel)
	{
		if (string.IsNullOrEmpty(fluidId)) return;
		int idx = IndexOfFluid(fluidId);
		var entry = idx >= 0 ? _entries[idx] : new Entry(0, fluidId, fluidLabel);
		if (idx >= 0) _entries.RemoveAt(idx);
		_entries.Insert(0, entry);
	}

	public void BringTagToFront(string tagLabel, IEnumerable<int> members)
	{
		if (string.IsNullOrEmpty(tagLabel)) return;
		int idx = IndexOfTag(tagLabel);
		var entry = idx >= 0 ? _entries[idx] : new Entry(0, null, null, tagLabel, new List<int>(members).ToArray());
		if (idx >= 0) _entries.RemoveAt(idx);
		_entries.Insert(0, entry);
	}

	public void RemoveTag(string tagLabel)
	{
		if (string.IsNullOrEmpty(tagLabel)) return;
		int idx = IndexOfTag(tagLabel);
		if (idx >= 0) _entries.RemoveAt(idx);
	}

	public void RemoveItem(int itemType)
	{
		if (itemType <= 0) return;
		int idx = IndexOfItem(itemType);
		if (idx >= 0) _entries.RemoveAt(idx);
	}

	public void RemoveFluid(string fluidId)
	{
		if (string.IsNullOrEmpty(fluidId)) return;
		int idx = IndexOfFluid(fluidId);
		if (idx >= 0) _entries.RemoveAt(idx);
	}

	public override void SaveData(TagCompound tag)
	{
		tag[Key] = SerializeEntries(_entries);
		tag[HistoryKey] = SerializeEntries(_history);
	}

	public override void LoadData(TagCompound tag)
	{
		_entries.Clear();
		_history.Clear();
		if (tag.TryGet<List<TagCompound>>(Key, out var favList)) DeserializeInto(favList, _entries);
		if (tag.TryGet<List<TagCompound>>(HistoryKey, out var histList)) DeserializeInto(histList, _history);
	}

	private static List<TagCompound> SerializeEntries(List<Entry> entries)
	{
		var list = new List<TagCompound>();
		foreach (var e in entries)
		{
			var sub = new TagCompound();
			if (e.ItemType > 0)
			{
				if (e.ItemType < ContentSamples.ItemsByType.Count)
					sub["item"] = ItemIO.Save(ContentSamples.ItemsByType[e.ItemType]);
				else continue;
			}
			else if (!string.IsNullOrEmpty(e.FluidId))
			{
				sub["fluidId"] = e.FluidId;
				if (!string.IsNullOrEmpty(e.FluidLabel)) sub["fluidLabel"] = e.FluidLabel;
			}
			else if (!string.IsNullOrEmpty(e.TagLabel))
			{
				sub["tagLabel"] = e.TagLabel;
				var ids = new List<string>();
				foreach (int type in e.TagMembers ?? System.Array.Empty<int>())
				{
					var id = Api.Recipe.Ingredient.IIngredientResolver.Default?.StableItemId(type);
					if (!string.IsNullOrEmpty(id)) ids.Add(id!);
				}
				sub["tagMemberIds"] = ids;
			}
			else continue;
			list.Add(sub);
		}
		return list;
	}

	private static void DeserializeInto(List<TagCompound> list, List<Entry> target)
	{
		foreach (var sub in list)
		{
			if (sub.ContainsKey("item"))
			{
				var item = ItemIO.Load(sub.GetCompound("item"));
				if (item is not null && !item.IsAir && item.type > ItemID.None
					&& !target.Exists(e => e.ItemType == item.type))
					target.Add(new Entry(item.type, null, null));
			}
			else if (sub.ContainsKey("fluidId"))
			{
				string id = sub.GetString("fluidId");
				string? label = sub.ContainsKey("fluidLabel") ? sub.GetString("fluidLabel") : null;
				if (!target.Exists(e => e.FluidId == id)) target.Add(new Entry(0, id, label));
			}
			else if (sub.ContainsKey("tagLabel"))
			{
				if (!sub.ContainsKey("tagMemberIds")) continue;
				string label = sub.GetString("tagLabel");
				var resolver = Api.Recipe.Ingredient.IIngredientResolver.Default;
				var members = new List<int>();
				foreach (var id in sub.GetList<string>("tagMemberIds"))
				{
					int type = resolver?.ResolveItemType(id) ?? 0;
					if (type > 0 && !members.Contains(type)) members.Add(type);
				}
				if (members.Count > 0 && !target.Exists(e => e.TagLabel == label))
					target.Add(new Entry(0, null, null, label, members.ToArray()));
			}
		}
	}

	private int IndexOfTag(string tagLabel)
	{
		for (int i = 0; i < _entries.Count; i++)
			if (_entries[i].TagLabel == tagLabel) return i;
		return -1;
	}

	private int IndexOfItem(int itemType)
	{
		for (int i = 0; i < _entries.Count; i++)
			if (_entries[i].ItemType == itemType) return i;
		return -1;
	}

	private int IndexOfFluid(string fluidId)
	{
		for (int i = 0; i < _entries.Count; i++)
			if (_entries[i].FluidId == fluidId) return i;
		return -1;
	}
}
