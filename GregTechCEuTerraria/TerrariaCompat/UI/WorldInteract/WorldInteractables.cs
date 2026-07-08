#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Pipelike;
using GregTechCEuTerraria.TerrariaCompat.UI.Layouts;
using GregTechCEuTerraria.TerrariaCompat.UI.MeBus;
using GregTechCEuTerraria.TerrariaCompat.UI.PipeSettings;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.UI.WorldInteract;

public readonly record struct WorldInteractable(string Label, System.Action Open);

public static class WorldInteractables
{
	public static void ProbeAt(int x, int y, List<WorldInteractable> outList)
	{
		if (MachineCellResolver.TryFindMachineAt(x, y, out var machine)
		    && MachineLayoutRegistry.Build(machine) != null)
		{
			var m = machine;
			outList.Add(new WorldInteractable(m.DisplayName, () =>
			{
				var layout = MachineLayoutRegistry.Build(m);
				if (layout != null) MachineUISystem.OpenFor(m, layout);
			}));
		}

		if (PipeSettingsSystem.IsOpenable(x, y, PipeKind.Item))
			outList.Add(new WorldInteractable("Item Pipe", () => PipeSettingsSystem.OpenFor(x, y, PipeKind.Item)));
		if (PipeSettingsSystem.IsOpenable(x, y, PipeKind.Fluid))
			outList.Add(new WorldInteractable("Fluid Pipe", () => PipeSettingsSystem.OpenFor(x, y, PipeKind.Fluid)));
		if (MeBusSettingsSystem.IsOpenable(x, y))
			outList.Add(new WorldInteractable("ME Cable", () => MeBusSettingsSystem.OpenFor(x, y)));
	}

	public static bool IsClickTool(Item it)
	{
		if (it.IsAir) return false;
		if (it.ModItem is Items.Cables.WireItem
		    or Items.Pipes.PipeItem
		    or Items.Pipes.SimpleItemPipeItem
		    or Items.Pipes.SimpleFluidPipeItem
		    or Items.Pipes.LaserPipeItem
		    or Items.MeCables.MeCableItem
		    or Items.TerminalItem) return true;
		if (it.ModItem is Items.Tools.ToolItem tool && tool.IsWireCutter) return true;
		return false;
	}
}
