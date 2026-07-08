#nullable enable
using GregTechCEuTerraria.Api.Machine.Feature;
using GregTechCEuTerraria.Common.Recipe;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class RecipeBrowserLauncher
{
	public const string ArrowTooltip = "Show recipes for machine";
	public const string ButtonLabel = "Show recipes";

	public static string? StationFor(MetaMachine? entity)
	{
		switch (entity)
		{
			case IRecipeLogicMachine proc:
				return proc.GetRecipeType()?.RegistryName;
			case MultiblockControllerMachine ctrl
				when ctrl.Definition?.RecipeType is { } rt && rt != GTRecipeTypes.DUMMY:
				return rt.RegistryName;
			default:
				return null;
		}
	}

	public static bool CanOpen(MetaMachine? entity) => !string.IsNullOrEmpty(StationFor(entity));

	public static void OpenForMachine(MetaMachine? entity)
	{
		var station = StationFor(entity);
		if (!string.IsNullOrEmpty(station))
			GlobalRecipeBrowserSystem.OpenStation(station!);
	}
}
