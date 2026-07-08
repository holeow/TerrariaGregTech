#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public static class RecipeBrowserPanel
{
	public static IReadOnlyList<GTRecipe> FilterAndSort(IReadOnlyList<GTRecipe> all, string[] tokens)
	{
		if (tokens.Length == 0) return all;

		var filtered = new List<GTRecipe>();
		foreach (var r in all)
			if (RecipeSearch.Matches(r, tokens)) filtered.Add(r);

		if (filtered.Count > 1)
		{
			var ranks = new int[filtered.Count];
			for (int i = 0; i < filtered.Count; i++)
				ranks[i] = RecipeSearch.MatchesOutputs(filtered[i], tokens) ? 0 : 1;
			for (int i = 1; i < filtered.Count; i++)
			{
				var r = filtered[i];
				int rank = ranks[i];
				int j = i - 1;
				while (j >= 0 && ranks[j] > rank)
				{
					filtered[j + 1] = filtered[j];
					ranks[j + 1] = ranks[j];
					j--;
				}
				filtered[j + 1] = r;
				ranks[j + 1] = rank;
			}
		}
		return filtered;
	}

	public static UITerrariaPanel BuildSearchPanel(float w, float h,
		Func<string> countLabel, UIRecipeList list, string searchPlaceholder, Action<string> onSearchChanged)
	{
		var panel = new UITerrariaPanel
		{
			Width = StyleDimension.FromPixels(w),
			Height = StyleDimension.FromPixels(h),
		};

		const int SearchH   = 22;
		const int CountW     = 110;
		const int HeaderPad  = 6;

		var search = new UISearchBar(searchPlaceholder, onSearchChanged)
		{
			Left = StyleDimension.FromPixels(HeaderPad),
			Top  = StyleDimension.FromPixels(HeaderPad),
			Width = StyleDimension.FromPixels(w - HeaderPad * 2 - CountW - 6),
			Height = StyleDimension.FromPixels(SearchH),
		};
		panel.Append(search);
		list.OnStationFilter = s => search.SetText("@" + s);

		var count = new UIDynamicLabel(countLabel, 0.75f)
		{
			HAlign = 1f,
			Left   = StyleDimension.FromPixels(-HeaderPad),
			Top    = StyleDimension.FromPixels(HeaderPad + 3),
			Width  = StyleDimension.FromPixels(CountW),
			Height = StyleDimension.FromPixels(SearchH),
		};
		panel.Append(count);

		list.Left = StyleDimension.FromPixels(4);
		list.Top  = StyleDimension.FromPixels(HeaderPad + SearchH + 4);
		list.Width  = StyleDimension.FromPixels(w - 8);
		list.Height = StyleDimension.FromPixels(h - (HeaderPad + SearchH + 8));
		panel.Append(list);
		return panel;
	}

	public static UITerrariaPanel BuildRelevantPanel(float w, float h, string title, UIRecipeList list)
	{
		var panel = new UITerrariaPanel
		{
			Width = StyleDimension.FromPixels(w),
			Height = StyleDimension.FromPixels(h),
		};

		var titleText = new UIText(title, 0.85f)
		{
			Left = StyleDimension.FromPixels(8),
			Top = StyleDimension.FromPixels(6),
		};
		panel.Append(titleText);

		list.Left = StyleDimension.FromPixels(4);
		list.Top = StyleDimension.FromPixels(28);
		list.Width = StyleDimension.FromPixels(w - 8);
		list.Height = StyleDimension.FromPixels(h - 32);
		panel.Append(list);
		return panel;
	}
}
