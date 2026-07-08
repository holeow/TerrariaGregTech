#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIToggleButton : UIElement
{
	private readonly string _iconAssetPath;
	private readonly Func<bool> _getter;
	private readonly Action<bool> _setter;
	private readonly string _tooltip;
	private Asset<Texture2D>? _icon;

	public Func<bool, Rectangle>? IconSrcRectFor { get; set; }
	public Func<bool, string>? TooltipFor { get; set; }

	public UIToggleButton(string iconAssetPath, Func<bool> getter, Action<bool> setter, string tooltip)
	{
		_iconAssetPath = iconAssetPath;
		_getter = getter;
		_setter = setter;
		_tooltip = tooltip;
		Width = StyleDimension.FromPixels(18);
		Height = StyleDimension.FromPixels(18);
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		_setter(!_getter());
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		_icon ??= ModContent.Request<Texture2D>(_iconAssetPath);

		var bounds = GetDimensions().ToRectangle();
		var px = TextureAssets.MagicPixel.Value;
		bool on = _getter();

		Color bg = on
			? new Color(70, 90, 50) * 0.85f
			: new Color(20, 20, 30) * 0.85f;
		if (IsMouseHovering) bg = Color.Lerp(bg, Color.White, 0.15f);
		spriteBatch.Draw(px, bounds, bg);

		if (_icon?.Value != null)
		{
			var t = _icon.Value;
			int inset = System.Math.Max(2, bounds.Width / 9);
			int iconSize = System.Math.Min(bounds.Width, bounds.Height) - inset * 2;
			var iconRect = new Rectangle(
				bounds.X + (bounds.Width - iconSize) / 2,
				bounds.Y + (bounds.Height - iconSize) / 2,
				iconSize, iconSize);
			Color iconTint = on ? Color.White : new Color(170, 170, 170);
			Rectangle? src = IconSrcRectFor?.Invoke(on);
			TerrariaCompat.UI.PointClampDraw.Draw(spriteBatch, () =>
			{
				if (src.HasValue) spriteBatch.Draw(t, iconRect, src.Value, iconTint);
				else              spriteBatch.Draw(t, iconRect, iconTint);
			});
		}

		var border = on ? new Color(230, 220, 80) : Color.White;
		spriteBatch.Draw(px, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), border);
		spriteBatch.Draw(px, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), border);
		spriteBatch.Draw(px, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), border);
		spriteBatch.Draw(px, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), border);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			string tt = TooltipFor?.Invoke(on) ?? _tooltip;
			if (!string.IsNullOrEmpty(tt))
			{
				Main.instance.MouseText(tt);
				Main.LocalPlayer.cursorItemIconEnabled = false;
			}
		}
	}
}
