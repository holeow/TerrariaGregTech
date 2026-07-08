#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Tiles.Machines;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class BlockBreakerModeSetAction : IMachineAction
{
	public PacketType Type => PacketType.BlockBreakerModeSet;

	private byte _mode;

	public BlockBreakerModeSetAction() { }
	public BlockBreakerModeSetAction(BlockBreakerMachine.BreakerMode mode) { _mode = (byte)mode; }

	public void Write(BinaryWriter w) => w.Write(_mode);
	public void Read (BinaryReader r) => _mode = r.ReadByte();

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is BlockBreakerMachine breaker)
			breaker.SetMode((BlockBreakerMachine.BreakerMode)_mode);
	}
}
