#nullable enable
using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class PointClampDraw
{
	private static readonly FieldInfo F_SortMode    = Get("sortMode");
	private static readonly FieldInfo F_Blend       = Get("blendState");
	private static readonly FieldInfo F_Sampler     = Get("samplerState");
	private static readonly FieldInfo F_DepthStencil= Get("depthStencilState");
	private static readonly FieldInfo F_Rasterizer  = Get("rasterizerState");
	private static readonly FieldInfo F_Effect      = Get("customEffect");
	private static readonly FieldInfo F_Matrix      = Get("transformMatrix");

	private static FieldInfo Get(string name) =>
		typeof(SpriteBatch).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException(
			$"SpriteBatch field `{name}` not found - XNA/FNA internals changed");

	public static void Draw(SpriteBatch sb, Action body)
	{
		var sortMode     = (SpriteSortMode)F_SortMode.GetValue(sb)!;
		var blendState   = (BlendState)F_Blend.GetValue(sb)!;
		var sampler      = (SamplerState)F_Sampler.GetValue(sb)!;
		var depthStencil = (DepthStencilState)F_DepthStencil.GetValue(sb)!;
		var rasterizer   = (RasterizerState)F_Rasterizer.GetValue(sb)!;
		var effect       = (Effect?)F_Effect.GetValue(sb);
		var matrix       = (Matrix)F_Matrix.GetValue(sb)!;

		sb.End();
		sb.Begin(sortMode, blendState, SamplerState.PointClamp,
			depthStencil, rasterizer, effect, matrix);
		try { body(); }
		finally
		{
			sb.End();
			sb.Begin(sortMode, blendState, sampler,
				depthStencil, rasterizer, effect, matrix);
		}
	}

	public static void Draw(SpriteBatch sb, Matrix _unused, Action body) => Draw(sb, body);

	public static void DrawInScreenSpace(SpriteBatch sb, Action body)
	{
		var sortMode     = (SpriteSortMode)F_SortMode.GetValue(sb)!;
		var blendState   = (BlendState)F_Blend.GetValue(sb)!;
		var sampler      = (SamplerState)F_Sampler.GetValue(sb)!;
		var depthStencil = (DepthStencilState)F_DepthStencil.GetValue(sb)!;
		var rasterizer   = (RasterizerState)F_Rasterizer.GetValue(sb)!;
		var effect       = (Effect?)F_Effect.GetValue(sb);
		var matrix       = (Matrix)F_Matrix.GetValue(sb)!;

		sb.End();
		sb.Begin(SpriteSortMode.Deferred, blendState, SamplerState.PointClamp,
			depthStencil, rasterizer, null, Matrix.Identity);
		try { body(); }
		finally
		{
			sb.End();
			sb.Begin(sortMode, blendState, sampler, depthStencil, rasterizer, effect, matrix);
		}
	}

	public static void DrawWithEffect(SpriteBatch sb, Effect effect, Action body)
	{
		var sortMode     = (SpriteSortMode)F_SortMode.GetValue(sb)!;
		var blendState   = (BlendState)F_Blend.GetValue(sb)!;
		var sampler      = (SamplerState)F_Sampler.GetValue(sb)!;
		var depthStencil = (DepthStencilState)F_DepthStencil.GetValue(sb)!;
		var rasterizer   = (RasterizerState)F_Rasterizer.GetValue(sb)!;
		var effect0      = (Effect?)F_Effect.GetValue(sb);
		var matrix       = (Matrix)F_Matrix.GetValue(sb)!;

		sb.End();
		sb.Begin(SpriteSortMode.Immediate, blendState, SamplerState.PointClamp,
			depthStencil, rasterizer, null, matrix);
		effect.CurrentTechnique.Passes[0].Apply();
		try { body(); }
		finally
		{
			sb.End();
			sb.Begin(sortMode, blendState, sampler, depthStencil, rasterizer, effect0, matrix);
		}
	}
}
