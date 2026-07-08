#nullable enable
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class MeInterfaceLayout
{
	public static MachineUILayout Build(MeInterfaceMachine iface)
	{
		const int slotSpan = 24;
		const int slotSize = 22;

		var settings = new MachineSettingsPanel(184, new Terraria.UI.UIElement[]
		{
			new UINumericStepper("Priority", () => iface.Priority,
				v => MachineActions.Send(MeInterfaceAction.SetPriority((int)v), iface),
				-9999, 9999, 1, 48, 184),
			MachineSettingsPanel.Toggle(184,
				() => iface.CraftMissing ? "Craft Missing: ON" : "Craft Missing: OFF",
				() => MachineActions.Send(MeInterfaceAction.ToggleCraft(), iface),
				"Request autocrafting for configured items the network can't supply.",
				() => iface.CraftMissing),
		});

		var layout = new MachineUILayout
		{
			Width = 240,
			Height = 120,
			Title = iface.DisplayName,
			TopPanel = new MachineUILayout.SatellitePanelSpec(settings, 184, settings.PixelHeight, "Settings"),
		};

		layout.Widgets.Add(new LabelWidgetSpec(X: 10, Y: 24, Text: "Stock", Scale: 0.8f));
		for (int i = 0; i < MeInterfaceMachine.Slots; i++)
			layout.Widgets.Add(new PrebuiltWidgetSpec(
				X: 10 + i * slotSpan, Y: 36,
				new UIMeInterfaceConfigSlot(iface, i, slotSize), slotSize, slotSize));

		layout.Widgets.Add(new LabelWidgetSpec(X: 10, Y: 62, Text: "Stocked", Scale: 0.8f));
		for (int i = 0; i < MeInterfaceMachine.Slots; i++)
			layout.Widgets.Add(new PrebuiltWidgetSpec(
				X: 10 + i * slotSpan, Y: 74,
				new UIMeInterfaceStorageSlot(iface, i, slotSize), slotSize, slotSize));

		return layout;
	}
}
