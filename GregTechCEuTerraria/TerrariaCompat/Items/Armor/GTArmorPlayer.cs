#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Armor;

public sealed class GTArmorPlayer : ModPlayer
{
	public override void OnHurt(Player.HurtInfo info)
	{
		int dmg = info.Damage;
		if (dmg <= 0) return;
		for (int i = 0; i <= 2; i++)
		{
			if (Player.armor[i]?.ModItem is GTArmorItem a)
				a.DrainOnHit(dmg);
		}
	}

	public override void PostUpdate()
	{
		if (Player.whoAmI != Main.myPlayer) return;
		if (Player.armor[1]?.ModItem is not GTArmorItem chest || !chest.FlightReady) return;
		if (Player.mount.Active) return;
		if (Player.controlJump)
		{
			float ascentCap = Player.jumpSpeed * chest.FlightAscentMult;
			Player.velocity.Y = MathHelper.Lerp(Player.velocity.Y, -ascentCap, 0.3f);
			Player.fallStart = (int)(Player.position.Y / 16f);
			chest.DrainForFlight();
		}

		if (Player.velocity.Y != 0f)
		{
			float runCap = chest.FlightRunSpeed;
			int dir = (Player.controlRight ? 1 : 0) - (Player.controlLeft ? 1 : 0);
			if (dir != 0 && (Math.Abs(Player.velocity.X) < runCap || Math.Sign(Player.velocity.X) != dir))
				Player.velocity.X = MathHelper.Lerp(Player.velocity.X, dir * runCap, 0.12f);
		}
	}
}
