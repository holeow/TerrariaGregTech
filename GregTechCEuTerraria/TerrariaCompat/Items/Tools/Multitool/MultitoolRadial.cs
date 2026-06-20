#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools.Multitool;

internal static class MultitoolRadial
{
	private const float FanRadius = 52f;

	public static void UpdateAndDraw(SpriteBatch sb, Player player)
	{
		if (!MultitoolState.RadialOpen)
		{
			TryOpen(player);
			return;
		}

		if (Main.mouseRight && Main.mouseRightRelease) { Close(); return; }

		MultitoolLayers.EnsureSelection(player);

		bool consumed = false;
		Vector2 c = MultitoolState.RadialAnchor;

		Label(sb, MultitoolPick.HotkeyHint(), new Vector2(c.X, c.Y - FanRadius - 34f), 0.5f, dim: true);

		DrawLayerFan(sb, player, c, ref consumed);
		DrawCutHub(sb, c, ref consumed);
		DrawActivePanel(sb, player, c, ref consumed);

		player.mouseInterface = true;
		if (Main.mouseLeft && Main.mouseLeftRelease && !consumed) Close();
	}

	private static void TryOpen(Player player)
	{
		if (!(Main.mouseRight && Main.mouseRightRelease)) return;
		if (player.mouseInterface) return;

		MultitoolState.RadialOpen = true;
		MultitoolState.RadialAnchor = Main.MouseScreen;
		Main.mouseRightRelease = false;
	}

	private static void Close()
	{
		MultitoolState.RadialOpen = false;
		Main.mouseRightRelease = false;
		Main.mouseLeftRelease = false;
	}

	private static void DrawLayerFan(SpriteBatch sb, Player player, Vector2 c, ref bool consumed)
	{
		var layers = MultitoolLayers.All;
		int n = layers.Count;
		for (int i = 0; i < n; i++)
		{
			float ang = -MathHelper.PiOver2 + i * (MathHelper.TwoPi / n);
			Vector2 p = c + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * FanRadius;
			var r = CenteredRect(p, 38, 38);
			bool active = layers[i].Id == MultitoolState.ActiveLayerId;
			bool hover = IconButton(sb, r, layers[i].IconItem(player), null,
				highlighted: active, dim: !layers[i].Enabled);
			if (hover)
			{
				consumed = true;
				if (Clicked()) SelectLayer(layers[i]);
			}
		}
	}

	private static void SelectLayer(MultitoolLayer layer)
	{
		MultitoolState.ActiveLayerId = layer.Id;
		var opts = layer.WidthOptions;
		if (opts.Count > 0)
		{
			bool ok = false;
			foreach (var o in opts) if (o == MultitoolState.Width) { ok = true; break; }
			if (!ok) MultitoolState.Width = opts[0];
		}
	}

	private static void DrawCutHub(SpriteBatch sb, Vector2 c, ref bool consumed)
	{
		var r = CenteredRect(c, 34, 34);
		bool hover = r.Contains(Main.MouseScreen.ToPoint());
		sb.Draw(TextureAssets.InventoryBack.Value, r,
			MultitoolState.Cutting ? new Color(255, 150, 150) : Color.White);
		DrawItemIcon(sb, ItemID.WireCutter, r, dim: false);
		Color? border = MultitoolState.Cutting ? new Color(220, 90, 90)
			: hover ? new Color(225, 225, 130) : (Color?)null;
		if (border is { } bc) Border(sb, r, bc, 2);

		if (hover)
		{
			consumed = true;
			Main.instance.MouseText(MultitoolState.Cutting ? "Cut: ON" : "Cut: OFF");
			if (Clicked()) MultitoolState.Cutting = !MultitoolState.Cutting;
		}
	}

	private static void DrawActivePanel(SpriteBatch sb, Player player, Vector2 c, ref bool consumed)
	{
		var layer = MultitoolLayers.Active;
		float panelY = c.Y + FanRadius + 18f;

		if (MultitoolState.Cutting)
		{
			Label(sb, "Cut mode - left-drag to remove " + layer.Name.ToLowerInvariant(),
				new Vector2(c.X, panelY), 0.5f, dim: true);
			return;
		}

		if (!layer.Enabled)
		{
			Label(sb, layer.Name + " - coming soon", new Vector2(c.X, panelY), 0.5f, dim: true);
			return;
		}

		MultitoolLayers.TryResolveArmedVariant(player, out _);
		var variants = layer.Variants(player);
		if (variants.Count == 0)
		{
			Label(sb, "(no " + layer.Name.ToLowerInvariant() + " in inventory)",
				new Vector2(c.X, panelY), 0.5f, dim: true);
			return;
		}

		const int cols = 6, cell = 40, labelH = 13;
		int shown = Math.Min(variants.Count, cols);
		float gridW = shown * cell;
		float left = c.X - gridW / 2f;
		for (int i = 0; i < variants.Count; i++)
		{
			int col = i % cols, row = i / cols;
			var r = CenteredRect(new Vector2(left + col * cell + cell / 2f, panelY + 18f + row * (cell + labelH)),
				34, 34);
			bool armed = variants[i].Key == MultitoolState.ArmedVariantKey;
			bool hover = IconButton(sb, r, variants[i].IconItem, variants[i].ValueLabel,
				highlighted: armed, dim: false);
			if (hover)
			{
				consumed = true;
				if (Clicked()) MultitoolState.ArmedVariantKey = variants[i].Key;
			}
		}

		var widths = layer.WidthOptions;
		if (widths.Count > 0)
		{
			int rows = (variants.Count + cols - 1) / cols;
			float wy = panelY + 18f + rows * (cell + labelH) + 4f;
			float wW = widths.Count * 36f;
			float wleft = c.X - wW / 2f;
			for (int i = 0; i < widths.Count; i++)
			{
				var r = new Rectangle((int)(wleft + i * 36f), (int)wy, 32, 20);
				bool sel = MultitoolState.Width == widths[i];
				bool hover = TextChip(sb, r, layer.WidthLabel(widths[i]), sel, accent: new Color(90, 150, 220));
				if (hover)
				{
					consumed = true;
					if (Clicked()) MultitoolState.Width = widths[i];
				}
			}
		}
	}

