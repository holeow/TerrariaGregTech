#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Items;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Players;

public sealed class StartingInventoryPlayer : ModPlayer
{
	public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
	{
		var tools = new Item();
		tools.SetDefaults(ModContent.ItemType<GregTechIronToolsBag>());
		yield return tools;

		var bag = new Item();
		bag.SetDefaults(ModContent.ItemType<SteamAgeSkipBag>());
		yield return bag;

		var stoneBag = new Item();
		stoneBag.SetDefaults(ModContent.ItemType<SkipStoneAgeBag>());
		yield return stoneBag;

		var storageBag = new Item();
		storageBag.SetDefaults(ModContent.ItemType<SuperStorageSystemBag>());
		yield return storageBag;
	}
}
