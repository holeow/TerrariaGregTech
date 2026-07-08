#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Items.Cables;
using GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Machine;

public sealed class MachineHoverModPlayer : ModPlayer
{
	private readonly List<string> _buffer = new();

	public override void PostUpdate()
	{
		if (Player.whoAmI != Main.myPlayer) return;
		if (Player.mouseInterface || Main.gameMenu) return;
		if (Player.HeldItem?.ModItem is WireItem) return;

		int x = (int)(Main.MouseWorld.X / 16);
		int y = (int)(Main.MouseWorld.Y / 16);
		if (x <= 0 || x >= Main.maxTilesX - 1 || y <= 0 || y >= Main.maxTilesY - 1) return;

		if (MachineCellResolver.TryFindMachineAt(x, y, out var machine))
		{
			_buffer.Clear();
			machine.AppendTooltip(_buffer);
			if (_buffer.Count > 0)
				TerrariaCompat.UI.WorldHoverTooltip.Set(string.Join("\n", _buffer));
		}

		if (MultiblockPreviewHover.TryFind(x, y, out var controller, out _, out var predicate)
			&& !predicate.IsController)
		{
			_buffer.Clear();
			MultiblockPreviewHover.AppendTooltip(_buffer, controller, predicate, x, y);
			if (_buffer.Count > 0)
				TerrariaCompat.UI.WorldHoverTooltip.Set(string.Join("\n", _buffer),
					TerrariaCompat.UI.WorldHoverTooltip.HoverPriority.Multi);
		}
	}
}
