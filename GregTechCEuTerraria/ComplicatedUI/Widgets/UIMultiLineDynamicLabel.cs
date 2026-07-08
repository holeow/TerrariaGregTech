#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Chat;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIMultiLineDynamicLabel : UIElement
{
	private readonly Func<IReadOnlyList<string>> _getter;
	private readonly float _scale;
	private readonly float _lineHeight;
	private readonly Scrollbar _bar = new();
	private int _scroll;

	public UIMultiLineDynamicLabel(Func<IReadOnlyList<string>> getter, float scale = 0.85f,
		float lineHeight = 16f, int width = 300, int height = 200)
	{
		_getter = getter;
		_scale  = scale;
		_lineHeight = lineHeight;
		Width  = StyleDimension.FromPixels(width);
		Height = StyleDimension.FromPixels(height);
	}

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);
		if (IsMouseHovering)
			Scrollbar.Wheel(evt, ref _scroll);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var b = GetDimensions();
		var font = FontAssets.MouseText.Value;
		var lines = _getter();

		int barW = Scrollbar.Width;
		int viewH = (int)b.Height;
		int maxWidth = Math.Max(40, (int)((b.Width - barW - 2) / _scale));

		var rows = new List<List<TextSnippet>?>();
		foreach (var line in lines)
		{
			if (string.IsNullOrEmpty(line)) { rows.Add(null); continue; }
			foreach (var wrapped in Terraria.Utils.WordwrapStringSmart(line, Color.White, font, maxWidth, 20))
				rows.Add(wrapped);
		}

		int contentH = (int)(rows.Count * _lineHeight);
		int maxScroll = Math.Max(0, contentH - viewH);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			PlayerInput.LockVanillaMouseScroll("GregTechCEuTerraria/MultiblockText");
		}

		var mouse = ModalEscape.PollCursor();
		Rectangle track = Rectangle.Empty, thumb = Rectangle.Empty;
		if (maxScroll > 0)
		{
			track = new Rectangle((int)(b.X + b.Width - barW), (int)b.Y, barW, viewH);
			thumb = _bar.Update(track, maxScroll, (float)viewH / contentH, ref _scroll, mouse);
		}
		if (_scroll > maxScroll) _scroll = maxScroll;

		ScissorDraw.Draw(spriteBatch, ScissorDraw.DeviceClip(b.ToRectangle()), () =>
		{
			float bottom = b.Y + b.Height;
			float y = b.Y - _scroll;
			for (int i = 0; i < rows.Count; i++)
			{
				if (rows[i] is { } row && y + _lineHeight >= b.Y && y <= bottom)
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, font, row.ToArray(), new Vector2(b.X, y),
						0f, Vector2.Zero, new Vector2(_scale), out _, -1f);
				y += _lineHeight;
			}
		});

		if (maxScroll > 0)
			_bar.Draw(spriteBatch, track, thumb, mouse);
	}
}
