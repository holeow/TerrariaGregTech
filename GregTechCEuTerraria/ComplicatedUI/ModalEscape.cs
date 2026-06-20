#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class ModalEscape
{
	private static readonly InputMode[] KeyboardModes = { InputMode.Keyboard, InputMode.KeyboardUI };
	private static readonly string[] EscapeKey = { "Escape" };

	public static void SuppressItemUse(UIState state)
	{
		if (state is null) return;
		var mouse = Main.MouseScreen / Main.UIScale;
		foreach (var child in state.Children)
		{
			if (child.ContainsPoint(mouse))
			{
				var p = Main.LocalPlayer;
				p.mouseInterface = true;
				p.controlUseItem = false;
				p.controlUseTile = false;
				return;
			}
		}
	}

	public static void SuppressVanillaUIClicks(UIState state)
	{
		if (state is null) return;
		var mouse = Main.MouseScreen;
		foreach (var child in state.Children)
		{
			if (child.ContainsPoint(mouse))
			{
				Main.mouseLeftRelease = false;
				Main.mouseRightRelease = false;
				Main.stackSplit = 9999;
				return;
			}
		}
	}

	private static long _escConsumedTick = -1;

	public static bool EscJustPressed =>
		_escConsumedTick != Main.GameUpdateCount
		&& Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape);

	public static void ConsumeEscape()
	{
		_escConsumedTick = Main.GameUpdateCount;
		foreach (var mode in KeyboardModes)
			ConsumePhysicalKeys(EscapeKey, mode);
	}

	public static void WithCursorParked(System.Action action)
	{
		int mx = Main.mouseX, my = Main.mouseY;
		Main.mouseX = Main.mouseY = -10000;
		try { action(); }
		finally { Main.mouseX = mx; Main.mouseY = my; }
	}

	public static void ConsumePhysicalKeys(ICollection<string> physicalKeys, InputMode mode)
	{
		if (physicalKeys.Count == 0) return;
		var profile = PlayerInput.CurrentProfile;
		if (profile is null || !profile.InputModes.TryGetValue(mode, out var cfg)) return;

		var current = PlayerInput.Triggers.Current;
		var justPressed = PlayerInput.Triggers.JustPressed;

		foreach (var binding in cfg.KeyStatus)
		{
			bool sharesKey = false;
			foreach (var key in binding.Value)
				if (physicalKeys.Contains(key)) { sharesKey = true; break; }
			if (!sharesKey) continue;

			if (current.KeyStatus.ContainsKey(binding.Key))
				current.KeyStatus[binding.Key] = false;
			if (justPressed.KeyStatus.ContainsKey(binding.Key))
				justPressed.KeyStatus[binding.Key] = false;
		}
	}
}
