#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Bosses.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Bosses.SoulDistiller;

// The Soul Distiller - a hardmode, pre-mechanical-boss worm
[AutoloadBossHead]
public class SoulDistiller : SoulDistillerHeadBase
{
	private const int FractionSegmentCount = 28;

	private bool _split;

	public override string Texture => "GregTechCEuTerraria/Content/Textures/block/casings/solid/machine_casing_clean_stainless_steel";
	public override string BossHeadTexture => "GregTechCEuTerraria/Content/Textures/block/casings/solid/machine_casing_clean_stainless_steel";

	protected override int BodyType => ModContent.NPCType<SoulDistillerBody>();
	protected override int TailType => ModContent.NPCType<SoulDistillerTail>();
	protected override int SegmentCount => 120;

	protected override WormMovementConfig MoveConfig => new()
	{
		MaxSpeed = 12.8f,
		Acceleration = 0.22f,
		TurnRate = 0.028f,
		MinSpeedFrac = 0.6f,
		GapDistance = 60f,
	};

	protected override float AimWobbleRadius => 320f;
	protected override float AimWobblePeriod => 280f;

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 1;
		NPCID.Sets.MPAllowedEnemies[Type] = true;
		NPCID.Sets.BossBestiaryPriority.Add(Type);

		NPCID.Sets.SpecificDebuffImmunity[Type] ??= new bool?[BuffLoader.BuffCount];
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;

		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.SoulDistiller.DisplayName", () => "Soul Distiller");
		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.SoulDistiller.Bestiary",
			() => "totally not an EoW reskin, I guess");
	}

	public override void SetDefaults()
	{
		NPC.scale = 1.24f;
		NPC.width = 80;
		NPC.height = 80;
		NPC.lifeMax = 24000;
		NPC.damage = 50;
		NPC.defense = 18;
		ConfigureCommonDefaults();
	}

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * balance * bossAdjustment);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.GregTechCEuTerraria.NPCs.SoulDistiller.Bestiary"));
	}

	protected override int PickAttackState()
	{
		int last = (int)NPC.localAI[0];
		int pick = Main.rand.Next(1, SoulDistillerAttacks.AttackCount + 1);
		if (pick == last) pick = Main.rand.Next(1, SoulDistillerAttacks.AttackCount + 1);
		NPC.localAI[0] = pick;
		return pick;
	}

	protected override bool PreTick(Player target)
	{
		if (_split || Main.netMode == NetmodeID.MultiplayerClient) return false;
		if (NPC.life > NPC.lifeMax / 2) return false;
		Fractionate(target);
		return true;
	}

	private void Fractionate(Player target)
	{
		_split = true;

		SoundEngine.PlaySound(SoundID.NPCDeath14, NPC.Center);
		if (!Main.dedServ)
			for (int i = 0; i < 30; i++)
			{
				var d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 80, default, 1.8f);
				d.noGravity = true;
				d.velocity *= 2.4f;
			}

		int fracType = ModContent.NPCType<SoulDistillerFraction>();
		int fracLife = NPC.lifeMax / 4;
		for (int f = 0; f < SoulDistillerRenderer.FractionCount; f++)
		{
			int who = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, fracType,
				Start: 0, ai3: f);
			if (who >= Main.maxNPCs) continue;

			NPC s = Main.npc[who];
			s.lifeMax = fracLife;
			s.life = fracLife;
			s.target = NPC.target;
			float ang = MathHelper.TwoPi * f / SoulDistillerRenderer.FractionCount;
			s.velocity = ang.ToRotationVector2() * 8f;
			s.netUpdate = true;
			NetMessage.SendData(MessageID.SyncNPC, number: who);
		}

		KillChain();
		NPC.active = false;
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
	}
}
