#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public static class RecipeNetworkCrafting
{
	public static HashSet<int> StationTiles(Item[] stations)
	{
		var set = new HashSet<int>();
		foreach (var it in stations)
			if (it is { IsAir: false } && it.createTile > -1) set.Add(it.createTile);
		return set;
	}

	private static HashSet<int>? _stationTiles;

	public static bool IsCraftingStationTile(int tile)
	{
		if (tile <= -1) return false;
		if (_stationTiles == null)
		{
			_stationTiles = new HashSet<int>();
			for (int i = 0; i < Terraria.Recipe.numRecipes; i++)
			{
				var r = Main.recipe[i];
				if (r == null) continue;
				foreach (int t in r.requiredTile)
					if (t > -1) _stationTiles.Add(t);
			}
		}
		return _stationTiles.Contains(tile);
	}

	public static bool IsCraftingStationItem(Item item) =>
		item is { IsAir: false } && IsCraftingStationTile(item.createTile);

	private static void AcceptableTypes(Terraria.Recipe r, Item req, HashSet<int> outTypes)
	{
		outTypes.Clear();
		outTypes.Add(req.type);
		foreach (int gid in r.acceptedGroups)
			if (RecipeGroup.recipeGroups.TryGetValue(gid, out var g) && g.ValidItems.Contains(req.type))
				outTypes.UnionWith(g.ValidItems);
	}

	public static bool StationsSatisfy(Terraria.Recipe r, HashSet<int> stations)
	{
		foreach (int t in r.requiredTile)
			if (t > -1 && !stations.Contains(t)) return false;
		return true;
	}

	public static Dictionary<int, long> NetByType(KeyCounter avail)
	{
		var d = new Dictionary<int, long>();
		foreach (var kv in avail)
			if (kv.Key is AEItemKey ik)
			{
				int t = ik.GetItem();
				d[t] = (d.TryGetValue(t, out var c) ? c : 0) + kv.Value;
			}
		return d;
	}

	public static long AvailableFor(Terraria.Recipe r, Item req, Dictionary<int, long> netByType)
	{
		var acc = new HashSet<int>();
		AcceptableTypes(r, req, acc);
		long have = 0;
		foreach (int t in acc)
			if (netByType.TryGetValue(t, out var c)) have += c;
		return have;
	}

	public static long MaxCrafts(Terraria.Recipe r, Dictionary<int, long> netByType, HashSet<int> stations)
	{
		if (r.createItem.IsAir || !StationsSatisfy(r, stations)) return 0;

		long ub = long.MaxValue;
		var acc = new HashSet<int>();
		foreach (var req in r.requiredItem)
		{
			if (req is null || req.IsAir || req.stack <= 0) continue;
			AcceptableTypes(r, req, acc);
			long have = 0;
			foreach (int t in acc)
				if (netByType.TryGetValue(t, out var c)) have += c;
			ub = System.Math.Min(ub, have / req.stack);
			if (ub == 0) return 0;
		}
		if (ub == long.MaxValue) return 9999;

		long lo = 1, hi = ub, best = 0;
		while (lo <= hi)
		{
			long mid = lo + (hi - lo) / 2;
			if (Affordable(r, netByType, mid)) { best = mid; lo = mid + 1; }
			else hi = mid - 1;
		}
		return best;
	}

	private static bool Affordable(Terraria.Recipe r, Dictionary<int, long> netByType, long times)
	{
		var remaining = new Dictionary<int, long>(netByType);
		var acc = new HashSet<int>();
		foreach (var req in r.requiredItem)
		{
			if (req is null || req.IsAir) continue;
			AcceptableTypes(r, req, acc);
			long need = (long)req.stack * times;
			foreach (int t in acc)
			{
				if (need <= 0) break;
				if (!remaining.TryGetValue(t, out var have) || have <= 0) continue;
				long take = System.Math.Min(need, have);
				remaining[t] = have - take;
				need -= take;
			}
			if (need > 0) return false;
		}
		return true;
	}

	public static bool IsCraftable(Terraria.Recipe r, Dictionary<int, long> netByType, HashSet<int> stations)
	{
		if (r.createItem.IsAir) return false;
		if (!StationsSatisfy(r, stations)) return false;
		var acc = new HashSet<int>();
		foreach (var req in r.requiredItem)
		{
			if (req is null || req.IsAir) continue;
			AcceptableTypes(r, req, acc);
			long have = 0;
			foreach (int t in acc)
				if (netByType.TryGetValue(t, out var c)) have += c;
			if (have < req.stack) return false;
		}
		return true;
	}

	public static bool Craft(Terraria.Recipe r, MEStorage net, HashSet<int> stations, Player player)
	{
		if (!ExtractIngredients(r, net, stations, IActionSource.Empty(), 1)) return false;
		GiveToPlayer(player, r.createItem, r.createItem.stack);
		return true;
	}

	public static bool ExtractIngredients(Terraria.Recipe r, MEStorage net, HashSet<int> stations,
		IActionSource src, int times)
	{
		if (r.createItem.IsAir || times <= 0 || !StationsSatisfy(r, stations)) return false;

		var keys = new List<(AEItemKey key, long amount)>();
		foreach (var kv in net.GetAvailableStacks())
			if (kv.Key is AEItemKey ik && kv.Value > 0) keys.Add((ik, kv.Value));

		var plan = new Dictionary<AEItemKey, long>();
		var acc = new HashSet<int>();
		foreach (var req in r.requiredItem)
		{
			if (req is null || req.IsAir) continue;
			AcceptableTypes(r, req, acc);
			long need = (long)req.stack * times;
			for (int i = 0; i < keys.Count && need > 0; i++)
			{
				var (k, amt) = keys[i];
				if (amt <= 0 || !acc.Contains(k.GetItem())) continue;
				long take = System.Math.Min(need, amt);
				plan[k] = (plan.TryGetValue(k, out var p) ? p : 0) + take;
				keys[i] = (k, amt - take);
				need -= take;
			}
			if (need > 0) return false;
		}

		foreach (var kv in plan)
			net.Extract(kv.Key, kv.Value, Actionable.MODULATE, src);
		return true;
	}

	public static List<CraftingPlanSummaryEntry> SimulateBatch(MEStorage net, Terraria.Recipe r,
		int times, out bool anyMissing)
	{
		anyMissing = false;
		var entries = new List<CraftingPlanSummaryEntry>();
		if (r.createItem.IsAir || times <= 0) return entries;

		var keys = new List<(AEItemKey key, long amount)>();
		foreach (var kv in net.GetAvailableStacks())
			if (kv.Key is AEItemKey ik && kv.Value > 0) keys.Add((ik, kv.Value));

		var acc = new HashSet<int>();
		foreach (var req in r.requiredItem)
		{
			if (req is null || req.IsAir) continue;
			AcceptableTypes(r, req, acc);
			long need = (long)req.stack * times;
			long taken = 0;
			for (int i = 0; i < keys.Count && taken < need; i++)
			{
				var (k, amt) = keys[i];
				if (amt <= 0 || !acc.Contains(k.GetItem())) continue;
				long take = System.Math.Min(need - taken, amt);
				taken += take;
				keys[i] = (k, amt - take);
			}
			long missing = need - taken;
			if (missing > 0) anyMissing = true;
			var key = AEItemKey.Of(req);
			if (key != null) entries.Add(new CraftingPlanSummaryEntry(key, missing, taken, 0));
		}
		return entries;
	}

	private static void GiveToPlayer(Player player, Item proto, int count)
	{
		while (count > 0)
		{
			var give = proto.Clone();
			give.stack = System.Math.Min(count, proto.maxStack);
			count -= give.stack;
			global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(
				player, player.GetSource_OpenItem(give.type), give);
		}
	}
}
