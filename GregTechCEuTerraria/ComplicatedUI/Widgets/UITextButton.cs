#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UITextButton : UIElement
{
	private readonly Func<string> _label;
	private readonly Action? _onLeft;
	private readonly Action? _onRight;
	private readonly string? _tooltip;
	private readonly float _textScale;

	public Func<bool>? IsActive { get; set; }

	public Func<bool>? IsDisabled { get; set; }
	public string? DisabledTooltip { get; set; }

	public Func<bool>? IsVisible { get; set; }

	public UITextButton(Func<string> label, Action? onLeft = null, Action? onRight = null,
		string? tooltip = null, int width = 64, int height = 18, float textScale = 0.72f)
	{
		_label = label;
		_onLeft = onLeft;
		_onRight = onRight;
		_tooltip = tooltip;
		_textScale = textScale;
		Width = StyleDimension.FromPixels(width);
		Height = StyleDimension.FromPixels(height);
	}

	public override bool ContainsPoint(Vector2 point)
	{
		if (IsVisible?.Invoke() == false) return false;
		return base.ContainsPoint(point);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		if (IsVisible?.Invoke() == false) return;
		bool disabled = IsDisabled?.Invoke() == true;
		bool active = !disabled && IsActive?.Invoke() == true;

		var bounds = GetDimensions().ToRectangle();
		var px = TextureAssets.MagicPixel.Value;

		Color bg = disabled
			? new Color(28, 28, 32) * 0.85f
			: active
				? new Color(70, 90, 50) * 0.92f
				: new Color(50, 52, 110) * 0.92f;
		Color border = disabled
			? new Color(70, 70, 75)
			: active
				? new Color(230, 220, 80)
				: (IsMouseHovering ? new Color(125, 145, 235) : new Color(89, 116, 213)) * 0.9f;
		sb.Draw(px, bounds, bg);
		sb.Draw(px, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), border);
		sb.Draw(px, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), border);
		sb.Draw(px, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), border);
		sb.Draw(px, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), border);

		string text = _label();
		var font = FontAssets.MouseText.Value;
		float scale = _textScale;
		float textW = ChatManager.GetStringSize(font, text, new Vector2(scale)).X;
		float textH = font.MeasureString(text).Y * scale;
		var pos = new Vector2(
			bounds.X + (bounds.Width - textW) / 2f,
			bounds.Y + (bounds.Height - textH) / 2f);
		Color textColor = disabled ? new Color(140, 140, 145) : Color.White;
		Terraria.Utils.DrawBorderString(sb, text, pos, textColor, scale);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			string? tt = disabled ? (DisabledTooltip ?? _tooltip) : _tooltip;
			if (tt != null) Main.instance.MouseText(tt);
		}
	}

	public override void LeftClick(UIMouseEvent evt)
	{
		base.LeftClick(evt);
		if (IsVisible?.Invoke() == false || IsDisabled?.Invoke() == true) return;
		if (_onLeft is null) return;
		_onLeft();
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	public override void RightClick(UIMouseEvent evt)
	{
		base.RightClick(evt);
		if (IsVisible?.Invoke() == false || IsDisabled?.Invoke() == true) return;
		if (_onRight is null) return;
		_onRight();
		SoundEngine.PlaySound(SoundID.MenuTick);
	}
}
