#nullable enable
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.Api.Fluids;
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Items;

namespace GregTechCEuTerraria.TerrariaCompat.Recipes;

public sealed class IngredientResolverImpl : IIngredientResolver
{
	public static readonly IngredientResolverImpl Instance = new();

	public int ResolveItemType(string upstreamId)
	{
		if (string.IsNullOrEmpty(upstreamId)) return 0;

		if (upstreamId.StartsWith("terraria:", System.StringComparison.Ordinal))
			return Terraria.ID.ItemID.Search.TryGetId(upstreamId["terraria:".Length..], out var tid) ? tid : 0;

		if (upstreamId.IndexOf(':') < 0)
		{
			int slash = upstreamId.IndexOf('/');
			if (slash > 0 && Terraria.ModLoader.ModContent.TryFind<Terraria.ModLoader.ModItem>(
				    upstreamId[..slash], upstreamId[(slash + 1)..], out var mi))
				return mi.Type;
		}

		if (VanillaItemMap.TryGet(upstreamId, out var v)) return v;

		if (Items.Registry.RegistryItemLoader.TryGet(upstreamId, out var reg)) return reg;
		if (Items.Tools.ToolItemLoader.TryGet(upstreamId, out var tool)) return tool;
		if (Items.Armor.ArmorItemLoader.TryGet(upstreamId, out var armor)) return armor;

		if (upstreamId == "gtceu:treated_wood_plate" &&
		    Terraria.ModLoader.ModContent.TryFind<Terraria.ModLoader.ModItem>(
			    "GregTechCEuTerraria", "treated_wood_planks", out var twp))
			return twp.Type;

		if (upstreamId == "gtceu:programmed_circuit")
			return Terraria.ModLoader.ModContent.ItemType<IntCircuitItem>();
		if (TryStripGtceuPrefix(upstreamId, out var bare) &&
		    Items.Fluids.FluidCellRegistry.TryGet(bare, out var cell))
			return cell;

		if (MaterialItemRegistry.TryGetByUpstreamId(upstreamId, out var matItem))
			return matItem;

		if (TryStripGtceuPrefix(upstreamId, out var modBare) &&
		    Terraria.ModLoader.ModContent.TryFind<Terraria.ModLoader.ModItem>("GregTechCEuTerraria", modBare, out var custom))
			return custom.Type;

		return 0;
	}

	public static string StableItemId(int type)
	{
		if (type <= 0) return "";
		if (Terraria.ModLoader.ItemLoader.GetItem(type) is { } modItem)
			return modItem.FullName;
		return Terraria.ID.ItemID.Search.TryGetName(type, out var name) ? "terraria:" + name : "";
	}

	string IIngredientResolver.StableItemId(int itemType) => StableItemId(itemType);

	public static string StableTileId(int tileType)
	{
		if (tileType < 0) return "";
		if (Terraria.ModLoader.TileLoader.GetTile(tileType) is { } modTile)
			return modTile.FullName;
		return Terraria.ID.TileID.Search.TryGetName(tileType, out var name) ? "terraria:" + name : "";
	}

	public static int ResolveTileType(string stableId)
	{
		if (string.IsNullOrEmpty(stableId)) return -1;

		if (stableId.StartsWith("terraria:", System.StringComparison.Ordinal))
			return Terraria.ID.TileID.Search.TryGetId(stableId["terraria:".Length..], out var tid) ? tid : -1;

		int slash = stableId.IndexOf('/');
		if (slash > 0 && Terraria.ModLoader.ModContent.TryFind<Terraria.ModLoader.ModTile>(
			    stableId[..slash], stableId[(slash + 1)..], out var mt))
			return mt.Type;

		return -1;
	}

	int IIngredientResolver.ResolveTileType(string stableId) => ResolveTileType(stableId);

	string IIngredientResolver.StableTileId(int tileType) => StableTileId(tileType);

	public IReadOnlyList<int> ResolveItemTag(string tagName)
	{
		if (string.IsNullOrEmpty(tagName)) return Array.Empty<int>();

		if (Items.Tools.ToolItemLoader.CraftingTagItems.TryGetValue(tagName, out var catalystItems))
			return catalystItems;

		if (TryResolveRecipeGroupTag(tagName, out var groupItems))
			return groupItems;

		if (VanillaItemMap.TryGetFungibleGroupView(tagName, out var groupView))
			return groupView;

		var types = new List<int>();

		if (VanillaItemMap.TryGetTagItem(tagName, out var vt))
			types.Add(vt);
		bool exclusive = false;
		if (VanillaItemMap.TryGetTagItems(tagName, out var multi))
		{
			foreach (var t in multi)
				if (t > 0 && !types.Contains(t)) types.Add(t);
			exclusive = true;
		}

		if (!exclusive && Items.Registry.RegistryTagLoader.HasTag(tagName))
		{
			foreach (var memberId in Items.Registry.RegistryTagLoader.ExpandItems(tagName))
			{
				int t = ResolveItemType(memberId);
				if (t > 0 && !types.Contains(t)) types.Add(t);
			}
		}

		if (types.Count == 0 && MaterialItemRegistry.TryGetByTagPath(tagName, out var matItem))
			types.Add(matItem);

		return types.Count > 0 ? types : (IReadOnlyList<int>)Array.Empty<int>();
	}

	public const string RecipeGroupTagPrefix = "$terraria:group/";

	public static string RecipeGroupTag(int groupId) => RecipeGroupTagPrefix + groupId;

	public static bool TryGetRecipeGroupId(string tagName, out int groupId)
	{
		groupId = 0;
		if (string.IsNullOrEmpty(tagName)) return false;
		if (!tagName.StartsWith(RecipeGroupTagPrefix, StringComparison.Ordinal)) return false;
		return int.TryParse(tagName[RecipeGroupTagPrefix.Length..], out groupId);
	}

	public static bool TryGetRecipeGroup(string tagName, out Terraria.RecipeGroup group)
	{
		group = null!;
		return TryGetRecipeGroupId(tagName, out int gid)
			&& Terraria.RecipeGroup.recipeGroups.TryGetValue(gid, out group!);
	}

	private static bool TryResolveRecipeGroupTag(string tagName, out IReadOnlyList<int> items)
	{
		items = Array.Empty<int>();
		if (!TryGetRecipeGroupId(tagName, out int gid)) return false;
		if (!Terraria.RecipeGroup.recipeGroups.ContainsKey(gid)) return false;
		items = VanillaItemMap.GroupItemView(gid);
		return items.Count > 0;
	}

	public FluidType? ResolveFluidType(string upstreamId)
	{
		if (string.IsNullOrEmpty(upstreamId)) return null;
		int colon = upstreamId.IndexOf(':');
		string id = colon >= 0 ? upstreamId[(colon + 1)..] : upstreamId;
		return Api.Fluids.FluidRegistry.TryGet(id, out var f) ? f : null;
	}

	public IReadOnlyList<FluidType> ResolveFluidTag(string tagName)
	{
		var t = ResolveFluidType(tagName);
		return t is null ? Array.Empty<FluidType>() : new[] { t };
	}

	private static bool TryStripGtceuPrefix(string upstreamId, out string bareId)
	{
		const string prefix = "gtceu:";
		if (upstreamId.StartsWith(prefix, StringComparison.Ordinal))
		{
			bareId = upstreamId[prefix.Length..];
			return true;
		}
		bareId = upstreamId;
		return false;
	}
}
