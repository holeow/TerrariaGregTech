// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.client.gui.me.common.Repo), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.

#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Me.Search;
using GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Widgets;
using GregTechCEuTerraria.AppliedEnergistics.Menu.Me.Common;

namespace GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Me.Common;

public class Repo : IClientRepo
{
	public static readonly IComparer<GridInventoryEntry> AmountAsc = Comparer<GridInventoryEntry>.Create(
		(a, b) =>
		{
			double da = (double)a.StoredAmount / a.What!.GetAmountPerUnit();
			double db = (double)b.StoredAmount / b.What!.GetAmountPerUnit();
			return da.CompareTo(db);
		});

	public static readonly IComparer<GridInventoryEntry> AmountDesc =
		Comparer<GridInventoryEntry>.Create((a, b) => AmountAsc.Compare(b, a));

	private static readonly IComparer<GridInventoryEntry> PinnedRowComparator =
		Comparer<GridInventoryEntry>.Create((a, b) =>
		{
			var ia = a.What != null ? PinnedKeys.GetPinInfo(a.What) : null;
			var ib = b.What != null ? PinnedKeys.GetPinInfo(b.What) : null;
			var sa = ia != null ? ia.Since : DateTime.MaxValue;
			var sb = ib != null ? ib.Since : DateTime.MaxValue;
			return sa.CompareTo(sb);
		});

	private int _rowSize = 9;
	private bool _hasPower;

	private readonly Dictionary<long, GridInventoryEntry> _entries = new();
	private readonly List<GridInventoryEntry> _view = new();
	private readonly List<GridInventoryEntry> _pinnedRow = new();
	private readonly RepoSearch _search = new();
	private System.Action? _updateViewListener;

	private readonly IScrollSource _src;
	private readonly ISortSource _sortSrc;
	private bool _paused;

	public Repo(IScrollSource src, ISortSource sortSrc)
	{
		_src = src;
		_sortSrc = sortSrc;
	}

	public void HandleUpdate(bool fullUpdate, List<GridInventoryEntry> entries)
	{
		if (fullUpdate)
			Clear();

		foreach (var entry in entries)
			HandleUpdate(entry);

		UpdateView();
	}

	private void HandleUpdate(GridInventoryEntry serverEntry)
	{
		if (!_entries.TryGetValue(serverEntry.Serial, out var localEntry))
		{
			if (serverEntry.What == null)
				return;
			if (serverEntry.IsMeaningful())
				_entries[serverEntry.Serial] = serverEntry;
			return;
		}

		if (!serverEntry.IsMeaningful())
		{
			_entries.Remove(serverEntry.Serial);
		}
		else if (serverEntry.What == null)
		{
			_entries[serverEntry.Serial] = new GridInventoryEntry(
				serverEntry.Serial,
				localEntry.What,
				serverEntry.StoredAmount,
				serverEntry.RequestableAmount,
				serverEntry.Craftable);
		}
		else
		{
			_entries[serverEntry.Serial] = serverEntry;
		}
	}

	public void UpdateView()
	{
		if (IsPaused())
		{
			var visibleSerials = new HashSet<long>(_view.Count);
			UpdateEntriesWhilePaused(_pinnedRow, visibleSerials);
			UpdateEntriesWhilePaused(_view, visibleSerials);

			var pinnedRowFreeSlots = GetFreeSlots(_pinnedRow);
			var viewFreeSlots = GetFreeSlots(_view);
			var entriesToAdd = new List<GridInventoryEntry>();

			foreach (var serverEntry in _entries.Values)
			{
				if (visibleSerials.Contains(serverEntry.Serial))
					continue;
				if (TakeOverSlotOccupiedByRemovedItem(serverEntry, pinnedRowFreeSlots, _pinnedRow)
					|| TakeOverSlotOccupiedByRemovedItem(serverEntry, viewFreeSlots, _view))
					continue;
				entriesToAdd.Add(serverEntry);
			}

			AddEntriesToView(entriesToAdd);
		}
		else
		{
			_view.Clear();
			_pinnedRow.Clear();
			_view.Capacity = System.Math.Max(_view.Capacity, _entries.Count);
			AddEntriesToView(_entries.Values);
		}

		if (!IsPaused())
		{
			_pinnedRow.Sort(PinnedRowComparator);

			var sortOrder = _sortSrc.GetSortBy();
			var sortDir = _sortSrc.GetSortDir();
			_view.Sort(GetComparator(sortOrder, sortDir));
		}

		_updateViewListener?.Invoke();
	}

