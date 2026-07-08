#nullable enable
using System;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class ItemDrag
{
	private const float Threshold = 6f;

	public static int ItemType { get; private set; }
	public static string? FluidId { get; private set; }
	public static string? FluidLabel { get; private set; }

	private static bool _armed;
	private static Vector2 _startPos;

	public static bool Armed => _armed;
	public static bool Active => _armed && Vector2.Distance(ModalEscape.UiCursor, _startPos) > Threshold;

	public static void ArmItem(int itemType)
	{
		if (itemType <= 0) return;
		_armed = true; ItemType = itemType; FluidId = null; FluidLabel = null;
		_startPos = ModalEscape.UiCursor;
	}

	public static void ArmFluid(string fluidId, string? label)
	{
		if (string.IsNullOrEmpty(fluidId)) return;
		_armed = true; ItemType = 0; FluidId = fluidId; FluidLabel = label;
		_startPos = ModalEscape.UiCursor;
	}

	public static void Clear()
	{
		_armed = false; ItemType = 0; FluidId = null; FluidLabel = null;
	}

	public static bool TryDropItem(Rectangle rect, out int itemType)
	{
		itemType = 0;
		if (!Active || ItemType <= 0 || !MouseClick.LeftReleased) return false;
		if (!rect.Contains(ModalEscape.UiCursorPoint)) return false;
		itemType = ItemType;
		Clear();
		return true;
	}

	public static bool TryDropFluid(Rectangle rect, out string fluidId, out string? label)
	{
		fluidId = ""; label = null;
		if (!Active || FluidId is null || !MouseClick.LeftReleased) return false;
		if (!rect.Contains(ModalEscape.UiCursorPoint)) return false;
		fluidId = FluidId; label = FluidLabel;
		Clear();
		return true;
	}

	public static bool TryDropInto(Rectangle rect, Action<int>? onItem, Action<string>? onFluid = null)
	{
		if (onItem != null && TryDropItem(rect, out int t)) { onItem(t); return true; }
		if (onFluid != null && TryDropFluid(rect, out var id, out _)) { onFluid(id); return true; }
		return false;
	}

	public static void ArmFromHover(int itemType, string? fluidId, string? fluidLabel)
	{
		if (!MouseClick.LeftPressed) return;
		if (itemType > 0) ArmItem(itemType);
		else if (!string.IsNullOrEmpty(fluidId)) ArmFluid(fluidId!, fluidLabel);
	}

	public static void Draw(SpriteBatch sb)
	{
		if (Active) DrawPayload(sb);
		if (MouseClick.LeftReleased) Clear();
	}

	private static void DrawPayload(SpriteBatch sb)
	{
		var pos = ModalEscape.UiCursor;
		if (ItemType > 0)
		{
			Main.instance.LoadItem(ItemType);
			var tex = TextureAssets.Item[ItemType].Value;
			Rectangle frame = Main.itemAnimations[ItemType] != null
				? Main.itemAnimations[ItemType].GetFrame(tex, -1)
				: tex.Frame();
			float scale = 32f / Math.Max(1, Math.Max(frame.Width, frame.Height));
			if (scale > 1f) scale = 1f;
			sb.Draw(tex, pos, frame, Color.White * 0.85f, 0f, frame.Size() / 2f, scale,
				SpriteEffects.None, 0f);
		}
		else if (FluidId is not null)
		{
			var fluid = FluidRegistry.Get(FluidId);
			uint c = fluid?.Color ?? 0xFF87CEEBu;
			var col = new Color((byte)(c >> 16), (byte)(c >> 8), (byte)c);
			var r = new Rectangle((int)pos.X - 14, (int)pos.Y - 14, 28, 28);
			sb.Draw(TextureAssets.MagicPixel.Value, r, col * 0.85f);
		}
	}
}
