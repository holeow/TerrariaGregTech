#nullable enable
using System;
using GregTechCEuTerraria.TerrariaCompat.Tiles.NeonSign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.NeonSign;

public sealed class NeonSignColorButton : UIElement
{
	private readonly int _index;
	private readonly Func<int> _selected;
	private readonly Action<int> _onPick;

	public NeonSignColorButton(int index, Func<int> selected, Action<int> onPick)
	{
		_index = index;
		_selected = selected;
		_onPick = onPick;
	}

	public override void LeftClick(UIMouseEvent evt)
	{
		base.LeftClick(evt);
		_onPick(_index);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		var b = GetDimensions().ToRectangle();
		var px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, b, NeonSignPalette.ColorFor(_index));

		bool selected = _selected() == _index;
		Color border = selected ? new Color(255, 235, 140)
			: IsMouseHovering ? Color.White : new Color(0, 0, 0, 130);
		int t = selected ? 2 : 1;
		sb.Draw(px, new Rectangle(b.X, b.Y, b.Width, t), border);
		sb.Draw(px, new Rectangle(b.X, b.Bottom - t, b.Width, t), border);
		sb.Draw(px, new Rectangle(b.X, b.Y, t, b.Height), border);
		sb.Draw(px, new Rectangle(b.Right - t, b.Y, t, b.Height), border);

		if (IsMouseHovering)
			Main.LocalPlayer.mouseInterface = true;
	}
}
