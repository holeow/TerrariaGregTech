#nullable enable
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class ItemDragRenderer : ModSystem
{
	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		int at = idx >= 0 ? idx + 1 : layers.Count;
		layers.Insert(at, new LegacyGameInterfaceLayer(
			"GregTechCEuTerraria: Item Drag",
			() => { ItemDrag.Draw(Main.spriteBatch); return true; },
			InterfaceScaleType.UI));
	}
}
