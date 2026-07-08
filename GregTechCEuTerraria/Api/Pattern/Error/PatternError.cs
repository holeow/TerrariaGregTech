#nullable enable
using System.Collections.Generic;

using Terraria;
using Terraria.Localization;

namespace GregTechCEuTerraria.Api.Pattern.Error;

public class PatternError
{
	public MultiblockState? WorldState { get; private set; }

	public void SetWorldState(MultiblockState worldState) => WorldState = worldState;

	public int GetX() => WorldState?.PosX ?? 0;
	public int GetY() => WorldState?.PosY ?? 0;

	public virtual List<List<Item>> GetCandidates()
	{
		var candidates = new List<List<Item>>();
		var predicate = WorldState?.Predicate;
		if (predicate is null) return candidates;
		foreach (var common in predicate.Common)
			candidates.Add(common.GetCandidates());
		foreach (var limited in predicate.Limited)
			candidates.Add(limited.GetCandidates());
		return candidates;
	}

	public virtual string ErrorInfo
	{
		get
		{
			var candidates = GetCandidates();
			var sb = new System.Text.StringBuilder();
			int shown = 0;
			int total = 0;
			foreach (var candidate in candidates)
			{
				if (candidate.Count == 0) continue;
				total++;
				if (shown < 2)
				{
					if (shown > 0) sb.Append(", ");
					sb.Append(global::GregTechCEuTerraria.Api.Util.TerrariaText.ItemName(candidate[0].type));
					shown++;
				}
			}
			if (total > shown) sb.Append($" +{total - shown} more");
			return $"Wrong block at ({GetX()}, {GetY()}): expected {sb} (found: {FoundBlockDescriptor()})";
		}
	}

	public string FoundBlockDescriptor()
	{
		int x = GetX(), y = GetY();
		if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return "out of world";
		var t = Main.tile[x, y];
		if (!t.HasTile) return "empty";
		var modTile = Terraria.ModLoader.TileLoader.GetTile(t.TileType);
		if (modTile != null)
		{
			if (modTile.Mod.TryFind<Terraria.ModLoader.ModItem>(modTile.Name, out var mi))
				return global::GregTechCEuTerraria.Api.Util.TerrariaText.ItemName(mi.Type);
			return modTile.Name;
		}
		string vanilla = Lang.GetMapObjectName(Terraria.Map.MapHelper.TileToLookup(t.TileType, 0));
		return string.IsNullOrEmpty(vanilla) ? $"Tile #{t.TileType}" : vanilla;
	}

	public virtual IEnumerable<string> ErrorDetailLines()
	{
		yield return $"Wrong block at ({GetX()}, {GetY()}) - found: {FoundBlockDescriptor()}";
		yield return "Expected one of:";
		var candidates = GetCandidates();
		int shown = 0;
		foreach (var group in candidates)
		{
			if (group.Count == 0) continue;
			string name = global::GregTechCEuTerraria.Api.Util.TerrariaText.ItemName(group[0].type);
			yield return group.Count > 1
				? $"  - {name} (+{group.Count - 1} more)"
				: $"  - {name}";
			if (++shown >= 6) { yield return "  - ..."; break; }
		}
	}
}
