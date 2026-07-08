#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;
using Terraria;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;

public sealed class CraftingStationState
{
	public const int StationSlots = 36;
	public const int StationCols = 9;

	private readonly Item[] _stations = NewSlots();
	private static Item[] NewSlots()
	{
		var a = new Item[StationSlots];
		for (int i = 0; i < a.Length; i++) a[i] = new Item();
		return a;
	}

	public Item[] Stations => _stations;
	public HashSet<int> StationTiles() => RecipeNetworkCrafting.StationTiles(_stations);

	public void CraftToHand(MeNetwork? net, int itemType, int count, Player player)
	{
		if (net is null) return;
		var storage = net.GetStorage();
		var stations = StationTiles();
		var netByType = RecipeNetworkCrafting.NetByType(storage.GetAvailableStacks());

		Terraria.Recipe? chosen = null;
		for (int i = 0; i < Main.recipe.Length; i++)
		{
			var r = Main.recipe[i];
			if (r.createItem.type != itemType || r.createItem.IsAir) continue;
			if (RecipeNetworkCrafting.IsCraftable(r, netByType, stations)) { chosen = r; break; }
		}
		if (chosen is null) return;

		for (int n = 0; n < count; n++)
			if (!RecipeNetworkCrafting.Craft(chosen, storage, stations, player))
				break;
	}

	public void Save(TagCompound tag)
	{
		for (int i = 0; i < StationSlots; i++)
			if (!_stations[i].IsAir) tag[$"st{i}"] = ItemIO.Save(_stations[i]);
	}

	public void Load(TagCompound tag)
	{
		for (int i = 0; i < StationSlots; i++)
			_stations[i] = tag.ContainsKey($"st{i}") ? ItemIO.Load(tag.GetCompound($"st{i}")) : new Item();
	}
}
