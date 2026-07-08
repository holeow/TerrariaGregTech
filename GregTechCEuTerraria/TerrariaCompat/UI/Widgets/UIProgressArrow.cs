#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIProgressArrow : UIElement
{
	private readonly Func<float> _progress;
	private readonly string _assetPath;
	private Asset<Texture2D>? _texture;

	private const int FrameWidth = 20;
	private const int FrameHeight = 20;

	public Action? OnClickAction { get; set; }
	public string? Tooltip { get; set; }

	public UIProgressArrow(Func<float> progress, string assetPath = "GregTechCEuTerraria/Content/Textures/gui/progress_bar/progress_bar_arrow")
	{
		_progress = progress;
		_assetPath = assetPath;
		Width = StyleDimension.FromPixels(FrameWidth);
		Height = StyleDimension.FromPixels(FrameHeight);
	}

	public override void LeftClick(UIMouseEvent evt)
	{
		base.LeftClick(evt);
		OnClickAction?.Invoke();
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		_texture ??= ModContent.Request<Texture2D>(_assetPath);
		if (_texture?.Value == null) return;

		var bounds = GetDimensions().ToRectangle();
		var tex = _texture.Value;
		float p = Math.Clamp(_progress(), 0f, 1f);

		if (IsMouseHovering && !string.IsNullOrEmpty(Tooltip))
			Main.instance.MouseText(Tooltip);

		PointClampDraw.Draw(spriteBatch, () =>
		{
			var emptySrc = new Rectangle(0, 0, FrameWidth, FrameHeight);
			spriteBatch.Draw(tex, bounds, emptySrc, Color.White);
			if (p > 0f)
			{
				int scale = Math.Max(1, bounds.Width / FrameWidth);
				int srcW  = (int)(FrameWidth * p);
				if (srcW > 0)
				{
					var filledSrc = new Rectangle(0, FrameHeight, srcW, FrameHeight);
					var filledDst = new Rectangle(bounds.X, bounds.Y, srcW * scale, bounds.Height);
					spriteBatch.Draw(tex, filledDst, filledSrc, Color.White);
				}
			}
		});
	}
}
