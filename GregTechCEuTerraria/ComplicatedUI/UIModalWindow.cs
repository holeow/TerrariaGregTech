#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public abstract class UIModalWindow : UIState
{
	public bool ContainsCursor()
	{
		var hit = GetElementAt(ModalEscape.UiCursor);
		return hit != null && hit != this;
	}

	public virtual IEnumerable<Rectangle> OccupiedRects()
	{
		foreach (var e in Elements)
		{
			var r = e.GetDimensions().ToRectangle();
			if (r.Width > 0 && r.Height > 0) yield return r;
		}
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
