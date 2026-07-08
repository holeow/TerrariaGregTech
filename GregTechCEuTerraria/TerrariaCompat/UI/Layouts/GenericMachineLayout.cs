#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;


public static class GenericMachineLayout
{
	private const int SlotStride = 22;
	private const int GroupPad   = 4;
	private const int MaxCols    = 3;
	private const int ArrowSize  = 22;
	private const int ArrowGap   = 40;

	public static MachineUILayout Build(WorkableTieredMachine m, string title)
	{
		var inputEntries  = BuildEntries(itemCount: m.InputSlots,  fluidCount: m.InputFluidTanks);
		var outputEntries = BuildEntries(itemCount: m.OutputSlots, fluidCount: m.OutputFluidTanks);

		var (inW, inH)   = MeasureGroup(inputEntries);
		var (outW, outH) = MeasureGroup(outputEntries);

		int groupH    = System.Math.Max(inH, outH);

		int circuitColumnHeight = m.UsesCircuit ? SlotStride + 4 + ArrowSize : ArrowSize;
		int templateW = inW + ArrowGap + outW;
		int templateH = System.Math.Max(groupH, circuitColumnHeight);

		int inputsBaseX  = 0;
		int inputsBaseY  = 40 + (templateH - inH) / 2;
		int outputsBaseX = inW + ArrowGap;
		int outputsBaseY = 40 + (templateH - outH) / 2;
		int arrowX       = inW + (ArrowGap - ArrowSize) / 2;
		int arrowY       = 40 + (templateH + circuitColumnHeight) / 2 - ArrowSize;

		int circuitX = arrowX + (ArrowSize - SlotStride) / 2;
		int circuitY = arrowY - SlotStride - 4;
		int energyX  = templateW + 6;
		int energyW  = 18;
		int energyH  = System.Math.Max(SlotStride * 2, templateH - 4);

		int leftPad   = 12;
		int rightPad  = 12;
		int totalW    = leftPad + templateW + 6 + energyW + rightPad;
		int footerY   = 40 + templateH + 6;
		string? byproductWarn = OutputLimitWarning.Text(m, m.OutputSlots);
		int warnY     = footerY + 16;
		int totalH    = byproductWarn != null ? warnY + 12 : footerY + 16;

		var layout = new MachineUILayout
		{
			Width  = totalW,
			Height = totalH,
			Title  = title,
		};

		EmitGroup(layout, m, inputEntries,
			baseX: leftPad + inputsBaseX,
			baseY: inputsBaseY,
			slotStartIndex: 0,
			fluidTankStartIndex: 0,
			isOutput: false,
			sectionLabel: "Input");

		EmitGroup(layout, m, outputEntries,
			baseX: leftPad + outputsBaseX,
			baseY: outputsBaseY,
			slotStartIndex: 0,
			fluidTankStartIndex: 0,
			isOutput: true,
			sectionLabel: "Output");

		layout.Widgets.Add(new ProgressArrowWidgetSpec(X: leftPad + arrowX, Y: arrowY, Progress: () => m.Progress01));

		if (m.UsesCircuit)
		{
			layout.Widgets.Add(new CircuitButtonWidgetSpec(X: leftPad + circuitX, Y: circuitY));
		}

		layout.Widgets.Add(new EnergyBarWidgetSpec(X: leftPad + energyX, Y: 40, Width: energyW, Height: energyH));

		layout.Widgets.Add(new DynamicLabelWidgetSpec(X: leftPad, Y: footerY, Getter: () =>
		{
			if (!m.IsRunning)
				return RecipeStatusText.StatusLine(m.Recipe);
			string status = $"Running  {m.ActiveEuPerTick} EU/t";
			if (m.ActiveOverclock > 0) status += $" OCx{m.ActiveOverclock}";
			status += $"  {RecipeStatusText.FormatProgressSeconds(m.Recipe.GetProgress(), m.Recipe.GetMaxProgress())}";
			return status;
		}, Scale: 0.7f));

		if (byproductWarn != null)
			layout.Widgets.Add(new LabelWidgetSpec(X: leftPad, Y: warnY, Text: byproductWarn,
				Scale: 0.6f, Color: OutputLimitWarning.Color));

		return layout;
	}

	private readonly record struct Entry(int Count, bool IsFluid);

	private static List<Entry> BuildEntries(int itemCount, int fluidCount)
	{
		var list = new List<Entry>();
		if (itemCount  > 0) list.Add(new Entry(itemCount,  IsFluid: false));
		if (fluidCount > 0) list.Add(new Entry(fluidCount, IsFluid: true));
		return list;
	}

	private static (int W, int H) MeasureGroup(List<Entry> entries)
	{
		int maxCount = 0;
		int totalRows = 0;
		foreach (var e in entries)
		{
			if (e.Count > maxCount) maxCount = System.Math.Min(e.Count, MaxCols);
			totalRows += (e.Count + MaxCols - 1) / MaxCols;
		}
		if (maxCount == 0) return (0, 0);
		return (maxCount * SlotStride + 2 * GroupPad,
		        totalRows * SlotStride + 2 * GroupPad);
	}

	private static void EmitGroup(MachineUILayout layout, WorkableTieredMachine m,
		List<Entry> entries, int baseX, int baseY,
		int slotStartIndex, int fluidTankStartIndex, bool isOutput, string sectionLabel)
	{
		if (entries.Count == 0) return;

		layout.Widgets.Add(new LabelWidgetSpec(X: baseX + GroupPad, Y: baseY - 14, Text: sectionLabel, Scale: 0.7f));

		int index = 0;
		int itemSlotCursor = slotStartIndex;
		int fluidTankCursor = fluidTankStartIndex;
		foreach (var e in entries)
		{
			for (int s = 0; s < e.Count; s++)
			{
				int col = index % MaxCols;
				int row = index / MaxCols;
				int x = baseX + GroupPad + col * SlotStride;
				int y = baseY + GroupPad + row * SlotStride;
				if (e.IsFluid)
				{
					layout.Widgets.Add(new FluidSlotWidgetSpec(X: x, Y: y,
						Width: SlotStride, Height: SlotStride,
						Direction: isOutput ? IO.OUT : IO.IN,
						TankIndex: fluidTankCursor++));
				}
				else
				{
					var group = isOutput
						? TerrariaCompat.Machine.SlotGroup.InventoryOutput
						: TerrariaCompat.Machine.SlotGroup.InventoryInput;
					layout.Widgets.Add(new SlotWidgetSpec(X: x, Y: y,
						Group: group, SlotIndex: itemSlotCursor++));
				}
				index++;
			}
			index += (MaxCols - (index % MaxCols)) % MaxCols;
		}
	}
}
