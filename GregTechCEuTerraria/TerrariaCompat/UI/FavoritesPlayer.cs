#nullable enable
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class FavoritesPlayer : ModPlayer
{
	public readonly record struct Entry(int ItemType, string? FluidId, string? FluidLabel);

	private const string Key = "gtFavorites";

	private readonly List<Entry> _entries = new();

	public static FavoritesPlayer Local => Main.LocalPlayer.GetModPlayer<FavoritesPlayer>();

	public IReadOnlyList<Entry> Entries => _entries;

	public bool IsItemFavorite(int itemType) =>
		itemType > 0 && IndexOfItem(itemType) >= 0;

	public bool IsFluidFavorite(string fluidId) =>
		!string.IsNullOrEmpty(fluidId) && IndexOfFluid(fluidId) >= 0;

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
		var list = new List<TagCompound>();
		foreach (var e in _entries)
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
			else continue;
			list.Add(sub);
		}
		tag[Key] = list;
	}

	public override void LoadData(TagCompound tag)
	{
		_entries.Clear();
		if (!tag.TryGet<List<TagCompound>>(Key, out var list)) return;
		foreach (var sub in list)
		{
			if (sub.ContainsKey("item"))
			{
				var item = ItemIO.Load(sub.GetCompound("item"));
				if (item is not null && !item.IsAir && item.type > ItemID.None)
					AddItemSilent(item.type);
			}
			else if (sub.ContainsKey("fluidId"))
			{
				string id = sub.GetString("fluidId");
				string? label = sub.ContainsKey("fluidLabel") ? sub.GetString("fluidLabel") : null;
				AddFluidSilent(id, label);
			}
		}
	}

	private void AddItemSilent(int itemType)
	{
		if (itemType <= 0 || IndexOfItem(itemType) >= 0) return;
		_entries.Add(new Entry(itemType, null, null));
	}

	private void AddFluidSilent(string fluidId, string? fluidLabel)
	{
		if (string.IsNullOrEmpty(fluidId) || IndexOfFluid(fluidId) >= 0) return;
		_entries.Add(new Entry(0, fluidId, fluidLabel));
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
