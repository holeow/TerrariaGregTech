#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Tiles.NeonSign;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class NeonSignEditPacket
{
	public static void SendRequest(int x, int y, string text, byte colorIndex, sbyte sizeStep)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			NeonSignEntity.At(x, y)?.ApplyEdit(text, colorIndex, sizeStep);
			return;
		}
		var p = NetRouter.NewPacket(PacketType.NeonSignEdit);
		p.Write((short)x);
		p.Write((short)y);
		p.Write(text ?? "");
		p.Write(colorIndex);
		p.Write(sizeStep);
		p.Send();
	}

	public static void Handle(BinaryReader r, int whoAmI)
	{
		int x = r.ReadInt16();
		int y = r.ReadInt16();
		string text = r.ReadString();
		byte colorIndex = r.ReadByte();
		sbyte sizeStep = r.ReadSByte();

		if (Main.netMode != NetmodeID.Server)
		{
			NetHelpers.LogBadPacket("NeonSignEdit", "received on non-server side");
			return;
		}

		var sign = NeonSignEntity.At(x, y);
		if (sign is null)
		{
			NetHelpers.LogBadPacket("NeonSignEdit", $"no neon sign at ({x},{y}) from player {whoAmI}");
			return;
		}

		sign.ApplyEdit(text, colorIndex, sizeStep);
		NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null,
			sign.ID, sign.Position.X, sign.Position.Y);
	}
}
