#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using GregTechCEuTerraria.Api.Recipe;

namespace GregTechCEuTerraria.TerrariaCompat.Recipes;

public static class RecipeRegistry
{
	private static IReadOnlyDictionary<string, IReadOnlyList<GTRecipe>> _byStation =
		new Dictionary<string, IReadOnlyList<GTRecipe>>();

	public static int Count { get; private set; }
	public static IReadOnlyDictionary<string, IReadOnlyList<GTRecipe>> ByStation => _byStation;

	public static void Set(IReadOnlyDictionary<string, IReadOnlyList<GTRecipe>> map)
	{
		_byStation = map;
		Count = map.Values.Sum(l => l.Count);
	}

	public static void Clear()
	{
		_byStation = new Dictionary<string, IReadOnlyList<GTRecipe>>();
		Count = 0;
	}

	public static void AppendAll(IDictionary<string, List<GTRecipe>> additions)
	{
		var map = new Dictionary<string, IReadOnlyList<GTRecipe>>(_byStation);
		foreach (var (station, extras) in additions)
		{
			if (extras.Count == 0) continue;
			if (map.TryGetValue(station, out var existing))
				map[station] = existing.Concat(extras).ToList();
			else
				map[station] = extras;
		}
		Set(map);
	}

	public static void RemoveStations(params string[] stations)
	{
		var map = new Dictionary<string, IReadOnlyList<GTRecipe>>(_byStation);
		bool changed = false;
		foreach (var station in stations)
			if (map.Remove(station)) changed = true;
		if (changed) Set(map);
	}

	public static IReadOnlyList<GTRecipe> ForStation(string station) =>
		_byStation.TryGetValue(station, out var list) ? list : Array.Empty<GTRecipe>();
}
