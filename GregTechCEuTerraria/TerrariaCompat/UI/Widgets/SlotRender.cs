#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

internal static class SlotRender
{
	public const float NativeSlotPixels = 52f;

	public static void DrawAmount(SpriteBatch sb, Rectangle bounds, AEKey? what, long amount, long hideAtOrBelow = 0)
	{
		if (amount <= hideAtOrBelow) return;
		string text = UINumberFormat.Amount(what, amount);
		DynamicSpriteFont font = FontAssets.ItemStack.Value;
		float scale = bounds.Width / NativeSlotPixels;
		var position = new Vector2(bounds.X, bounds.Y);
		var offset = new Vector2(10f, 26f + font.LineSpacing);
		ChatManager.DrawColorCodedStringWithShadow(sb, font, text,
			position + offset * scale,
			Color.White, 0f, new Vector2(0f, font.LineSpacing), new Vector2(scale));
	}
}
