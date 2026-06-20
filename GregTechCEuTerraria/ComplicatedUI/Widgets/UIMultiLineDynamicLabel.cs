#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIMultiLineDynamicLabel : UIElement
{
	private readonly Func<IReadOnlyList<string>> _getter;
	private readonly float _scale;
	private readonly float _lineHeight;

	public UIMultiLineDynamicLabel(Func<IReadOnlyList<string>> getter, float scale = 0.85f, float lineHeight = 16f)
	{
		_getter = getter;
		_scale  = scale;
		_lineHeight = lineHeight;
		Width  = StyleDimension.FromPixels(300);
		Height = StyleDimension.FromPixels(200);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var b = GetDimensions();
		var font = FontAssets.MouseText.Value;
		var lines = _getter();
		float y = b.Y;
		int maxWidth = Math.Max(40, (int)(b.Width / _scale));
		for (int i = 0; i < lines.Count; i++)
		{
			string line = lines[i];
			if (string.IsNullOrEmpty(line))
			{
				y += _lineHeight;
				continue;
			}
			var wrapped = Terraria.Utils.WordwrapStringSmart(line, Color.White, font, maxWidth, 20);
			foreach (var snippetLine in wrapped)
			{
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, snippetLine.ToArray(), new Vector2(b.X, y),
					0f, Vector2.Zero, new Vector2(_scale), out _, -1f);
				y += _lineHeight;
			}
		}
	}
}
