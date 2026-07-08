#nullable enable
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.TerrariaCompat.Tiles.Machines;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class DrumLayout
{
	public static MachineUILayout Build(DrumMachine drum) => new()
	{
		Width = 140,
		Height = 96,
		Title = drum.DisplayName,

		Widgets =
		{
			new LabelWidgetSpec(X: 12, Y: 28, Text: "Fluid Amount", Scale: 0.8f),
			new DynamicLabelWidgetSpec(X: 12, Y: 42, Getter: () =>
				$"{drum.GetTank(0).Amount:N0} / {drum.Capacity:N0} mB"),
			new DynamicLabelWidgetSpec(X: 12, Y: 56, Getter: () =>
				drum.GetTank(0).Type?.DisplayName ?? "(no fluid)"),
			new LabelWidgetSpec(X: 12, Y: 70, Text: "qol: output side!", Scale: 0.8f),

			new FluidSlotWidgetSpec(X: 100, Y: 26, Width: 18, Height: 60,
				Direction: IO.BOTH, TankIndex: 0),
		},
	};
}
