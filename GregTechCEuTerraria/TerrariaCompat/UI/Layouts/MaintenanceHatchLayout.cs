#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock.Part;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class MaintenanceHatchLayout
{
	private const int Padding = 12;
	private const int TitleH  = 14;

	public static MachineUILayout Build(MaintenanceHatchPartMachine hatch)
	{
		const int ReadoutW = 160;
		int contentH = 32;

		var layout = new MachineUILayout
		{
			Width  = Padding + ReadoutW + Padding,
			Height = Padding + TitleH + contentH + Padding,
			Title  = hatch.DisplayName,
		};

		int baseY = Padding + TitleH;

		layout.Widgets.Add(new DynamicLabelWidgetSpec(X: Padding, Y: baseY, Getter: () =>
		{
			int missing = ((Api.Machine.Feature.Multiblock.IMaintenanceMachine)hatch).GetNumMaintenanceProblems();
			return missing == 0 ? "No problems" : $"Problems: {missing} / 6";
		}, Scale: 0.85f));
		if (hatch.IsConfigurable)
		{
			layout.Widgets.Add(new DynamicLabelWidgetSpec(X: Padding, Y: baseY + 14, Getter: () =>
				$"Duration x{hatch.GetDurationMultiplier():F2}", Scale: 0.7f));
		}

		return layout;
	}
}
