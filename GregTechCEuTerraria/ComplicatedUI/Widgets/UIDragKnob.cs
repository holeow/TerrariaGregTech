#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIDragKnob : UIElement
{
	public enum Kind { Move, Resize }

	private readonly System.Action _onPress;
	private readonly Kind _kind;
	private readonly string _tooltip;

	public UIDragKnob(System.Action onPress, Kind kind, string tooltip)
	{
		_onPress = onPress;
		_kind    = kind;
		_tooltip = tooltip;
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		_onPress();
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		var px = TextureAssets.MagicPixel.Value;
		Rectangle r = GetDimensions().ToRectangle();
		bool hot = IsMouseHovering;

		var grip   = hot ? Color.White : new Color(190, 205, 235);
		var shadow = new Color(0, 0, 0, 150);
		void Dot(int x, int y, int s)
		{
			sb.Draw(px, new Rectangle(x + 1, y + 1, s, s), shadow);
			sb.Draw(px, new Rectangle(x, y, s, s), grip);
		}

		if (_kind == Kind.Resize)
		{
			int step = System.Math.Max(4, r.Width / 5);
			int s = System.Math.Max(2, r.Width / 8);
			for (int i = 0; i < 3; i++)
			{
				int o = 3 + i * step;
				Dot(r.Right - o - s, r.Bottom - 3 - s, s);
				Dot(r.Right - 3 - s, r.Bottom - o - s, s);
			}
		}
		else
		{
			int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
			Dot(cx - 5, cy - 2, 3); Dot(cx + 2, cy - 2, 3);
			Dot(cx - 2, cy - 5, 3); Dot(cx - 2, cy + 2, 3);
		}

		if (hot)
		{
			Main.instance.MouseText(_tooltip);
			Main.LocalPlayer.mouseInterface = true;
		}
	}
}
