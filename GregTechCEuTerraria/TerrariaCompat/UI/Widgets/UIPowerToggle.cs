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

public sealed class UIPowerToggle : UIElement
{
	private readonly Func<bool> _get;
	private readonly Action<bool> _set;
	private bool _down;

	public Func<bool, string>? TooltipFor;

	private static Asset<Texture2D>? _tex;

	public UIPowerToggle(Func<bool> get, Action<bool> set, int width = 22, int height = 22)
	{
		_get = get;
		_set = set;
		Width = StyleDimension.FromPixels(width);
		Height = StyleDimension.FromPixels(height);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		_tex ??= ModContent.Request<Texture2D>("GregTechCEuTerraria/Content/Textures/gui/widget/button_power");
		if (_tex?.Value is not { } tex) return;
		var bounds = GetDimensions().ToRectangle();
		bool on = _get();
		int halfH = tex.Height / 2;
		var src = new Rectangle(0, on ? halfH : 0, tex.Width, halfH);
		TerrariaCompat.UI.PointClampDraw.Draw(spriteBatch, () =>
			spriteBatch.Draw(tex, bounds, src, Color.White));
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
		Main.instance.MouseText(TooltipFor != null
			? TooltipFor(_get())
			: _get() ? "Working: enabled (click to pause)" : "Working: paused (click to enable)");
	}
}