	private static bool Clicked() => Main.mouseLeft && Main.mouseLeftRelease;

	private static bool IconButton(SpriteBatch sb, Rectangle r, int itemType, string? valueLabel,
		bool highlighted, bool dim)
	{
		bool hover = r.Contains(Main.MouseScreen.ToPoint());
		var back = TextureAssets.InventoryBack.Value;
		sb.Draw(back, r, Color.White * (dim ? 0.4f : 1f));

		DrawItemIcon(sb, itemType, r, dim);

		Color? border = highlighted ? new Color(120, 230, 140)
			: hover ? new Color(225, 225, 130) : (Color?)null;
		if (border is { } bc) Border(sb, r, bc, 2);

		if (valueLabel != null)
			Terraria.Utils.DrawBorderString(sb, valueLabel,
				new Vector2(r.Center.X, r.Bottom + 1), dim ? new Color(150, 150, 150) : Color.White,
				0.72f, 0.5f, 0f);

		if (hover && itemType > 0)
		{
			var sample = ContentSamples.ItemsByType[itemType].Clone();
			Main.HoverItem = sample;
			Main.hoverItemName = sample.Name;
		}
		return hover;
	}

	private static void DrawItemIcon(SpriteBatch sb, int itemType, Rectangle r, bool dim)
	{
		if (itemType <= 0) return;
		Main.instance.LoadItem(itemType);
		if (ItemLoader.GetItem(itemType) is ITextureWarmUp warm) warm.WarmUpTexture();

		var tex = TextureAssets.Item[itemType].Value;
		Rectangle frame = Main.itemAnimations[itemType] != null
			? Main.itemAnimations[itemType].GetFrame(tex)
			: tex.Frame();
		float maxDim = r.Width * 0.7f;
		float sc = Math.Min(maxDim / frame.Width, maxDim / frame.Height);
		if (sc > 3f) sc = 3f;
		Color tint = dim ? new Color(120, 120, 120, 160) : Color.White;
		sb.Draw(tex, new Vector2(r.Center.X, r.Center.Y), frame, tint, 0f, frame.Size() / 2f,
			sc, SpriteEffects.None, 0f);
	}

	private static bool TextChip(SpriteBatch sb, Rectangle r, string label, bool selected, Color accent)
	{
		bool hover = r.Contains(Main.MouseScreen.ToPoint());
		Color bg = selected ? accent : hover ? new Color(80, 82, 95, 235) : new Color(45, 46, 56, 225);
		sb.Draw(TextureAssets.MagicPixel.Value, r, bg);
		Border(sb, r, selected ? Color.White : hover ? new Color(160, 160, 170) : new Color(90, 90, 100), 1);
		var font = FontAssets.MouseText.Value;
		float scTxt = Math.Min(0.9f, (r.Width - 8f) / Math.Max(1f, font.MeasureString(label).X));
		Terraria.Utils.DrawBorderString(sb, label, new Vector2(r.Center.X, r.Center.Y - 8), Color.White,
			scTxt, 0.5f, 0f);
		return hover;
	}

	private static void Label(SpriteBatch sb, string text, Vector2 pos, float alignX, bool dim = false)
		=> Terraria.Utils.DrawBorderString(sb, text, pos,
			dim ? new Color(170, 170, 175) : Color.White, 0.8f, alignX, 0f);

	private static void Border(SpriteBatch sb, Rectangle r, Color c, int t)
	{
		var px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, t), c);
		sb.Draw(px, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
		sb.Draw(px, new Rectangle(r.X, r.Y, t, r.Height), c);
		sb.Draw(px, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
	}

	private static Rectangle CenteredRect(Vector2 center, int w, int h)
		=> new((int)(center.X - w / 2f), (int)(center.Y - h / 2f), w, h);
}
