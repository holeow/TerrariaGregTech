#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class UILayers
{
	private static readonly Dictionary<string, Func<bool>> _modalIsOpen = new();
	private static readonly Dictionary<string, Func<bool>> _modalCursorOver = new();
	private static readonly List<string> _modalStack = new();
	private static uint _stackSyncTick = uint.MaxValue;

	public static void Push(string name)
	{
		_modalStack.Remove(name);
		_modalStack.Add(name);
	}

	private static void SyncModalStack()
	{
		if (_stackSyncTick == Main.GameUpdateCount) return;
		_stackSyncTick = Main.GameUpdateCount;
		_modalStack.RemoveAll(n => !_modalIsOpen.TryGetValue(n, out var f) || !f());
	}

	public static void RegisterModal(string name, Func<bool> isOpen)
		=> _modalIsOpen[name] = isOpen;

	public static void RegisterModalCursorProbe(string name, Func<bool> cursorOver)
		=> _modalCursorOver[name] = cursorOver;

	public static bool IsCursorOverAnyModal()
	{
		SyncModalStack();
		foreach (var name in _modalStack)
			if (_modalCursorOver.TryGetValue(name, out var f) && f())
				return true;
		return false;
	}

	public static string? TopmostModalAtCursor()
	{
		SyncModalStack();
		for (int i = _modalStack.Count - 1; i >= 0; i--)
			if (_modalCursorOver.TryGetValue(_modalStack[i], out var f) && f())
				return _modalStack[i];
		return null;
	}

	public static bool IsCursorOverHigherModal(string myName)
	{
		SyncModalStack();
		int i = _modalStack.IndexOf(myName);
		if (i < 0) return false;
		for (int j = i + 1; j < _modalStack.Count; j++)
			if (_modalCursorOver.TryGetValue(_modalStack[j], out var f) && f())
				return true;
		return false;
	}

	public static string? TopModal
	{ get { SyncModalStack(); return _modalStack.Count > 0 ? _modalStack[^1] : null; } }

	public static bool IsTopModal(string name)
	{ SyncModalStack(); return _modalStack.Count > 0 && _modalStack[^1] == name; }

	private static int ModalDepth(string name) { SyncModalStack(); return _modalStack.IndexOf(name); }

	public static void InsertButton(
		List<GameInterfaceLayer> layers, string layerName, GameInterfaceDrawMethod drawFn)
		=> Insert(layers, layerName, drawFn, AnchorPlacement.AboveAccessoryBar);

	private const int BtnX = 580, BtnTop = 86, BtnStep = 36, BtnSize = 30;
	public static void DrawStackedButton(
		int slot, Color background, Action<SpriteBatch, Rectangle> drawIcon,
		string tooltip, Action onClick, Func<bool>? visible = null, ModKeybind? keybind = null)
	{
		if (Main.dedServ || !Main.playerInventory) return;
		if (visible != null && !visible()) return;

		var rect = new Rectangle(BtnX, BtnTop + slot * BtnStep, BtnSize, BtnSize);
		bool hover = rect.Contains(new Point(Main.mouseX, Main.mouseY))
			&& !PlayerInput.IgnoreMouseInterface;
		if (hover)
		{
			Main.LocalPlayer.mouseInterface = true;
			if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
				onClick();
			}
		}

		var sb = Main.spriteBatch;
		var px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, rect, background);
		drawIcon(sb, rect);

		if (hover)
		{
			sb.Draw(px, rect, Color.White * 0.16f);
			var b = new Color(255, 235, 140);
			sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), b);
			sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), b);
			sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height), b);
			sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), b);
			DrawHotkeyTooltip(tooltip, keybind);
		}
	}

	private static readonly Color TooltipTitleColor = new(120, 180, 255);
	private static readonly Color TooltipHotkeyColor = new(255, 90, 90);

	private static void DrawHotkeyTooltip(string title, ModKeybind? keybind)
	{
		string keyStr = "NOT SET";
		if (keybind != null)
		{
			try
			{
				var keys = keybind.GetAssignedKeys();
				if (keys.Count > 0) keyStr = string.Join(", ", keys);
			}
			catch { /* key not yet registered */ }
		}
		string hotkey = $"Hotkey: {keyStr} (Set in controls settings)";

		var font = FontAssets.MouseText.Value;
		Vector2 titleSize = font.MeasureString(title);
		Vector2 hotkeySize = font.MeasureString(hotkey);
		float w = System.Math.Max(titleSize.X, hotkeySize.X);
		float h = titleSize.Y + hotkeySize.Y + 4f;

		float x = Main.mouseX + 14f;
		float y = Main.mouseY + 14f;
		if (x + w + 16f > Main.screenWidth) x = Main.screenWidth - w - 16f;
		if (y + h + 16f > Main.screenHeight) y = Main.screenHeight - h - 16f;

		var sb = Main.spriteBatch;
		var px = TextureAssets.MagicPixel.Value;
		var bg = new Rectangle((int)x - 7, (int)y - 5, (int)w + 14, (int)h + 10);
		sb.Draw(px, bg, new Color(20, 22, 38) * 0.92f);

		ChatManager.DrawColorCodedStringWithShadow(sb, font, title,
			new Vector2(x, y), TooltipTitleColor, 0f, Vector2.Zero, Vector2.One);
		ChatManager.DrawColorCodedStringWithShadow(sb, font, hotkey,
			new Vector2(x, y + titleSize.Y + 4f), TooltipHotkeyColor, 0f, Vector2.Zero, Vector2.One);
	}

	public static void InsertModal(
		List<GameInterfaceLayer> layers, string layerName, GameInterfaceDrawMethod drawFn)
		=> Insert(layers, layerName, drawFn, AnchorPlacement.BelowMouseText);

	private enum AnchorPlacement { AboveAccessoryBar, BelowMouseText }

	private static void Insert(
		List<GameInterfaceLayer> layers, string layerName, GameInterfaceDrawMethod drawFn,
		AnchorPlacement placement)
	{
		int idx;
		if (placement == AnchorPlacement.BelowMouseText)
		{
			idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
			if (idx >= 0)
			{
				int myDepth = ModalDepth(layerName);
				int insertAt = idx;
				for (int i = idx - 1; i >= 0; i--)
				{
					int d = ModalDepth(layers[i].Name);
					if (d < 0) continue;
					if (d > myDepth) insertAt = i;
					else break;
				}
				layers.Insert(insertAt, MakeLayer(layerName, drawFn));
				return;
			}
		}

		idx = layers.FindIndex(l => l.Name == "Vanilla: Info Accessories Bar");
		if (idx < 0) idx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
		if (idx < 0) idx = layers.Count - 1;
		layers.Insert(idx + 1, MakeLayer(layerName, drawFn));
	}

	private static LegacyGameInterfaceLayer MakeLayer(string name, GameInterfaceDrawMethod draw)
		=> new(name, draw, InterfaceScaleType.UI);
}
