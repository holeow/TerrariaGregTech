#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class PowerToggleAction : IMachineAction
{
	public PacketType Type => PacketType.PowerToggle;

	private bool _enabled;

	public PowerToggleAction() { }
	public PowerToggleAction(bool enabled) { _enabled = enabled; }

	public void Write(BinaryWriter w) => w.Write(_enabled);
	public void Read (BinaryReader r) => _enabled = r.ReadBoolean();

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		entity.WorkingEnabled = _enabled;
	}
}
