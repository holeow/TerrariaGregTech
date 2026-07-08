#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Tiles.Machines;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class BlockBreakerReplantSetAction : IMachineAction
{
	public PacketType Type => PacketType.BlockBreakerReplantSet;

	private bool _enabled;

	public BlockBreakerReplantSetAction() { }
	public BlockBreakerReplantSetAction(bool enabled) { _enabled = enabled; }

	public void Write(BinaryWriter w) => w.Write(_enabled);
	public void Read (BinaryReader r) => _enabled = r.ReadBoolean();

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is BlockBreakerMachine breaker)
			breaker.SetReplant(_enabled);
	}
}
