#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIOverlayToggle : UIElement
{
	private readonly string _iconPath;
	private readonly Func<bool> _get;
	private readonly Action<bool> _set;
	private bool _down;

	public Func<bool, string>? TooltipFor;

	private static Asset<Texture2D>? _bg;
	private Asset<Texture2D>? _icon;

	public UIOverlayToggle(string iconPath, Func<bool> get, Action<bool> set, int width = 22, int height = 22)
	{
		_iconPath = iconPath;
		_get = get;
		_set = set;
		Width = StyleDimension.FromPixels(width);
		Height = StyleDimension.FromPixels(height);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		_bg   ??= ModContent.Request<Texture2D>("GregTechCEuTerraria/Content/Textures/gui/widget/toggle_button_background");
		_icon ??= ModContent.Request<Texture2D>(_iconPath);
		var bounds = GetDimensions().ToRectangle();
		bool on = _get();
		TerrariaCompat.UI.PointClampDraw.Draw(spriteBatch, () =>
		{
			if (_bg?.Value is { } bg)
			{
				int halfH = bg.Height / 2;
				var src = new Rectangle(0, on ? halfH : 0, bg.Width, halfH);
				spriteBatch.Draw(bg, bounds, src, Color.White);
			}
			if (_icon?.Value is { } icon)
			{
				int inset = System.Math.Max(2, bounds.Width / 8);
				int size = System.Math.Min(bounds.Width, bounds.Height) - inset * 2;
				var dst = new Rectangle(
					bounds.X + (bounds.Width - size) / 2,
					bounds.Y + (bounds.Height - size) / 2,
					size, size);
				spriteBatch.Draw(icon, dst, on ? Color.White : new Color(170, 170, 170));
			}
		});
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		_down = true;
	}

	public override void LeftMouseUp(UIMouseEvent evt)
	{
		base.LeftMouseUp(evt);
		if (!_down) return;
		_down = false;
		if (!IsMouseHovering) return;
		_set(!_get());
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (!IsMouseHovering) return;
		Main.LocalPlayer.mouseInterface = true;
		if (TooltipFor != null) Main.instance.MouseText(TooltipFor(_get()));
	}
}
