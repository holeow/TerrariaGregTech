#nullable enable
using System.Collections.Generic;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class MachineUILayout
{
	public int Width { get; init; } = 176;
	public int Height { get; init; } = 166;
	public string Title { get; init; } = "";
	public float Scale { get; init; } = 2.0f;

	public List<WidgetSpec> Widgets { get; init; } = new();

	public System.Action<MachineUIState, TerrariaCompat.Machine.MetaMachine>? BuildOverride { get; init; }

	public SatellitePanelSpec? LeftPanel { get; init; }
	public SatellitePanelSpec? TopPanel { get; init; }
	public SatellitePanelSpec? BottomPanel { get; init; }

	public sealed record SatellitePanelSpec(UIElement Element, int Width, int Height, string Title = "");
}
