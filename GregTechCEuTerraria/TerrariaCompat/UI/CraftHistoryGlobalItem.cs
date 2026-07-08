#nullable enable
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class CraftHistoryGlobalItem : GlobalItem
{
	public override void OnCreated(Item item, ItemCreationContext context)
	{
		if (Main.dedServ) return;
		if (context is RecipeItemCreationContext && item is not null && item.type > ItemID.None)
			FavoritesPlayer.Local.RecordItem(item.type);
	}
}
