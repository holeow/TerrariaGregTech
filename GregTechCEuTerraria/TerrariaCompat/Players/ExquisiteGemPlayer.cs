#nullable enable
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Players;

public sealed class ExquisiteGemPlayer : ModPlayer
{
	public float DamageTakenBonus;

	public override void ResetEffects()
	{
		DamageTakenBonus = 0f;
	}

	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (DamageTakenBonus > 0f)
			modifiers.FinalDamage *= 1f + DamageTakenBonus;
	}
}
