#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using GregTechCEuTerraria.TerrariaCompat.Tiles.Machines.Creative;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class CreativeEnergyLayout
{
	public static MachineUILayout Build(CreativeEnergyContainerMachine cec)
	{
		var layout = new MachineUILayout
		{
			Width = 184,
			Height = 158,
			Title = cec.DisplayName,

			Widgets =
			{
				new LabelWidgetSpec(X: 8, Y: 12, Text: "Voltage:", Scale: 0.72f),
				new DynamicLabelWidgetSpec(X: 62, Y: 12, Getter: () =>
					cec.Voltage <= 0 ? "OFF" : $"{cec.Voltage:N0} EU/t", Scale: 0.7f),

				new NumericStepperWidgetSpec(
					X: 8, Y: 98,
					Label: "amps:",
					Getter: () => cec.Amps,
					Setter: v => MachineActions.Send(CreativeEnergySetAction.Amps((int)v), cec),
					Min: 0, Max: int.MaxValue, Step: 1, LabelWidth: 50, Width: 88),

				new TextButtonWidgetSpec(
					X: 8, Y: 118,
					Label: () => cec.Source ? "[Source]" : "[Sink]",
					OnLeft: () => MachineActions.Send(CreativeEnergySetAction.Source(!cec.Source), cec),
					Tooltip: "Toggle source (push) / sink (accept)",
					Width: 72, Height: 14),
				new TextButtonWidgetSpec(
					X: 96, Y: 118,
					Label: () => cec.Active ? "[ ON]" : "[OFF]",
					OnLeft: () => MachineActions.Send(CreativeEnergySetAction.Active(!cec.Active), cec),
					Tooltip: "Toggle active",
					Width: 72, Height: 14),

				new DynamicLabelWidgetSpec(
					X: 8, Y: 140,
					Getter: () => $"Avg I/O: {cec.LastAverageEnergyIOPerTick:N0} EU/t",
					Scale: 0.7f),
			},
		};

		var tiers = new List<(string Label, long Voltage)> { ("OFF", 0) };
		foreach (var info in VoltageTiers.All) tiers.Add((info.ShortName, info.Voltage));

		const int Cols = 4, BtnW = 40, BtnH = 14, GapX = 2, GapY = 3, StartX = 8, StartY = 28;
		for (int i = 0; i < tiers.Count; i++)
		{
			var (label, voltage) = tiers[i];
			int x = StartX + (i % Cols) * (BtnW + GapX);
			int y = StartY + (i / Cols) * (BtnH + GapY);
			layout.Widgets.Add(new TextButtonWidgetSpec(
				X: x, Y: y,
				Label: () => label,
				OnLeft: () => MachineActions.Send(CreativeEnergySetAction.Voltage(voltage), cec),
				Tooltip: voltage <= 0 ? "Off (no voltage)" : $"{label}  {voltage:N0} EU/t",
				Width: BtnW, Height: BtnH,
				IsActive: () => VoltageMatches(cec.Voltage, voltage)));
		}

		return layout;
	}

	private static bool VoltageMatches(long current, long tierVoltage) =>
		tierVoltage <= 0 ? current <= 0 : current == tierVoltage;
}
