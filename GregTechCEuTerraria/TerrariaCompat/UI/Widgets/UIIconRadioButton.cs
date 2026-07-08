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

public sealed class UIIconRadioButton : UIElement
{
	private readonly int[] _icons;
	private readonly Func<bool> _isActive;
	private readonly Action _onClick;
	private readonly string? _tooltip;

	public UIIconRadioButton(int[] icons, Func<bool> isActive, Action onClick, string? tooltip,
		int width, int height)
	{
		_icons = icons;
		_isActive = isActive;
		_onClick = onClick;
		_tooltip = tooltip;
		Width = StyleDimension.FromPixels(width);
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
		bool active = _isActive();
		var bounds = GetDimensions().ToRectangle();
		var px = TextureAssets.MagicPixel.Value;

		Color bg = active ? new Color(70, 90, 50) * 0.92f : new Color(50, 52, 110) * 0.92f;
		Color border = active
			? new Color(230, 220, 80)
			: (IsMouseHovering ? new Color(125, 145, 235) : new Color(89, 116, 213)) * 0.9f;
		sb.Draw(px, bounds, bg);
		sb.Draw(px, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), border);
		sb.Draw(px, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), border);
		sb.Draw(px, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), border);
		sb.Draw(px, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), border);

		int n = _icons.Length;
		int cell = (bounds.Width - 4) / Math.Max(1, n);
		for (int i = 0; i < n; i++)
		{
			var dest = new Rectangle(bounds.X + 2 + i * cell, bounds.Y + 1, cell, bounds.Height - 2);
			DrawIcon(sb, _icons[i], dest);
		}

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			if (_tooltip != null) Main.instance.MouseText(_tooltip);
		}
	}

	private static void DrawIcon(SpriteBatch sb, int itemType, Rectangle dest)
	{
		if (itemType <= 0) return;
		Main.instance.LoadItem(itemType);
		var tex = TextureAssets.Item[itemType]?.Value;
		if (tex is null) return;

		Rectangle src = Main.itemAnimations[itemType] != null
			? Main.itemAnimations[itemType].GetFrame(tex, 0)
			: tex.Bounds;

		float scale = Math.Min((float)dest.Width / src.Width, (float)dest.Height / src.Height);
		if (scale > 1f) scale = 1f;
		var size = new Vector2(src.Width, src.Height) * scale;
		var pos = new Vector2(
			dest.X + (dest.Width - size.X) / 2f,
			dest.Y + (dest.Height - size.Y) / 2f);
		sb.Draw(tex, pos, src, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}
