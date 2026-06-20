#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Items.Tools;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Recipes;

public static class ToolRecipeGroups
{
	public static void Register()
	{
		foreach (var (tag, ids) in ToolItemLoader.CraftingTagItems)
		{
			if (ids.Count == 0) continue;

			string bare = tag[(tag.LastIndexOf('/') + 1)..].Replace("crafting_", "");
			string label = "Any " + bare.TrimEnd('s').Replace('_', ' ');

			int groupId = RecipeGroup.RegisterGroup(
				$"GregTechCEuTerraria:{tag}",
				new RecipeGroup(() => label, ids.ToArray()));

			VanillaItemMap.RegisterGroup(tag, groupId);
		}
	}
}
