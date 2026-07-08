#nullable enable
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.TerrariaCompat.Tiles.Machines;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class SuperTankLayout
{
	public static MachineUILayout Build(SuperTankTileEntity tank) => new()
	{
		Width = 140,
		Height = 96,
		Title = tank.DisplayName,

		Widgets =
		{
			new LabelWidgetSpec(X: 12, Y: 28, Text: "Fluid Amount", Scale: 0.8f),
			new DynamicLabelWidgetSpec(X: 12, Y: 42, Getter: () =>
				tank.StoredType is null ? "0 mB" : $"{tank.StoredAmount:N0} / {tank.MaxAmount:N0} mB"),
			new DynamicLabelWidgetSpec(X: 12, Y: 56, Getter: () =>
				tank.StoredType?.DisplayName ?? "(no fluid)"),

			new FluidSlotWidgetSpec(X: 100, Y: 26, Width: 18, Height: 60,
				Direction: IO.BOTH, TankIndex: 0),

			new ToggleButtonWidgetSpec(
				X: 12, Y: 72,
				IconAssetPath: "GregTechCEuTerraria/Content/Textures/gui/widget/button_lock",
				Getter: () => tank.IsLocked,
				Setter: v => TerrariaCompat.Net.Actions.MachineActions.Send(new TerrariaCompat.Net.Actions.TankConfigSetAction(TerrariaCompat.Net.Actions.TankConfigSetAction.Field.Locked, v), tank),
				Tooltip: "Lock to current fluid type"),

			new ToggleButtonWidgetSpec(
				X: 32, Y: 72,
				IconAssetPath: "GregTechCEuTerraria/Content/Textures/gui/widget/button_void_partial",
				Getter: () => tank.IsVoiding,
				Setter: v => TerrariaCompat.Net.Actions.MachineActions.Send(new TerrariaCompat.Net.Actions.TankConfigSetAction(TerrariaCompat.Net.Actions.TankConfigSetAction.Field.Voiding, v), tank),
				Tooltip: "Void overflow (accept then discard)"),
		},
	};
}
