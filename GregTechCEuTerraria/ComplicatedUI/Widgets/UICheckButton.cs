#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UICheckButton : UIElement
{
	private readonly Func<string> _label;
	private readonly Func<bool> _checked;
	private readonly Action _onClick;
	private readonly string? _tooltip;

	public UICheckButton(Func<string> label, Func<bool> isChecked, Action onClick,
		string? tooltip = null, int height = 26)
	{
		_label   = label;
		_checked = isChecked;
		_onClick = onClick;
		_tooltip = tooltip;
		Width  = StyleDimension.FromPixels(120);
		Height = StyleDimension.FromPixels(height);
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		_onClick();
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		bool on = _checked();
		var bounds = GetDimensions().ToRectangle();
		var px = TextureAssets.MagicPixel.Value;

		sb.Draw(px, bounds, new Color(50, 52, 110) * (IsMouseHovering ? 0.55f : 0.32f));

		var tick = (on ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff).Value;
		var tickPos = new Vector2(bounds.X + 4, bounds.Y + (bounds.Height - tick.Height) / 2f);
		sb.Draw(tick, tickPos, Color.White);

		string text = _label();
		var font = FontAssets.MouseText.Value;
		const float scale = 0.72f;
		float ty = bounds.Y + (bounds.Height - font.MeasureString(text).Y * scale) / 2f;
		Terraria.Utils.DrawBorderString(sb, text,
			new Vector2(tickPos.X + tick.Width + 5f, ty),
			on ? Color.White : new Color(195, 198, 205), scale);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			if (_tooltip != null) Main.instance.MouseText(_tooltip);
		}
	}
}
