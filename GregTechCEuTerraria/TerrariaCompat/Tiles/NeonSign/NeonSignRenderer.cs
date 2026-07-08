#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.NeonSign;

public static class NeonSignRenderer
{
	public static void Draw(SpriteBatch sb, int i, int j, NeonSignEntity sign)
	{
		string text = sign.Text;
		if (string.IsNullOrEmpty(text)) return;

		var font = FontAssets.MouseText.Value;
		float scale = sign.Scale;
		Color color = NeonSignPalette.ColorFor(sign.ColorIndex);

		Vector2 offset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange, Main.offScreenRange);
		Vector2 tileCenter = new Vector2(i * 16 + 8, j * 16 + 8) - Main.screenPosition + offset;

		string[] lines = text.Replace("\r", "").Split('\n');
		float lineHeight = font.LineSpacing * scale;
		float topY = tileCenter.Y - lineHeight * lines.Length;

		for (int line = 0; line < lines.Length; line++)
		{
			var pos = new Vector2(tileCenter.X, topY + line * lineHeight);
			Terraria.Utils.DrawBorderString(sb, lines[line], pos, color, scale, 0.5f, 0f);
		}
	}
}
