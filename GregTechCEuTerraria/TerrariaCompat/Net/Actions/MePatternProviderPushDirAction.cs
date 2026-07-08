#nullable enable
using System.IO;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MePatternProviderPushDirAction : IMachineAction
{
	public PacketType Type => PacketType.MePatternProviderPushDir;

	private IODirection _dir;

	public MePatternProviderPushDirAction() { }
	public MePatternProviderPushDirAction(IODirection dir) => _dir = dir;

	public void Write(BinaryWriter w) => w.Write((byte)_dir);
	public void Read(BinaryReader r) => _dir = (IODirection)r.ReadByte();

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is PatternProviderMachine provider)
			provider.ApplySetPushDirection(_dir);
	}
}
