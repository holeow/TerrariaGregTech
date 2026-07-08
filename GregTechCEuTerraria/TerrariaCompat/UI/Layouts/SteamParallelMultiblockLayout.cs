#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Machine.Multiblock;
using GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock.Steam;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class SteamParallelMultiblockLayout
{
	private const int Padding = 12;
	private const int TitleH  = 14;
	private const int BodyW   = 190;
	private const int BodyH   = 120;

	public static MachineUILayout Build(SteamParallelMultiblockMachine machine)
	{
		var layout = new MachineUILayout
		{
			Width  = Padding + BodyW + Padding,
			Height = Padding + TitleH + BodyH + Padding,
			Title  = machine.DisplayName,
		};
		int baseY = Padding + TitleH;

		layout.Widgets.Add(new MultiLineDynamicLabelWidgetSpec(
			X: Padding, Y: baseY,
			Getter: () => BuildDisplayLines(machine),
			Width: BodyW, Height: BodyH));

		return layout;
	}

	private static List<string> BuildDisplayLines(SteamParallelMultiblockMachine machine)
	{
		var lines = new List<string>();
		var recipeLogic = machine.Recipe;

		if (!machine.IsFormed)
			lines.Add(Machine.RecipeStatusText.StatusLineForMulti(machine, recipeLogic));

		foreach (var part in machine.GetParts())
			part.AddMultiText(lines);

		if (machine.IsFormed)
		{
			long capacity = machine.SteamCapacity;
			if (capacity > 0)
			{
				lines.Add(MultiblockDisplayText.Tr("gtceu.multiblock.steam.steam_stored",
					machine.SteamStored.ToString("N0"), capacity.ToString("N0")));
			}

			if (!recipeLogic.IsWorkingEnabled())
			{
				lines.Add(MultiblockDisplayText.Tr("gtceu.multiblock.work_paused"));
			}
			else if (recipeLogic.IsActive())
			{
				lines.Add(MultiblockDisplayText.Tr("gtceu.multiblock.running"));
				if (machine.MaxParallels > 1)
					lines.Add(MultiblockDisplayText.Tr("gtceu.multiblock.parallel",
						machine.MaxParallels.ToString("N0")));
				int currentProgress = (int)(recipeLogic.GetProgressPercent() * 100);
				double maxInSec     = recipeLogic.GetMaxProgress() / 20.0;
				double currentInSec = recipeLogic.GetProgress()    / 20.0;
				lines.Add(MultiblockDisplayText.Tr("gtceu.multiblock.progress",
					currentInSec.ToString("0.00"), maxInSec.ToString("0.00"),
					currentProgress));
			}
			else
			{
				lines.Add(MultiblockDisplayText.Tr("gtceu.multiblock.idling"));
			}

			if (recipeLogic.IsWaiting())
				lines.Add(MultiblockDisplayText.Tr("gtceu.multiblock.steam.low_steam"));
		}

		return lines;
	}
}
