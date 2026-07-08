#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using GregTechCEuTerraria.TerrariaCompat.Tiles.Machines.Creative;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class CreativeChestLayout
{
	public static MachineUILayout Build(CreativeChestTileEntity chest) => new()
	{
		Width = 140,
		Height = 90,
		Title = chest.DisplayName,

		Widgets =
		{
			new LabelWidgetSpec(X: 12, Y: 24, Text: "Source", Scale: 0.8f),
			new CreativeSourceItemSlotWidgetSpec(
				X: 12, Y: 36,
				Getter: () => chest.StoredItem,
				Setter: item => MachineActions.Send(CreativeChestSetAction.SetSourceType(item), chest)),

			new NumericStepperWidgetSpec(
				X: 40, Y: 36,
				Label: "items/cycle:",
				Getter: () => chest.ItemsPerCycle,
				Setter: v => MachineActions.Send(CreativeChestSetAction.ItemsPerCycle((int)v), chest),
				Min: 1, Max: int.MaxValue, Step: 1, LabelWidth: 72, Width: 88),
			new NumericStepperWidgetSpec(
				X: 40, Y: 56,
				Label: "ticks/cycle:",
				Getter: () => chest.TicksPerCycle,
				Setter: v => MachineActions.Send(CreativeChestSetAction.TicksPerCycle((int)v), chest),
				Min: 1, Max: int.MaxValue, Step: 1, LabelWidth: 72, Width: 88),
		},
	};
}
