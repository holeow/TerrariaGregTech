// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.client.gui.me.search.RepoSearch + its predicate classes), Forge 1.20.1.
// LGPL-3.0-only. See AE2 LICENSE.

#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Cover.Filter;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Menu.Me.Common;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Me.Search;

public sealed class RepoSearch
{
	private string _searchString = "";

	private readonly Dictionary<long, bool> _cache = new();
	private Func<GridInventoryEntry, bool> _search = _ => true;

	public string GetSearchString() => _searchString;

	public void SetSearchString(string searchString)
	{
		if (searchString != _searchString)
		{
			_search = FromString(searchString);
			_searchString = searchString;
			_cache.Clear();
		}
	}

	public bool Matches(GridInventoryEntry entry)
	{
		if (_cache.TryGetValue(entry.Serial, out var cached))
			return cached;
		var result = _search(entry);
		_cache[entry.Serial] = result;
		return result;
	}

	private Func<GridInventoryEntry, bool> FromString(string searchString)
	{
		var orParts = searchString.Split('|');
		if (orParts.Length == 1)
			return And(GetPredicates(orParts[0]));

		var orFilters = new List<Func<GridInventoryEntry, bool>>(orParts.Length);
		foreach (var orPart in orParts)
			orFilters.Add(And(GetPredicates(orPart)));
		return Or(orFilters);
	}

	private static List<Func<GridInventoryEntry, bool>> GetPredicates(string query)
	{
		var terms = query.ToLowerInvariant().Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		var filters = new List<Func<GridInventoryEntry, bool>>(terms.Length);
		foreach (var part in terms)
		{
			if (part.StartsWith("@"))      filters.Add(ModPredicate(part.Substring(1)));
			else if (part.StartsWith("#")) filters.Add(TooltipPredicate(part.Substring(1)));
			else if (part.StartsWith("$")) filters.Add(TagPredicate(part.Substring(1)));
			else if (part.StartsWith("*")) filters.Add(ItemIdPredicate(part.Substring(1)));
			else                           filters.Add(NamePredicate(part));
		}
		return filters;
	}

	private static Func<GridInventoryEntry, bool> And(List<Func<GridInventoryEntry, bool>> ps)
	{
		if (ps.Count == 0) return _ => true;
		if (ps.Count == 1) return ps[0];
		return e => { foreach (var p in ps) if (!p(e)) return false; return true; };
	}

	private static Func<GridInventoryEntry, bool> Or(List<Func<GridInventoryEntry, bool>> ps)
	{
		if (ps.Count == 0) return _ => false;
		if (ps.Count == 1) return ps[0];
		return e => { foreach (var p in ps) if (p(e)) return true; return false; };
	}

	private static Func<GridInventoryEntry, bool> NamePredicate(string term)
	{
		term = term.ToLowerInvariant();
		return e => e.What != null &&
			e.What.GetDisplayName().ToLowerInvariant().Contains(term);
	}

	private static Func<GridInventoryEntry, bool> ModPredicate(string term)
	{
		term = term.ToLowerInvariant();
		return e =>
		{
			if (e.What is null) return false;
			var modId = e.What.GetModId();
			if (modId.ToLowerInvariant().Contains(term)) return true;
			var modName = ModName(modId).ToLowerInvariant();
			return modName.Contains(term);
		};
	}

	private static Func<GridInventoryEntry, bool> ItemIdPredicate(string term)
	{
		term = term.ToLowerInvariant();
		return e => e.What != null && e.What.Id.ToLowerInvariant().Contains(term);
	}

	private static Func<GridInventoryEntry, bool> TagPredicate(string term)
	{
		term = term.ToLowerInvariant();
		return e =>
		{
			if (e.What is not AEItemKey itemKey) return false;
			foreach (var tag in TagSource.TagsOf(itemKey.GetReadOnlyStack()))
				if (tag.ToLowerInvariant().Contains(term)) return true;
			return false;
		};
	}

	private static Func<GridInventoryEntry, bool> TooltipPredicate(string term)
	{
		term = term.ToLowerInvariant().Replace(" ", "");
		return e => e.What != null &&
			e.What.GetDisplayName().ToLowerInvariant().Replace(" ", "").Contains(term);
	}

	private static string ModName(string modId)
	{
		if (modId == "Terraria") return "Terraria";
		try { return ModLoader.TryGetMod(modId, out var mod) ? mod.DisplayName : modId; }
		catch { return modId; }
	}
}
