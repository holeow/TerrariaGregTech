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

namespace GregTechCEuTerraria.TerrariaCompat.Bosses.Common;

public class DebugOverlaySystem : ModSystem
{
	private static readonly List<string> _scratch = new();

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (idx < 0) idx = layers.Count;
		layers.Insert(idx, new LegacyGameInterfaceLayer(
			"GregTechCEuTerraria: Boss Debug Overlay",
			DrawOverlay, InterfaceScaleType.UI));
	}

	private static bool DrawOverlay()
	{
		if (!GTConfig.Instance.DebugMobs) return true;

		float x = 12f;
		float y = 320f;
		const float lineH = 16f;
		const float panelPad = 6f;

		var font = FontAssets.MouseText.Value;
		var sb = Main.spriteBatch;

		bool drewAny = false;
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			var npc = Main.npc[i];
			if (!npc.active || npc.ModNPC is not IDebuggableBoss debug) continue;

			_scratch.Clear();
			debug.BuildDebugLines(_scratch);
			if (_scratch.Count == 0) continue;
			drewAny = true;

			float maxW = 0f;
			foreach (var line in _scratch)
			{
				float w = font.MeasureString(line).X * 0.7f;
				if (w > maxW) maxW = w;
			}
			float panelH = _scratch.Count * lineH + panelPad * 2;
			float panelW = maxW + panelPad * 2;

			DrawRect(sb, new Rectangle((int)x, (int)y, (int)panelW, (int)panelH),
				new Color(0, 0, 0, 180));
			DrawRectBorder(sb, new Rectangle((int)x, (int)y, (int)panelW, (int)panelH),
				new Color(255, 120, 60), 1);

			for (int li = 0; li < _scratch.Count; li++)
			{
				Color c = li == 0 ? new Color(255, 200, 120) : Color.White;
				float scale = li == 0 ? 0.85f : 0.7f;
				DrawShadowed(sb, font, _scratch[li],
					new Vector2(x + panelPad, y + panelPad + li * lineH), c, scale);
			}

			y += panelH + 8f;
		}

		if (drewAny || BossFightTracker.RecentHits.Count > 0)
			DrawFightSummary(sb, font, x, ref y, lineH, panelPad);

		return true;
	}

	private static void DrawFightSummary(SpriteBatch sb, DynamicSpriteFont font,
		float x, ref float y, float lineH, float panelPad)
	{
		_scratch.Clear();

		_scratch.Add(BossFightTracker.FightActive
			? $"Fight  {BossFightTracker.FightDurationSec:0.0}s   DPS to boss {BossFightTracker.DpsToBosses:0}   Taking {BossFightTracker.DpsTaken:0}/s"
			: $"Fight ended   final DPS to boss {BossFightTracker.DpsToBosses:0}   took {BossFightTracker.DpsTaken:0}/s");

		_scratch.Add($"Total taken: {BossFightTracker.DamageTakenTotal}    Top sources:");
		foreach (var (src, hits, dmg) in BossFightTracker.TopDamageSources(5))
			_scratch.Add($"  {hits,3}x {dmg,5} dmg   {src}");

		if (BossFightTracker.RecentHits.Count > 0)
		{
			_scratch.Add("Last hits:");
			for (int i = BossFightTracker.RecentHits.Count - 1; i >= 0; i--)
			{
				var h = BossFightTracker.RecentHits[i];
				_scratch.Add($"  t={h.Tick / 60f:0.0}s  {h.Damage} dmg <- {h.Source}");
			}
		}

		float maxW = 0f;
		foreach (var line in _scratch)
		{
			float w = font.MeasureString(line).X * 0.7f;
			if (w > maxW) maxW = w;
		}
		float panelH = _scratch.Count * lineH + panelPad * 2;
		float panelW = maxW + panelPad * 2;

		DrawRect(sb, new Rectangle((int)x, (int)y, (int)panelW, (int)panelH),
			new Color(0, 0, 0, 180));
		DrawRectBorder(sb, new Rectangle((int)x, (int)y, (int)panelW, (int)panelH),
			new Color(220, 100, 220), 1);

		for (int li = 0; li < _scratch.Count; li++)
		{
			Color c = li == 0 ? new Color(255, 180, 220) : Color.White;
			float scale = li == 0 ? 0.85f : 0.7f;
			DrawShadowed(sb, font, _scratch[li],
				new Vector2(x + panelPad, y + panelPad + li * lineH), c, scale);
		}

		y += panelH + 8f;
	}

	internal static void DrawRect(SpriteBatch sb, Rectangle r, Color c)
	{
		sb.Draw(TextureAssets.MagicPixel.Value, r, c);
	}

	internal static void DrawRectBorder(SpriteBatch sb, Rectangle r, Color c, int thick)
	{
		var px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, thick), c);
		sb.Draw(px, new Rectangle(r.X, r.Bottom - thick, r.Width, thick), c);
		sb.Draw(px, new Rectangle(r.X, r.Y, thick, r.Height), c);
		sb.Draw(px, new Rectangle(r.Right - thick, r.Y, thick, r.Height), c);
	}

	internal static void DrawShadowed(SpriteBatch sb, DynamicSpriteFont font, string s,
		Vector2 at, Color c, float scale)
	{
		sb.DrawString(font, s, at + new Vector2(1, 1), Color.Black * 0.7f, 0f,
			Vector2.Zero, scale, SpriteEffects.None, 0f);
		sb.DrawString(font, s, at, c, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}

	public static void DrawLine(SpriteBatch sb, Vector2 fromScreen, Vector2 toScreen,
		Color c, int thickness = 2)
	{
		Vector2 delta = toScreen - fromScreen;
		float len = delta.Length();
		if (len < 0.5f) return;
		float rot = delta.ToRotation();
		sb.Draw(TextureAssets.MagicPixel.Value, fromScreen, null, c, rot,
			new Vector2(0f, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0);
	}

	public static void DrawCircle(SpriteBatch sb, Vector2 centreWorld, float radius,
		Vector2 screenPos, Color c, int segments = 32, int thickness = 2)
	{
		if (radius < 1f || segments < 4) return;
		Vector2 centreScreen = centreWorld - screenPos;
		float step = MathHelper.TwoPi / segments;
		// Chord length between adjacent segment endpoints.
		float chord = 2f * radius * (float)System.Math.Sin(step * 0.5f);
		for (int i = 0; i < segments; i++)
		{
			float ang = i * step;
			Vector2 at = centreScreen + ang.ToRotationVector2() * radius;
			float rot = ang + MathHelper.PiOver2;
			sb.Draw(TextureAssets.MagicPixel.Value, at, null, c, rot,
				new Vector2(0f, 0.5f), new Vector2(chord, thickness), SpriteEffects.None, 0);
		}
	}

	public static void DrawCrosshair(SpriteBatch sb, Vector2 atWorld, Vector2 screenPos,
		Color c, float size = 14f, int thickness = 2)
	{
		int s = System.Math.Max(2, (int)size);
		int cx = (int)(atWorld.X - screenPos.X);
		int cy = (int)(atWorld.Y - screenPos.Y);
		DrawRectBorder(sb, new Rectangle(cx - s, cy - s, s * 2, s * 2), c, thickness);
	}

	public static void DrawLabel(SpriteBatch sb, string text, Vector2 atWorld,
		Vector2 screenPos, Color c, float scale = 0.7f)
	{
		var font = FontAssets.MouseText.Value;
		Vector2 at = atWorld - screenPos;
		DrawShadowed(sb, font, text, at, c, scale);
	}
}