	private void AddEntriesToView(ICollection<GridInventoryEntry> entries)
	{
		var viewMode = _sortSrc.GetSortDisplay();
		var typeFilter = _sortSrc.GetTypeFilter().GetFilter();

		var hasPinnedRow = !PinnedKeys.IsEmpty();

		foreach (var entry in entries)
		{
			if (hasPinnedRow && _pinnedRow.Count < _rowSize && entry.What != null && PinnedKeys.IsPinned(entry.What))
			{
				_pinnedRow.Add(entry);
				continue;
			}

			if (viewMode == ViewItems.CRAFTABLE && !entry.Craftable)
				continue;
			if (viewMode == ViewItems.STORED && entry.StoredAmount == 0)
				continue;
			if (entry.What != null && !typeFilter.Matches(entry.What))
				continue;
			if (_search.Matches(entry))
				_view.Add(entry);
		}

		if (hasPinnedRow)
		{
			foreach (var pinnedKey in PinnedKeys.GetPinnedKeys())
			{
				var info = PinnedKeys.GetPinInfo(pinnedKey);
				if (info!.Reason != PinnedKeys.PinReason.CRAFTING && !PinnedRowContains(pinnedKey))
					_pinnedRow.Add(new GridInventoryEntry(-1, pinnedKey, 0, 0, false));
			}
		}
	}

	private bool PinnedRowContains(AEKey what)
	{
		foreach (var entry in _pinnedRow)
			if (what.Equals(entry.What))
				return true;
		return false;
	}

	private void UpdateEntriesWhilePaused(List<GridInventoryEntry> shownEntries, HashSet<long> visibleSerials)
	{
		for (int i = 0; i < shownEntries.Count; i++)
		{
			var entry = shownEntries[i];
			if (!_entries.TryGetValue(entry.Serial, out var serverEntry))
			{
				entry = new GridInventoryEntry(entry.Serial, entry.What, 0, 0, false);
			}
			else
			{
				entry = serverEntry;
			}
			visibleSerials.Add(entry.Serial);
			shownEntries[i] = entry;
		}
	}

	private Dictionary<AEKey, List<int>> GetFreeSlots(List<GridInventoryEntry> slots)
	{
		var freeSlots = new Dictionary<AEKey, List<int>>();
		for (int i = 0; i < slots.Count; i++)
		{
			var entry = slots[i];
			if (entry.What != null && !_entries.ContainsKey(entry.Serial))
			{
				if (!freeSlots.TryGetValue(entry.What, out var list))
					freeSlots[entry.What] = list = new List<int>();
				list.Add(i);
			}
		}
		foreach (var list in freeSlots.Values)
			list.Reverse();
		return freeSlots;
	}

	private static bool TakeOverSlotOccupiedByRemovedItem(GridInventoryEntry serverEntry,
		Dictionary<AEKey, List<int>> freeSlots, List<GridInventoryEntry> slots)
	{
		if (serverEntry.What == null || !freeSlots.TryGetValue(serverEntry.What, out var indices))
			return false;

		int i = indices[indices.Count - 1];
		indices.RemoveAt(indices.Count - 1);
		if (indices.Count == 0)
			freeSlots.Remove(serverEntry.What);

		slots[i] = serverEntry;
		return true;
	}

	private IComparer<GridInventoryEntry> GetComparator(SortOrder sortOrder, SortDir sortDir)
	{
		if (sortOrder == SortOrder.AMOUNT)
			return sortDir == SortDir.ASCENDING ? AmountAsc : AmountDesc;

		var keyComparator = KeySorters.GetComparator(sortOrder, sortDir);
		return Comparer<GridInventoryEntry>.Create((a, b) => keyComparator.Compare(a.What!, b.What!));
	}

	public IReadOnlyList<GridInventoryEntry> GetView() => _view;

	public IReadOnlyList<GridInventoryEntry> GetPinnedEntries() => _pinnedRow;

	public GridInventoryEntry? Get(int idx)
	{
		if (_pinnedRow.Count > 0)
		{
			if (idx < _rowSize)
			{
				if (idx < _pinnedRow.Count)
					return _pinnedRow[idx];
				return null;
			}
			idx -= _rowSize;
		}

		idx += _src.GetCurrentScroll() * _rowSize;
		if (idx < 0 || idx >= _view.Count)
			return null;
		return _view[idx];
	}

	public int Size() => _view.Count + _pinnedRow.Count;

	public void Clear()
	{
		_entries.Clear();
		_view.Clear();
		_pinnedRow.Clear();
	}

	public bool HasPinnedRow() => _pinnedRow.Count > 0;

	public bool HasPower() => _hasPower;
	public void SetPower(bool hasPower) => _hasPower = hasPower;

	public int GetRowSize() => _rowSize;
	public void SetRowSize(int rowSize) => _rowSize = rowSize;

	public string GetSearchString() => _search.GetSearchString();
	public void SetSearchString(string searchString) => _search.SetSearchString(searchString);

	public bool IsPaused() => _paused;

	public void SetPaused(bool paused)
	{
		if (_paused != paused)
		{
			_paused = paused;
			if (!paused)
				UpdateView();
		}
	}

	public ICollection<GridInventoryEntry> GetAllEntries() => _entries.Values;

	public void SetUpdateViewListener(System.Action listener) => _updateViewListener = listener;

	public bool IsCraftable(AEKey what)
	{
		foreach (var entry in _entries.Values)
			if (entry.Craftable && what.Equals(entry.What))
				return true;
		return false;
	}
}
