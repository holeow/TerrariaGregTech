#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using GregTechCEuTerraria.TerrariaCompat.UI;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools.Multitool;

public sealed class MultitoolSystem : ModSystem
{
	public static ModKeybind? Eyedropper;

	public override void Load()
	{
		Language.GetOrRegister("Mods.GregTechCEuTerraria.Keybinds.MultitoolEyedropper.DisplayName",
			() => "Greg Multitool - eyedropper");
		Eyedropper = KeybindLoader.RegisterKeybind(Mod, "MultitoolEyedropper", Keys.Z);
	}

	public override void Unload() => Eyedropper = null;

	public override void PostUpdateInput()
	{
		var p = Main.LocalPlayer;
		if (Main.dedServ || p is null || !MultitoolState.IsHeld(p)) return;
		p.controlUseTile = false;
		p.releaseUseTile = false;
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseText = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		int insertAt = mouseText >= 0 ? mouseText : layers.Count;
		layers.Insert(insertAt, new LegacyGameInterfaceLayer(
			"GregTechCEuTerraria: Multitool Drag Preview",
			DrawDragPreview, InterfaceScaleType.Game));

		UILayers.InsertModal(layers, "GregTechCEuTerraria: Multitool Radial", () =>
		{
			var player = Main.LocalPlayer;
			if (!Main.gameMenu && player is not null && MultitoolState.IsHeld(player))
				MultitoolRadial.UpdateAndDraw(Main.spriteBatch, player);
			else
				MultitoolState.RadialOpen = false;
			return true;
		});
	}

	private static bool DrawDragPreview()
	{
		if (Main.dedServ || Main.gameMenu || !MultitoolDragPlayer.Dragging) return true;

		var player = Main.LocalPlayer;
		var layer = MultitoolLayers.Active;
		if (!layer.Enabled) return true;

		var path = MultitoolState.LPath(MultitoolDragPlayer.Start, MultitoolDragPlayer.End, player.direction);
		bool cut = MultitoolState.Cutting;

		int affordable = int.MaxValue;
		if (!cut)
		{
			affordable = MultitoolLayers.TryResolveArmedVariant(player, out var v)
				? layer.AffordableTiles(v, MultitoolState.Width)
				: 0;
		}

		var sb = Main.spriteBatch;
		var px = TextureAssets.MagicPixel.Value;
		var sp = Main.screenPosition;
		for (int i = 0; i < path.Count; i++)
		{
			var t = path[i];
			bool ok = cut ? layer.HasCellAt(t.X, t.Y) : i < affordable;
			Color edge = ok ? new Color(90, 220, 120) : new Color(220, 90, 90);

			int rx = (int)(t.X * 16f - sp.X);
			int ry = (int)(t.Y * 16f - sp.Y);
			sb.Draw(px, new Rectangle(rx, ry, 16, 16), edge * 0.18f);
			sb.Draw(px, new Rectangle(rx, ry, 16, 1), edge);
			sb.Draw(px, new Rectangle(rx, ry + 15, 16, 1), edge);
			sb.Draw(px, new Rectangle(rx, ry, 1, 16), edge);
			sb.Draw(px, new Rectangle(rx + 15, ry, 1, 16), edge);
		}
		return true;
	}
}
