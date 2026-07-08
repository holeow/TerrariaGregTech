#nullable enable
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock.Primitive;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class CokeOvenLayout
{
	public static MachineUILayout Build(CokeOvenMachine m) => new()
	{
		Width  = 176,
		Height = 128,
		Title  = m.DisplayName,

		Widgets =
		{
			new LabelWidgetSpec(X: 12, Y: 26, Text: "Input", Scale: 0.75f),
			new SlotWidgetSpec (X: 12, Y: 40, Group: SlotGroup.InventoryInput,  SlotIndex: 0),

			new ProgressArrowWidgetSpec(X: 50, Y: 44, Progress: () => (float)m.Recipe.GetProgressPercent()),

			new LabelWidgetSpec(X: 84, Y: 26, Text: "Output", Scale: 0.75f),
			new SlotWidgetSpec (X: 84, Y: 40, Group: SlotGroup.InventoryOutput, SlotIndex: 0),

			new LabelWidgetSpec    (X: 116, Y: 26, Text: "Creosote", Scale: 0.7f),
			new FluidSlotWidgetSpec(X: 116, Y: 40, Width: 22, Height: 48, Direction: IO.OUT, TankIndex: 0, FillBar: true),

			new DynamicLabelWidgetSpec(X: 8, Y: 70,
				Getter: () => m.GetCapacity(0) > 0 && m.GetTank(0).Amount * 2 >= m.GetCapacity(0)
					? "Creosote overflows?\nTry Liquid Boiler or Void Cover"
					: "",
				Scale: 0.6f, Color: new Microsoft.Xna.Framework.Color(255, 80, 80)),

			new DynamicLabelWidgetSpec(X: 12, Y: 106,
				Getter: () => RecipeStatusText.StatusLineForMulti(m, m.Recipe), Scale: 0.7f),
		},
	};
}
