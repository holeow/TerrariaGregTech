#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MePatternProviderRenameAction : IMachineAction
{
	public PacketType Type => PacketType.MePatternProviderRename;

	private string _name = "";

	public MePatternProviderRenameAction() { }
	public MePatternProviderRenameAction(string name) => _name = name ?? "";

	public void Write(BinaryWriter w) => w.Write(_name);
	public void Read(BinaryReader r) => _name = r.ReadString();

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is PatternProviderMachine provider)
			provider.ApplySetName(_name.Length > 32 ? _name.Substring(0, 32) : _name);
	}
}
