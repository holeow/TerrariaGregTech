#nullable enable
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class ModalFocusSystem : ModSystem
{
	public override void UpdateUI(GameTime gameTime)
	{
		if (Main.dedServ) return;

		if (!MouseClick.LeftPressed && !MouseClick.RightPressed) return;
		if (UILayers.PushedThisFrame) return;

		if (UILayers.TopmostModalAtCursor() is { } target)
			UILayers.Push(target);
	}
}
