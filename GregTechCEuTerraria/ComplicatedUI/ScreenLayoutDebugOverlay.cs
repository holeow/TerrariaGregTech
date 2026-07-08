#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class ScreenLayoutDebugOverlay : ModSystem
{
	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (idx < 0) idx = layers.Count;
		layers.Insert(idx, new LegacyGameInterfaceLayer(
			"GregTechCEuTerraria: ScreenLayout Debug", Draw, InterfaceScaleType.UI));
	}

	private static bool Draw()
	{
		if (Main.dedServ || !GTClientConfig.Instance.DebugScreenLayout || !Main.playerInventory) return true;

		var sb = Main.spriteBatch;
		var font = FontAssets.MouseText.Value;

		var windows = new List<Rectangle>(UILayers.OpenModalBounds(GlobalRecipeBrowserSystem.LayerNameStr));

		int i = 0;
		foreach (var o in ScreenLayout.Occupied)
		{
			Fill(sb, o, new Color(220, 40, 40, 60));
			Border(sb, o, new Color(255, 80, 80), 1);
			Text(sb, font, i.ToString(), new Vector2(o.X + 2, o.Y + 1), Color.White, 0.7f);
			i++;
		}
		foreach (var o in windows)
		{
			Fill(sb, o, new Color(220, 140, 40, 60));
			Border(sb, o, new Color(255, 180, 80), 1);
		}

		var screen = ScreenLayout.Screen;
		var recipes = DockRegions.Recipes(windows);
		Border(sb, recipes, new Color(80, 255, 120), 2);
		Text(sb, font, "RECIPES", new Vector2(recipes.X + 4, recipes.Y + 2), new Color(80, 255, 120), 0.8f);

		var fav = DockRegions.Favorites(windows);
		Border(sb, fav, new Color(120, 180, 255), 2);
		Text(sb, font, "FAV", new Vector2(fav.X + 4, fav.Y + 2), new Color(120, 180, 255), 0.8f);

		Text(sb, font, "ScreenLayout debug  (obstacles=red, recipes=green, favorites=blue)",
			new Vector2(screen.Width / 2 - 200, 4), Color.White, 0.7f);

		return true;
	}

	private static void Fill(SpriteBatch sb, Rectangle r, Color c)
		=> sb.Draw(TextureAssets.MagicPixel.Value, r, c);

	private static void Border(SpriteBatch sb, Rectangle r, Color c, int t)
	{
		var px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, t), c);
		sb.Draw(px, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
		sb.Draw(px, new Rectangle(r.X, r.Y, t, r.Height), c);
		sb.Draw(px, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
	}

	private static void Text(SpriteBatch sb, DynamicSpriteFont font, string s, Vector2 at, Color c, float scale)
	{
		sb.DrawString(font, s, at + new Vector2(1, 1), Color.Black * 0.7f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		sb.DrawString(font, s, at, c, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}
