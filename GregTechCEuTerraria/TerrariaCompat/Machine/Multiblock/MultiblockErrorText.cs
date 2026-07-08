#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using GregTechCEuTerraria.Api.Pattern.Error;
using Terraria;
using Terraria.Localization;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock;

public static class MultiblockErrorText
{
	private static readonly string[] TierShort =
	{
		"ULV", "LV", "MV", "HV", "EV", "IV", "LuV", "ZPM",
		"UV",  "UHV","UEV","UIV","UXV","OpV","MAX",
	};

	public static string Describe(PatternError err)
	{
		if (err is SinglePredicateError spe) return DescribeCount(spe);
		if (err.GetType() == typeof(PatternError)) return DescribeWrongBlockLine(err);
		return err.ErrorInfo;
	}

	private static string DescribeWrongBlockLine(PatternError err)
	{
		var sb = new StringBuilder();
		int shown = 0;
		foreach (var group in err.GetCandidates())
		{
			if (group.Count == 0) continue;
			if (shown > 0) sb.Append(", ");
			sb.Append(DescribeGroup(group));
			shown++;
		}
		return $"Wrong block, found {err.FoundBlockDescriptor()}, expected {sb}";
	}

	public static string DescribeGroup(List<Item> candidates)
	{
		var bases    = new List<string>();
		var seenBase = new HashSet<string>(StringComparer.Ordinal);
		int minTier = int.MaxValue, maxTier = -1;

		foreach (var it in candidates)
		{
			if (it is null || it.IsAir) continue;
			(int tier, string baseName) = SplitTier(Api.Util.TerrariaText.ItemName(it.type));
			if (tier >= 0)
			{
				if (tier < minTier) minTier = tier;
				if (tier > maxTier) maxTier = tier;
			}
			if (seenBase.Add(baseName)) bases.Add(baseName);
		}

		if (bases.Count == 0) return "(unknown)";

		if (bases.Count == 1)
		{
			if (maxTier < 0) return bases[0];
			return minTier == maxTier
				? $"{TierShort[minTier]} {bases[0]}"
				: $"{TierShort[minTier]} {bases[0]} or better";
		}

		string common = CommonSuffix(bases);
		return common.Length > 0 ? common : bases[0];
	}

	private static string CommonSuffix(List<string> names)
	{
		string[][] words = new string[names.Count][];
		for (int i = 0; i < names.Count; i++)
			words[i] = names[i].Split(' ');

		var suffix = new List<string>();
		for (int back = 1; ; back++)
		{
			string? word = null;
			foreach (var w in words)
			{
				if (back > w.Length) { word = null; break; }
				string candidate = w[w.Length - back];
				if (word is null) word = candidate;
				else if (!string.Equals(word, candidate, StringComparison.Ordinal)) { word = null; break; }
			}
			if (word is null) break;
			suffix.Insert(0, word);
		}
		return string.Join(" ", suffix);
	}

	private static string DescribeCount(SinglePredicateError spe)
	{
		bool tooMany  = spe.Type == 0 || spe.Type == 2;
		bool perLayer = spe.Type == 2 || spe.Type == 3;
		int required = spe.Type switch
		{
			0 => spe.Predicate.MaxCount,
			1 => spe.Predicate.MinCount,
			2 => spe.Predicate.MaxLayerCount,
			3 => spe.Predicate.MinLayerCount,
			_ => -1,
		};

		int actual = -1;
		if (!perLayer && spe.WorldState is not null)
			spe.WorldState.GlobalCount.TryGetValue(spe.Predicate, out actual);

		string desc     = DescribeGroup(spe.Predicate.GetCandidates());
		string verb     = tooMany ? "Too many" : "Not enough";
		string layer    = perLayer ? " per layer" : "";
		string needWord = tooMany ? "max" : "need";
		string have     = actual >= 0 ? $", have {actual}" : "";
		return $"{verb} {desc}{layer} {needWord} {required}{have}";
	}

	private static (int tier, string baseName) SplitTier(string name)
	{
		int sp = name.IndexOf(' ');
		if (sp <= 0) return (-1, name);
		int idx = Array.IndexOf(TierShort, name.Substring(0, sp));
		return idx < 0 ? (-1, name) : (idx, name.Substring(sp + 1));
	}
}
