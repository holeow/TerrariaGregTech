#nullable enable
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.UI.Terminal;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Layouts;

public static class MeModularTerminalLayout
{
	public static MachineUILayout Build(MeModularTerminalMachine term)
	{
		return new MachineUILayout
		{
			Title = term.DisplayName,
			BuildOverride = (state, machine) =>
				MeModularTerminalHud.Build(state, (MeModularTerminalMachine)machine),
		};
	}
}
