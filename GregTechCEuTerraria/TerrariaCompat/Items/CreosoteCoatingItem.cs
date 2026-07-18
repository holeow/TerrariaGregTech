using System;
using System.Collections.Generic;
using System.Text;

namespace GregTechCEuTerraria.TerrariaCompat.Items
{
	public sealed class CreosoteCoatingItem : ModItem
	{

		public override string Name => "creosote_coating";
		public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/creosote_coating";

		public override void SetStaticDefaults()
		=> Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName",
			() => "Creosote Coating");

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.maxStack = 9999;
			Item.rare = ItemRarityID.Green;
		}


	}
}
