#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MePatternProviderBlockingAction : IMachineAction
{
	public PacketType Type => PacketType.MePatternProviderBlocking;

	private bool _blocking;

	public MePatternProviderBlockingAction() { }
	public MePatternProviderBlockingAction(bool blocking) => _blocking = blocking;

	public void Write(BinaryWriter w) => w.Write(_blocking);
	public void Read(BinaryReader r) => _blocking = r.ReadBoolean();

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is PatternProviderMachine provider)
			provider.Blocking = _blocking;
	}
}
