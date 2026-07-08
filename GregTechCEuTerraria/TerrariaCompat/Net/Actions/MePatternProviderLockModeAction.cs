#nullable enable
using System.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MePatternProviderLockModeAction : IMachineAction
{
	public PacketType Type => PacketType.MePatternProviderLockMode;

	private byte _mode;

	public MePatternProviderLockModeAction() { }
	public MePatternProviderLockModeAction(LockCraftingMode mode) => _mode = (byte)mode;

	public void Write(BinaryWriter w) => w.Write(_mode);
	public void Read(BinaryReader r) => _mode = r.ReadByte();

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is PatternProviderMachine provider)
			provider.ApplySetLockMode((LockCraftingMode)_mode);
	}
}
