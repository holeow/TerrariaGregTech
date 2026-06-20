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

public sealed class UIIconButton : UIElement
{
	private readonly int _itemType;
	private readonly Action _onClick;
	private readonly Func<bool> _isActive;
	private readonly string _tooltip;

	public UIIconButton(int itemType, Action onClick, Func<bool> isActive, string tooltip, int size)
	{
		_itemType = itemType;
		_onClick  = onClick;
		_isActive = isActive;
		_tooltip  = tooltip;
		Width  = StyleDimension.FromPixels(size);
		Height = StyleDimension.FromPixels(size);
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

		DrawIcon(sb, bounds, active);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.instance.MouseText(_tooltip);
		}
	}

	private void DrawIcon(SpriteBatch sb, Rectangle bounds, bool active)
	{
		if (_itemType <= 0 || _itemType >= TextureAssets.Item.Length) return;
		Main.instance.LoadItem(_itemType);
		var tex = TextureAssets.Item[_itemType]?.Value;
		if (tex is null) return;

		Rectangle frame = Main.itemAnimations[_itemType] != null
			? Main.itemAnimations[_itemType].GetFrame(tex)
			: tex.Frame();

		float avail = bounds.Width - 8f;
		float scale = Math.Min(avail / frame.Width, avail / frame.Height);
		if (scale > 1f) scale = 1f;

		var origin = new Vector2(frame.Width / 2f, frame.Height / 2f);
		var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
		Color tint = active ? Color.White : Color.White * 0.78f;
		sb.Draw(tex, center, frame, tint, 0f, origin, scale, SpriteEffects.None, 0f);
	}
}
