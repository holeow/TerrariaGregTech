#nullable enable
using System;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using GregTechCEuTerraria.TerrariaCompat.Tiles.Machines;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class BlockBreakerLayout
{
	public static MachineUILayout Build(BlockBreakerMachine machine)
	{
		const int Padding  = 12;
		const int EnergyW  = 18;
		const int LabelRow = 14;
		const int ModeH    = 16;

		const int StatusW = 150;
		const int StatusH = 2 * (ModeH + 4) + 3 * 14;

		int contentH = StatusH;
		int width  = Padding + EnergyW + 8 + StatusW + Padding;
		int height = Padding + LabelRow + contentH + Padding;
		int contentTop = Padding + LabelRow;

		var layout = new MachineUILayout
		{
			Width  = width,
			Height = height,
			Title  = machine.DisplayName,
		};

		int leftX = Padding;
		layout.Widgets.Add(new EnergyBarWidgetSpec(
			X: leftX, Y: contentTop, Width: EnergyW, Height: StatusH));

		int statusX = leftX + EnergyW + 8;

		layout.Widgets.Add(new TextButtonWidgetSpec(
			X: statusX, Y: contentTop,
			Label:   () => machine.Mode == BlockBreakerMachine.BreakerMode.CutTrees
				? "Mode: Cut Trees"
				: "Mode: Mine Down",
			OnLeft:  () => ToggleMode(machine),
			OnRight: () => ToggleMode(machine),
			Width: StatusW, Height: ModeH));

		layout.Widgets.Add(new TextButtonWidgetSpec(
			X: statusX, Y: contentTop + ModeH + 4,
			Label:   () => machine.ReplantEnabled ? "Replant trees: On" : "Replant trees: Off",
			OnLeft:  () => MachineActions.Send(new BlockBreakerReplantSetAction(!machine.ReplantEnabled), machine),
			OnRight: () => MachineActions.Send(new BlockBreakerReplantSetAction(!machine.ReplantEnabled), machine),
			Width: StatusW, Height: ModeH,
			Visible: () => machine.Mode == BlockBreakerMachine.BreakerMode.CutTrees));

		int textY = contentTop + 2 * (ModeH + 4);
		layout.Widgets.Add(new DynamicLabelWidgetSpec(
			X: statusX, Y: textY,
			Getter: () => machine.Mode == BlockBreakerMachine.BreakerMode.CutTrees
				? $"Area: {machine.CutWidth} wide"
				: $"Range: {machine.Range} tiles",
			Scale: 0.7f));
		long euPerTick = VoltageTiers.Voltage((VoltageTier)Math.Max(0, (int)machine.Tier - 1));
		layout.Widgets.Add(new LabelWidgetSpec(
			X: statusX, Y: textY + 14,
			Text: $"Draw: {euPerTick:N0} EU/t", Scale: 0.7f));
		layout.Widgets.Add(new DynamicLabelWidgetSpec(
			X: statusX, Y: textY + 28,
			Getter: () => machine.IsActive
				? (machine.Mode == BlockBreakerMachine.BreakerMode.CutTrees ? "Cutting..." : "Drilling...")
				: (machine.IsWorkingEnabled() ? "Idle" : "Disabled"),
			Scale: 0.7f));

		return layout;
	}

	private static void ToggleMode(BlockBreakerMachine machine)
	{
		var next = machine.Mode == BlockBreakerMachine.BreakerMode.CutTrees
			? BlockBreakerMachine.BreakerMode.MineDown
			: BlockBreakerMachine.BreakerMode.CutTrees;
		MachineActions.Send(new BlockBreakerModeSetAction(next), machine);
	}
}
