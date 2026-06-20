#nullable enable
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

// click to get modal to front
public sealed class ModalFocusSystem : ModSystem
{
	private bool _prevLeft, _prevRight;

	public override void UpdateUI(GameTime gameTime)
	{
		if (Main.dedServ) return;

		bool press = (Main.mouseLeft && !_prevLeft) || (Main.mouseRight && !_prevRight);
		_prevLeft  = Main.mouseLeft;
		_prevRight = Main.mouseRight;
		if (!press) return;

		if (UILayers.TopmostModalAtCursor() is { } target)
			UILayers.Push(target);
	}
}
