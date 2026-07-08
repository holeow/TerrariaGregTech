#nullable enable
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net;

public static class MeStationCraftEffectPacket
{
	public static void Emit(int tileX, int tileY)
	{
		PlayLocal(tileX, tileY);
		if (Main.netMode != NetmodeID.Server) return;
		var p = NetRouter.NewPacket(PacketType.MeStationCraftEffect);
		p.Write((short)tileX);
		p.Write((short)tileY);
		p.Send();
	}

	public static void HandleOnClient(BinaryReader r)
	{
		PlayLocal(r.ReadInt16(), r.ReadInt16());
	}

	public static void PlayLocal(int tileX, int tileY)
	{
		if (Main.dedServ) return;
		var center = new Vector2(tileX * 16f + 8f, tileY * 16f + 2f);

		for (int d = 0; d < 16; d++)
		{
			var dd = Dust.NewDustPerfect(
				center + new Vector2(Main.rand.NextFloat(-9f, 9f), Main.rand.NextFloat(-3f, 5f)),
				DustID.GoldFlame,
				new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), -Main.rand.NextFloat(0.9f, 2.8f)),
				0, default, Main.rand.NextFloat(1.2f, 1.9f));
			dd.noGravity = true;
		}
		for (int d = 0; d < 6; d++)
			Dust.NewDustPerfect(
				center + new Vector2(Main.rand.NextFloat(-7f, 7f), 0f),
				DustID.Smoke,
				new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(0.6f, 1.6f)),
				90, default, 1.4f);
		Lighting.AddLight(center, 0.7f, 0.55f, 0.15f);
	}
}
