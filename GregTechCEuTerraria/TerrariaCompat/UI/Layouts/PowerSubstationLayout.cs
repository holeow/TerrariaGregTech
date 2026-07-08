#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock.Electric;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class PowerSubstationLayout
{
	private const int Padding = 12;
	private const int TitleH  = 14;
	private const int BodyW   = 190;
	private const int BodyH   = 120;

	public static MachineUILayout Build(PowerSubstationMachine machine)
	{
		var layout = new MachineUILayout
		{
			Width  = Padding + BodyW + Padding,
			Height = Padding + TitleH + BodyH + Padding,
			Title  = machine.DisplayName,
		};

		layout.Widgets.Add(new MultiLineDynamicLabelWidgetSpec(
			X: Padding, Y: Padding + TitleH,
			Getter: () => machine.BuildPanelLines(),
			Width: BodyW, Height: BodyH));

		return layout;
	}
}
