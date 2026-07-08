#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MePatternProviderShowInTermAction : IMachineAction
{
	public PacketType Type => PacketType.MePatternProviderShowInTerm;

	private bool _show;

	public MePatternProviderShowInTermAction() { }
	public MePatternProviderShowInTermAction(bool show) => _show = show;

	public void Write(BinaryWriter w) => w.Write(_show);
	public void Read(BinaryReader r) => _show = r.ReadBoolean();

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is PatternProviderMachine provider)
			provider.ShowInAccessTerminal = _show;
	}
}
