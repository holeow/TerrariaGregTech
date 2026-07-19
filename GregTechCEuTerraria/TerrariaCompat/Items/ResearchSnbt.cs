#nullable enable
using System;
using GregTechCEuTerraria.Api.Recipe.Ingredient;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public static class ResearchSnbt
{
	public static (string Id, string Type) Parse(string snbt) =>
		(ExtractQuoted(snbt, "research_id"), StripNs(ExtractQuoted(snbt, "research_type")));

	public static (int Type, string? Nbt) PeelItem(Ingredient? ing) => ing switch
	{
		SizedIngredient s          => PeelItem(s.Inner),
		NBTPredicateIngredient nbt => (nbt.ItemType, nbt.OutputNbt),
		ItemStackIngredient i      => (i.ItemType, null),
		TagIngredient t            => (t.GetItems().Count > 0 ? t.GetItems()[0].type : 0, null),
		_                          => (0, null),
	};

	private static string ExtractQuoted(string snbt, string key)
	{
		int k = snbt.IndexOf(key, StringComparison.Ordinal);
		if (k < 0) return "";
		int q1 = snbt.IndexOf('"', k);
		if (q1 < 0) return "";
		int q2 = snbt.IndexOf('"', q1 + 1);
		if (q2 < 0) return "";
		return snbt.Substring(q1 + 1, q2 - q1 - 1);
	}

	private static string StripNs(string id)
	{
		int i = id.IndexOf(':');
		return i >= 0 ? id[(i + 1)..] : id;
	}
}
