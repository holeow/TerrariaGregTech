#nullable enable
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public abstract class UIModalWindow : UIState
{
	public bool ContainsCursor()
	{
		var hit = GetElementAt(Main.MouseScreen);
		return hit != null && hit != this;
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		if (ContainsCursor())
		{
			Main.HoverItem = new Item();
			Main.hoverItemName = "";
			Main.mouseText = false;
		}
		base.Draw(spriteBatch);
	}
}
