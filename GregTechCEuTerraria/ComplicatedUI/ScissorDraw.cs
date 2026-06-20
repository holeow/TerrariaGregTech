#nullable enable
using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class ScissorDraw
{
	private static readonly FieldInfo F_SortMode     = Get("sortMode");
	private static readonly FieldInfo F_Blend        = Get("blendState");
	private static readonly FieldInfo F_Sampler      = Get("samplerState");
	private static readonly FieldInfo F_DepthStencil = Get("depthStencilState");
	private static readonly FieldInfo F_Rasterizer   = Get("rasterizerState");
	private static readonly FieldInfo F_Effect       = Get("customEffect");
	private static readonly FieldInfo F_Matrix       = Get("transformMatrix");

	private static FieldInfo Get(string name) =>
		typeof(SpriteBatch).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException(
			$"SpriteBatch field `{name}` not found - XNA/FNA internals changed");

	public static Rectangle DeviceClip(Rectangle uiRect)
	{
		Vector2 tl = Vector2.Transform(new Vector2(uiRect.X, uiRect.Y), Main.UIScaleMatrix);
		Vector2 br = Vector2.Transform(new Vector2(uiRect.Right, uiRect.Bottom), Main.UIScaleMatrix);
		var r = new Rectangle((int)tl.X, (int)tl.Y, (int)(br.X - tl.X), (int)(br.Y - tl.Y));
		int w = (int)(Main.screenWidth * Main.UIScale);
		int h = (int)(Main.screenHeight * Main.UIScale);
		r.X = Terraria.Utils.Clamp(r.X, 0, w);
		r.Y = Terraria.Utils.Clamp(r.Y, 0, h);
		r.Width = Terraria.Utils.Clamp(r.Width, 0, w - r.X);
		r.Height = Terraria.Utils.Clamp(r.Height, 0, h - r.Y);
		return r;
	}

	public static void Draw(SpriteBatch sb, Rectangle clip, Action body)
	{
		var sortMode     = (SpriteSortMode)F_SortMode.GetValue(sb)!;
		var blendState   = (BlendState)F_Blend.GetValue(sb)!;
		var sampler      = (SamplerState)F_Sampler.GetValue(sb)!;
		var depthStencil = (DepthStencilState)F_DepthStencil.GetValue(sb)!;
		var rasterizer   = (RasterizerState)F_Rasterizer.GetValue(sb)!;
		var effect       = (Effect?)F_Effect.GetValue(sb);
		var matrix       = (Matrix)F_Matrix.GetValue(sb)!;

		GraphicsDevice device = sb.GraphicsDevice;
		Rectangle previous = device.ScissorRectangle;

		sb.End();
		var scissorRasterizer = new RasterizerState { CullMode = CullMode.None, ScissorTestEnable = true };
		device.ScissorRectangle = Rectangle.Intersect(previous, clip);
		sb.Begin(sortMode, blendState, sampler, depthStencil, scissorRasterizer, effect, matrix);
		try
		{
			body();
		}
		finally
		{
			sb.End();
			device.ScissorRectangle = previous;
			sb.Begin(sortMode, blendState, sampler, depthStencil, rasterizer, effect, matrix);
		}
	}
}
