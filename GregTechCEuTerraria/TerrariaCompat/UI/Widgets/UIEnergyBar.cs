#nullable enable
using System;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.Common.Energy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIEnergyBar : UIElement
{
	private const int MaxBarHeight = 80;

	private readonly TieredEnergyMachine _container;

	public UIEnergyBar(TieredEnergyMachine container, int width, int height)
	{
		_container = container;
		Width = StyleDimension.FromPixels(width);
		Height = StyleDimension.FromPixels(Math.Min(height, MaxBarHeight));
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var bounds = GetDimensions().ToRectangle();

		long stored = _container.EnergyStored;
		long cap = _container.EnergyCapacity;
		float fill = cap > 0 ? Math.Clamp((float)stored / cap, 0f, 1f) : 0f;
		var color = VoltageTiers.TextColor(_container.Tier);
		float s = Main.UIScale > 0f ? Main.UIScale : 1f;

		int numberBand = Snap(14 * s);
		PointClampDraw.DrawInScreenSpace(spriteBatch, () =>
			DrawBar(spriteBatch, bounds, color, fill, s, numberBand));

		DrawNumber(spriteBatch, bounds, stored, color);

		if (IsMouseHovering)
		{
			var ec = _container.EnergyContainer;
			var tier = _container.Tier;
			string text = $"{stored:N0} / {cap:N0} EU"
			            + $"\n{VoltageTiers.ShortName(tier)} - {VoltageTiers.Voltage(tier):N0} EU/t";
			if (ec.InputVoltage > 0)
				text += $"\nIn: {ec.InputAmperage}A @ {ec.InputVoltage:N0} EU/t";
			if (ec.OutputVoltage > 0)
				text += $"\nOut: {ec.OutputAmperage}A @ {ec.OutputVoltage:N0} EU/t";
			Main.instance.MouseText(text);
			Main.LocalPlayer.cursorItemIconEnabled = false;
		}
	}

	private static void DrawBar(SpriteBatch sb, Rectangle bounds, Color color, float fill, float s, int numberBand)
	{
		var px = TextureAssets.MagicPixel.Value;

		int L = Snap(bounds.X * s);
		int R = Snap(bounds.Right * s);
		int barBottom = Snap(bounds.Bottom * s) - numberBand;
		int w = R - L;

		int border = Math.Max(1, Snap(s));
		int gap = border;
		int cell = Math.Max(2, Snap(5 * s));
		int pitch = cell + gap;

		int availInnerH = barBottom - Snap(bounds.Y * s) - border * 2;
		if (w <= 2 || availInnerH < cell) return;

		int segCount = Math.Max(1, (availInnerH + gap) / pitch);
		int usedInnerH = segCount * pitch - gap;
		int T = barBottom - usedInnerH - border * 2;

		int innerL = L + border;
		int innerW = w - border * 2;
		int innerTop = T + border;
		int innerBottom = innerTop + usedInnerH;

		sb.Draw(px, new Rectangle(L, T, w, barBottom - T), new Color(10, 10, 16));
		sb.Draw(px, new Rectangle(innerL, innerTop, innerW, usedInnerH), new Color(30, 30, 44));

		int litSegs = (int)Math.Round(fill * segCount);
		var highlight = Color.Lerp(color, Color.White, 0.45f);
		var shadow = Mul(color, 0.5f);

		for (int i = 0; i < litSegs; i++)
		{
			int cellBottom = innerBottom - i * pitch;
			int cellTop = cellBottom - cell;
			sb.Draw(px, new Rectangle(innerL, cellTop, innerW, cell), color);
			sb.Draw(px, new Rectangle(innerL, cellTop, innerW, border), highlight);
			if (cell > border)
				sb.Draw(px, new Rectangle(innerL, cellBottom - border, innerW, border), shadow);
		}
	}

	private static void DrawNumber(SpriteBatch sb, Rectangle bounds, long stored, Color color)
	{
		var font = FontAssets.MouseText.Value;
		string txt = UINumberFormat.Energy(stored);
		const float scale = 0.62f;
		var size = font.MeasureString(txt) * scale;
		float x = bounds.Center.X - size.X / 2f;
		float y = bounds.Bottom - size.Y - 1f;
		Terraria.Utils.DrawBorderString(sb, txt, new Vector2(x, y),
			Color.Lerp(color, Color.White, 0.6f), scale);
	}

	private static int Snap(float v) => (int)Math.Round(v);

	private static Color Mul(Color c, float f) =>
		new Color((byte)Math.Min(255f, c.R * f), (byte)Math.Min(255f, c.G * f), (byte)Math.Min(255f, c.B * f));
}
