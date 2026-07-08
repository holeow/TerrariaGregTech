#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MeCraftAtTerminalAction : IMachineAction
{
	public PacketType Type => PacketType.MeCraftAtTerminal;

	private int _itemType;
	private int _count;

	public MeCraftAtTerminalAction() { }
	public MeCraftAtTerminalAction(int itemType, int count)
	{
		_itemType = itemType;
		_count = count;
	}

	public void Write(BinaryWriter w)
	{
		w.Write(_itemType);
		w.Write(_count);
	}

	public void Read(BinaryReader r)
	{
		_itemType = r.ReadInt32();
		_count = r.ReadInt32();
	}

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is not IMeCraftingHost host) return;
		if (byWhoAmI < 0 || byWhoAmI >= Main.maxPlayers) return;
		host.Crafting.CraftToHand(host.Network, _itemType, System.Math.Clamp(_count, 1, 9999), Main.player[byWhoAmI]);
	}
}
