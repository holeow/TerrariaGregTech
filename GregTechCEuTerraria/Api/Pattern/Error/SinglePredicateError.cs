#nullable enable
using System.Collections.Generic;

using Terraria;
using Terraria.Localization;

namespace GregTechCEuTerraria.Api.Pattern.Error;

public class SinglePredicateError : PatternError
{
	public SimplePredicate Predicate { get; }
	public int Type { get; }

	public SinglePredicateError(SimplePredicate predicate, int type)
	{
		Predicate = predicate;
		Type = type;
	}

	public override List<List<Item>> GetCandidates() => new() { Predicate.GetCandidates() };

	public override string ErrorInfo
	{
		get
		{
			int required = -1;
			int actual   = -1;
			string what  = "";
			switch (Type)
			{
				case 0:
					required = Predicate.MaxCount;
					if (WorldState is not null) WorldState.GlobalCount.TryGetValue(Predicate, out actual);
					what = "too many";
					break;
				case 1:
					required = Predicate.MinCount;
					if (WorldState is not null) WorldState.GlobalCount.TryGetValue(Predicate, out actual);
					what = "not enough";
					break;
				case 2:
					required = Predicate.MaxLayerCount;
					what = "too many per layer";
					break;
				case 3:
					required = Predicate.MinLayerCount;
					what = "not enough per layer";
					break;
			}

			string names = JoinCandidateNames(Predicate.GetCandidates(), max: 1);

			string actualPart = actual >= 0 ? $" (have {actual})" : "";
			string namePart   = names.Length > 0 ? $" of {names}" : "";
			return $"{what}{namePart}: need {required}{actualPart}";
		}
	}

	public override System.Collections.Generic.IEnumerable<string> ErrorDetailLines()
	{
		yield return ErrorInfo;
	}

	private static string JoinCandidateNames(List<Item> candidates, int max)
	{
		if (candidates.Count == 0) return "";
		var sb = new System.Text.StringBuilder();
		int n = System.Math.Min(candidates.Count, max);
		for (int i = 0; i < n; i++)
		{
			if (i > 0) sb.Append(i == n - 1 && candidates.Count <= max ? " or " : ", ");
			sb.Append(global::GregTechCEuTerraria.Api.Util.TerrariaText.ItemName(candidates[i].type));
		}
		if (candidates.Count > max) sb.Append($" +{candidates.Count - max} more");
		return sb.ToString();
	}
}
