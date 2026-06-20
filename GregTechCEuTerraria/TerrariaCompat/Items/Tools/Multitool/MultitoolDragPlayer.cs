#nullable enable
using Microsoft.Xna.Framework;
using GregTechCEuTerraria.TerrariaCompat.UI;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools.Multitool;

public sealed class MultitoolDragPlayer : ModPlayer
{
	public static bool Dragging { get; private set; }
	public static Point Start { get; private set; }
	public static Point End { get; private set; }

	private bool _prevDown;

	public override void PostUpdate()
	{
		if (Player.whoAmI != Main.myPlayer) return;

		if (!MultitoolState.IsHeld(Player) || MultitoolState.RadialOpen)
		{
			Dragging = false;
			_prevDown = Main.mouseLeft;
			return;
		}

		MultitoolLayers.EnsureSelection(Player);

		int tx = (int)(Main.MouseWorld.X / 16f);
		int ty = (int)(Main.MouseWorld.Y / 16f);
		UpdateHoverTooltip(tx, ty);

		bool down = Main.mouseLeft;
		bool pressEdge = down && !_prevDown;

		if (!Player.mouseInterface && MultitoolSystem.Eyedropper is not null)
		{
			bool eyePressed;
			try { eyePressed = MultitoolSystem.Eyedropper.JustPressed; }
			catch (System.Collections.Generic.KeyNotFoundException) { eyePressed = false; }
			if (eyePressed) MultitoolPick.TryPickUnderCursor(Player, tx, ty);
		}

		if (!Dragging)
		{
			if (pressEdge && !Player.mouseInterface && CanAct())
			{
				Dragging = true;
				Start = new Point(tx, ty);
				End = Start;
			}
		}
		else
		{
			End = new Point(tx, ty);
			if (!down)
			{
				Commit();
				Dragging = false;
			}
		}

		_prevDown = down;
	}

	private void UpdateHoverTooltip(int x, int y)
	{
		if (Player.mouseInterface) return;
		if (x <= 0 || x >= Main.maxTilesX - 1 || y <= 0 || y >= Main.maxTilesY - 1) return;

		var layer = MultitoolLayers.Active;
		string status;
		if (MultitoolState.Cutting)
			status = layer.Name + ": Cut";
		else if (!layer.Enabled)
			status = layer.Name + " (coming soon)";
		else if (MultitoolLayers.TryResolveArmedVariant(Player, out var v))
		{
			string w = layer.WidthOptions.Count > 0 ? layer.WidthLabel(MultitoolState.Width) + " " : "";
			int tiles = layer.AffordableTiles(v, MultitoolState.Width);
			string tilesText = tiles == int.MaxValue ? "" : " - " + tiles + " tiles";
			status = $"{layer.Name} {w}{v.ValueLabel}{tilesText}";
		}
		else
			status = layer.Name + ": empty - RMB to set up";

		WorldHoverTooltip.Set(status + "\n" + MultitoolPick.HotkeyHint(),
			WorldHoverTooltip.HoverPriority.Tool);
	}

	private bool CanAct()
	{
		var layer = MultitoolLayers.Active;
		if (!layer.Enabled) return false;
		if (MultitoolState.Cutting) return true;
		return MultitoolLayers.TryResolveArmedVariant(Player, out _);
	}

	private void Commit()
	{
		var layer = MultitoolLayers.Active;
		if (!layer.Enabled) return;

		var path = MultitoolState.LPath(Start, End, Player.direction);
		if (MultitoolState.Cutting)
		{
			layer.CommitCut(Player, path);
			return;
		}

		if (MultitoolLayers.TryResolveArmedVariant(Player, out var v))
			layer.CommitPlace(Player, path, v, MultitoolState.Width);
	}
}
