#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public class UITerrariaPanel : UIPanel
{
	public UITerrariaPanel()
	{
		BackgroundColor = new Color(63, 65, 151) * 0.785f;
		BorderColor = new Color(89, 116, 213) * 0.9f;
		PaddingLeft = PaddingRight = PaddingTop = PaddingBottom = 0f;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		if (ContainsPoint(ModalEscape.PollCursorScreen()))
			Main.LocalPlayer.mouseInterface = true;
	}
}
