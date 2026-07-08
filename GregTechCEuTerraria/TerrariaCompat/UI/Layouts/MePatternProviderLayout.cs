#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Items.Patterns;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class MePatternProviderLayout
{
	public static MachineUILayout Build(PatternProviderMachine machine)
	{
		var nameField = new UITextField(
			current: () => machine.CustomName,
			onConfirm: txt => MachineActions.Send(new MePatternProviderRenameAction(txt), machine),
			maxLength: 32,
			filter: ch => !char.IsControl(ch),
			placeholder: machine.DefaultName());

		var settings = new MachineSettingsPanel(184, new Terraria.UI.UIElement[]
		{
			MachineSettingsPanel.Toggle(184, () => machine.Blocking ? "Blocking: ON" : "Blocking: OFF",
				() => MachineActions.Send(new MePatternProviderBlockingAction(!machine.Blocking), machine),
				"Blocking Mode: when ON, won't push a pattern's inputs into an adjacent\n"
					+ "machine that still holds them (waits for the previous batch to finish)",
				() => machine.Blocking),
			MachineSettingsPanel.Toggle(184, () => "Lock: " + LockModeLabel(machine.LockMode),
				() => MachineActions.Send(new MePatternProviderLockModeAction(NextLockMode(machine.LockMode)), machine),
				"Block next request until some condition",
				() => machine.LockMode != LockCraftingMode.None),
			MachineSettingsPanel.Toggle(184, () => machine.ShowInAccessTerminal ? "In Access Terminal: SHOWN" : "In Access Terminal: HIDDEN",
				() => MachineActions.Send(new MePatternProviderShowInTermAction(!machine.ShowInAccessTerminal), machine),
				"Whether this provider appears in the ME Pattern Access Terminal",
				() => machine.ShowInAccessTerminal),
		});

		var pushSelector = new UIDirectionSelector(
			UIDirectionSelector.Mode.Items,
			() => machine.PushDirection,
			dir => MachineActions.Send(new MePatternProviderPushDirAction(dir), machine),
			() => true,
			_ => { },
			label: "Push side",
			autoOutputToggleable: false);

		const int settingsW = 184, gap = 10, padW = UIDirectionSelector.ClusterSize;
		int rowH = System.Math.Max(settings.PixelHeight, padW + 14);
		var settingsRow = new Terraria.UI.UIElement
		{
			Width = Terraria.UI.StyleDimension.FromPixels(settingsW + gap + padW),
			Height = Terraria.UI.StyleDimension.FromPixels(rowH),
		};
		settings.Left = Terraria.UI.StyleDimension.FromPixels(0);
		settings.Top = Terraria.UI.StyleDimension.FromPixels(0);
		settingsRow.Append(settings);

		settingsRow.Append(new Terraria.GameContent.UI.Elements.UIText("Push side", 0.55f)
		{
			Left = Terraria.UI.StyleDimension.FromPixels(settingsW + gap),
			Top = Terraria.UI.StyleDimension.FromPixels(0),
		});

		pushSelector.Left = Terraria.UI.StyleDimension.FromPixels(settingsW + gap);
		pushSelector.Top = Terraria.UI.StyleDimension.FromPixels(14);
		settingsRow.Append(pushSelector);

		var layout = new MachineUILayout
		{
			Width = 240,
			Height = 162,
			Title = machine.DisplayName,
			TopPanel = new MachineUILayout.SatellitePanelSpec(settingsRow, settingsW + gap + padW, rowH, "Settings"),
		};

		layout.Widgets.Add(new PrebuiltWidgetSpec(X: 10, Y: 24, nameField, Width: 220, Height: 16));

		layout.Widgets.Add(new LabelWidgetSpec(X: 10, Y: 46, Text: "Patterns (put encoded patterns in empty slots)", Scale: 0.8f));

		for (int i = 0; i < PatternProviderMachine.PatternSlots; i++)
		{
			int slot = i;
			layout.Widgets.Add(new SlotWidgetSpec(
				X: 10 + (i % 9) * 24, Y: 60 + (i / 9) * 24,
				Group: SlotGroup.InventoryInput, SlotIndex: i,
				Invalid: () => IsInvalidPattern(machine, slot)));
		}

		return layout;
	}

	private static LockCraftingMode NextLockMode(LockCraftingMode m) => m switch
	{
		LockCraftingMode.None => LockCraftingMode.LockUntilPulse,
		LockCraftingMode.LockUntilPulse => LockCraftingMode.LockUntilResult,
		_ => LockCraftingMode.None,
	};

	private static string LockModeLabel(LockCraftingMode m) => m switch
	{
		LockCraftingMode.LockUntilPulse => "Red wire signal",
		LockCraftingMode.LockUntilResult => "Until result returns",
		_ => "None",
	};

	private static bool IsInvalidPattern(PatternProviderMachine machine, int slot)
	{
		var slots = machine.GetSlotGroup(SlotGroup.InventoryInput);
		if (slots == null || slot < 0 || slot >= slots.Length) return false;
		var it = slots[slot];
		if (it.IsAir || it.ModItem is not EncodedPatternItem e || e.Pattern == null) return false;
		return !machine.CanFulfill(e.Pattern);
	}
}
