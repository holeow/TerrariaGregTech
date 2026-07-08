#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Tiles.Machines;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class SuperChestLayout
{
	public static MachineUILayout Build(SuperChestTileEntity chest) => new()
	{
		Width = 140,
		Height = 96,
		Title = chest.DisplayName,

		Widgets =
		{
			new LabelWidgetSpec(X: 12, Y: 28, Text: "Items Stored", Scale: 0.8f),
			new DynamicLabelWidgetSpec(X: 12, Y: 42, Getter: () =>
			{
				var s = chest.StoredItem;
				return s.IsAir ? "0" : $"{chest.StoredAmount:N0} / {chest.MaxAmount:N0}";
			}),
			new DynamicLabelWidgetSpec(X: 12, Y: 56, Getter: () =>
			{
				var s = chest.StoredItem;
				return s.IsAir ? "(empty)" : s.Name;
			}),

			new SuperChestSlotWidgetSpec(X: 100, Y: 28),

			new ToggleButtonWidgetSpec(
				X: 100, Y: 54,
				IconAssetPath: "GregTechCEuTerraria/Content/Textures/gui/widget/button_output",
				Getter: () => false,
				Setter: _ => TerrariaCompat.Net.Actions.MachineActions.Send(new TerrariaCompat.Net.Actions.ChestAction(TerrariaCompat.Net.Actions.ChestAction.Op.Dump, true), chest),
				Tooltip: "Take a stack of the stored item"),

			new ToggleButtonWidgetSpec(
				X: 12, Y: 72,
				IconAssetPath: "GregTechCEuTerraria/Content/Textures/gui/widget/button_lock",
				Getter: () => chest.IsLocked,
				Setter: v => TerrariaCompat.Net.Actions.MachineActions.Send(new TerrariaCompat.Net.Actions.ChestAction(TerrariaCompat.Net.Actions.ChestAction.Op.Locked, v), chest),
				Tooltip: "Lock to current item type"),

			new ToggleButtonWidgetSpec(
				X: 32, Y: 72,
				IconAssetPath: "GregTechCEuTerraria/Content/Textures/gui/widget/button_void_partial",
				Getter: () => chest.IsVoiding,
				Setter: v => TerrariaCompat.Net.Actions.MachineActions.Send(new TerrariaCompat.Net.Actions.ChestAction(TerrariaCompat.Net.Actions.ChestAction.Op.Voiding, v), chest),
				Tooltip: "Void overflow (accept then discard)"),
		},
	};
}
