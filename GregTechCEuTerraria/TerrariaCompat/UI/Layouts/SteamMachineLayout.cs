#nullable enable
using System;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Machine.Steam;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class SteamMachineLayout
{
	private const int SlotStride = 22;
	private const int GroupPad   = 4;
	private const int MaxCols    = 3;
	private const int ArrowSize  = 22;
	private const int ArrowGap   = 40;

	public static MachineUILayout Build(SimpleSteamMachine m, string title)
	{
		int inCount  = m.InputSlots;
		int outCount = m.OutputSlots;

		var (inW,  inH)  = MeasureItems(inCount);
		var (outW, outH) = MeasureItems(outCount);

		int circuitColumnHeight = m.UsesCircuit ? SlotStride + 4 + ArrowSize : ArrowSize;
		int templateH = Math.Max(Math.Max(inH, outH), circuitColumnHeight);
		int templateW = inW + ArrowGap + outW;

		int inputsBaseX  = 0;
		int outputsBaseX = inW + ArrowGap;
		int arrowX       = inW + (ArrowGap - ArrowSize) / 2;
		const int Top = 40;
		int arrowY       = Top + (templateH + circuitColumnHeight) / 2 - ArrowSize;

		int leftPad = 12;
		int steamX  = templateW + 6;
		int steamW  = 22;
		int steamH  = Math.Max(SlotStride * 2, templateH - 4);
		int totalW  = leftPad + templateW + 6 + steamW + 12;
		int footerY = Top + templateH + 6;
		string? byproductWarn = OutputLimitWarning.Text(m, outCount);
		int warnY   = footerY + 16;
		int totalH  = byproductWarn != null ? warnY + 12 : footerY + 14;

		var layout = new MachineUILayout { Width = totalW, Height = totalH, Title = title };

		EmitItemGrid(layout, inCount,  leftPad + inputsBaseX,  Top + (templateH - inH)  / 2, isOutput: false, "Input");
		EmitItemGrid(layout, outCount, leftPad + outputsBaseX, Top + (templateH - outH) / 2, isOutput: true,  "Output");

		layout.Widgets.Add(new ProgressArrowWidgetSpec(X: leftPad + arrowX, Y: arrowY, Progress: () => m.Progress01));

		layout.Widgets.Add(new FluidSlotWidgetSpec(X: leftPad + steamX, Y: Top,
			Width: steamW, Height: steamH, Direction: IO.IN, TankIndex: 0, FillBar: true));

		layout.Widgets.Add(new DynamicLabelWidgetSpec(X: leftPad, Y: footerY,
			Getter: () => RecipeStatusText.StatusLine(m.Recipe, "Running"), Scale: 0.7f));

		if (byproductWarn != null)
			layout.Widgets.Add(new LabelWidgetSpec(X: leftPad, Y: warnY, Text: byproductWarn,
				Scale: 0.6f, Color: OutputLimitWarning.Color));

		return layout;
	}

	private static (int W, int H) MeasureItems(int count)
	{
		if (count <= 0) return (0, 0);
		int cols = Math.Min(count, MaxCols);
		int rows = (count + MaxCols - 1) / MaxCols;
		return (cols * SlotStride + 2 * GroupPad, rows * SlotStride + 2 * GroupPad);
	}

	private static void EmitItemGrid(MachineUILayout layout, int count,
		int baseX, int baseY, bool isOutput, string sectionLabel)
	{
		if (count <= 0) return;
		layout.Widgets.Add(new LabelWidgetSpec(X: baseX + GroupPad, Y: baseY - 14, Text: sectionLabel, Scale: 0.7f));

		var group = isOutput ? SlotGroup.InventoryOutput : SlotGroup.InventoryInput;
		for (int s = 0; s < count; s++)
		{
			int col = s % MaxCols;
			int row = s / MaxCols;
			layout.Widgets.Add(new SlotWidgetSpec(
				X: baseX + GroupPad + col * SlotStride,
				Y: baseY + GroupPad + row * SlotStride,
				Group: group, SlotIndex: s));
		}
	}
}
