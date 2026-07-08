#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class ToiletAuraPacket
{
	public static void SendChange(bool add, int x, int y)
	{
		var p = NetRouter.NewPacket(PacketType.ToiletAura);
		p.Write(add);
		p.Write((short)x);
		p.Write((short)y);
		p.Send();
	}

	public static void Handle(BinaryReader r, int whoAmI)
	{
		bool add = r.ReadBoolean();
		int x = r.ReadInt16();
		int y = r.ReadInt16();
		if (Main.netMode != NetmodeID.Server)
		{
			NetHelpers.LogBadPacket("ToiletAura", "received on non-server side");
			return;
		}
		if (add) GregtechToiletAura.ServerAdd(x, y);
		else     GregtechToiletAura.ServerRemove(x, y);
	}
}
