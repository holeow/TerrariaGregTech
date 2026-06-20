#nullable enable
using System;
using GregTechCEuTerraria.TerrariaCompat.Bosses.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Bosses.CausticReactor;

// EV-age custom boss, slotted post-Plantera
//   ai[0] = state   ai[1] = state timer
//   ai[2] = last spell (avoid repeats)   ai[3] = phase (0 = corrosive, 1 = distilled)
[AutoloadBossHead]
public class CausticReactor : ModNPC, IDebuggableBoss
{
	private const int S_Reposition = 0, S_Rose = 1, S_Phyllo = 2, S_Lissajous = 3,
		S_Hex = 4, S_Spiral = 5, S_Cardioid = 6;

	private const int DropletDamage = 32;
	private const float StandoffRadius = 360f;
	private const int FadeOutTicks = 16;
	private const int FadeInTicks = 16;
	private const int TelegraphTicks = 24;
	private const int SpellDurP1 = 240;
	private const int SpellDurP2 = 210;

	private const float OrbitRate = 0.013f;
	private const float OrbitSpeed = 3.6f;
	private const float OrbitAccel = 0.04f;

	private const int ChaosIntervalP1 = 20;
	private const int ChaosIntervalP2 = 13;
	private const float ChaosSpreadDeg = 75f;

	private static bool _headSwapped;

	private CausticReactorAttacks.Emit? _emit;
	private CausticReactorAttacks.Emit EmitCallback => _emit ??= SpawnDroplet;

	public override string Texture => "GregTechCEuTerraria/Content/Textures/block/casings/solid/machine_casing_inert_ptfe";
	public override string BossHeadTexture => "GregTechCEuTerraria/Content/Textures/block/casings/solid/machine_casing_inert_ptfe";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 1;
		NPCID.Sets.MPAllowedEnemies[Type] = true;
		NPCID.Sets.BossBestiaryPriority.Add(Type);

		NPCID.Sets.SpecificDebuffImmunity[Type] ??= new bool?[BuffLoader.BuffCount];
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;

		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.CausticReactor.DisplayName", () => "Caustic Reactor");
		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.CausticReactor.Bestiary",
			() => "they said LCR is OP");
	}

	public override void SetDefaults()
	{
		NPC.scale = 2f;
		NPC.width = (int)(CausticReactorRenderer.Width * NPC.scale * 0.70f);
		NPC.height = (int)(CausticReactorRenderer.Height * NPC.scale * 0.70f);
		NPC.damage = 40;
		NPC.defense = 22;
		NPC.lifeMax = 14000;
		NPC.HitSound = SoundID.NPCHit4;       // metallic clang
		NPC.DeathSound = SoundID.NPCDeath14;  // explosion
		NPC.knockBackResist = 0f;
		NPC.noGravity = true;
		NPC.noTileCollide = true;
		NPC.boss = true;
		NPC.npcSlots = 10f;
		NPC.aiStyle = -1;
		NPC.value = Item.buyPrice(gold: 8);
		NPC.SpawnWithHigherTime(30);

		if (!Main.dedServ)
			Music = MusicID.Boss3;
	}

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * balance * bossAdjustment);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.GregTechCEuTerraria.NPCs.CausticReactor.Bestiary"));
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		if (Mod.TryFind<ModItem>("inert_machine_casing", out var casing))
			npcLoot.Add(ItemDropRule.Common(casing.Type, 1, 15, 15));

		var condition = new BossDrops.BossDropCondition();
		foreach (var d in BossDrops.BossDropRegistry.GetTierDrops(4, withComponents: true))
			npcLoot.Add(new ItemDropWithConditionRule(d.ItemType, chanceDenominator: 1,
				amountDroppedMinimum: d.Min, amountDroppedMaximum: d.Max, condition));
	}

	public override void OnKill()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			CausticReactorWorld.MarkDowned();
	}

	public override void AI()
	{
		UpdateVisuals();

		float glow = NPC.localAI[2];
		Lighting.AddLight(NPC.Center, 0.30f * glow, 0.52f * glow, 0.16f * glow);

		if (!BossAI.TryAcquireTarget(NPC, out Player player))
		{
			Despawn();
			return;
		}
		if (NPC.timeLeft < 1800) NPC.timeLeft = 1800;

		NPC.noGravity = true;
		NPC.noTileCollide = true;

		bool phase2 = NPC.ai[3] >= 1f;
		if (!phase2 && NPC.life < NPC.lifeMax * 0.5f)
		{
			EnterPhase2(player);
			phase2 = true;
		}

		switch ((int)NPC.ai[0])
		{
			case S_Reposition: Reposition(player, phase2); break;
			case S_Rose:       RunSpell(player, phase2, S_Rose);      break;
			case S_Phyllo:     RunSpell(player, phase2, S_Phyllo);    break;
			case S_Lissajous:  RunSpell(player, phase2, S_Lissajous); break;
			case S_Hex:        RunSpell(player, phase2, S_Hex);       break;
			case S_Spiral:     RunSpell(player, phase2, S_Spiral);    break;
			case S_Cardioid:   RunSpell(player, phase2, S_Cardioid);  break;
		}

		if ((int)NPC.ai[0] != S_Reposition)
			AmbientChaos(player, phase2);

		BossAI.SmoothTilt(NPC, perVelocity: 0.010f, maxTilt: 0.06f);
	}

	private void Reposition(Player player, bool phase2)
	{
		NPC.velocity *= 0.85f;
		NPC.ai[1]++;
		int t = (int)NPC.ai[1];

		if (t <= FadeOutTicks)
			NPC.localAI[1] = 1f - t / (float)FadeOutTicks;
		else
			NPC.localAI[1] = MathHelper.Clamp((t - FadeOutTicks) / (float)FadeInTicks, 0f, 1f);

		EmitDissolveDust();

		if (t == FadeOutTicks)
		{
			SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.4f }, NPC.Center);
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				float ang = -MathHelper.PiOver2 + Main.rand.NextFloat(-1.05f, 1.05f);
				NPC.Center = player.Center + ang.ToRotationVector2() * StandoffRadius;
				NPC.velocity = Vector2.Zero;
				NPC.netUpdate = true;
			}
		}

		if (t >= FadeOutTicks + FadeInTicks)
		{
			NPC.localAI[1] = 1f;
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				NPC.ai[0] = PickSpell(phase2, (int)NPC.ai[2]);
				NPC.ai[2] = NPC.ai[0];
				NPC.ai[1] = 0f;
				NPC.netUpdate = true;
			}
		}
	}

	private int PickSpell(bool phase2, int last)
	{
		ReadOnlySpan<int> pool = phase2
			? stackalloc int[] { S_Rose, S_Phyllo, S_Lissajous, S_Hex, S_Spiral, S_Cardioid }
			: stackalloc int[] { S_Rose, S_Phyllo, S_Lissajous, S_Hex, S_Spiral };
		int pick = pool[Main.rand.Next(pool.Length)];
		if (pick == last) pick = pool[Main.rand.Next(pool.Length)]; // one reroll
		return pick;
	}

	private void RunSpell(Player player, bool phase2, int spell)
	{
		float orbitDir = ((int)NPC.ai[2] & 1) == 0 ? 1f : -1f;
		Vector2 toBoss = NPC.Center - player.Center;
		if (toBoss == Vector2.Zero) toBoss = -Vector2.UnitY;
		float ang = toBoss.ToRotation() + orbitDir * OrbitRate * (phase2 ? 1.3f : 1f);
		Vector2 anchor = player.Center + ang.ToRotationVector2() * StandoffRadius;
		BossAI.MoveToward(NPC, anchor, OrbitSpeed, OrbitAccel, easeRadius: 140f);

		NPC.localAI[1] = 1f;
		NPC.ai[1]++;

		int spellDur = phase2 ? SpellDurP2 : SpellDurP1;
		_activePalette = PaletteFor(spell, phase2);

		if (NPC.ai[1] < TelegraphTicks)
		{
			Telegraph();
			return;
		}

		int t = (int)NPC.ai[1] - TelegraphTicks;

		if (t % 18 == 0)
			SoundEngine.PlaySound(SoundID.Item85 with { Volume = 0.5f, Pitch = 0.3f }, NPC.Center);

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var emit = EmitCallback;
			switch (spell)
			{
				case S_Rose:      CausticReactorAttacks.Rose(NPC, t, phase2, spellDur, emit); break;
				case S_Phyllo:    CausticReactorAttacks.Phyllotaxis(NPC, t, phase2, emit); break;
				case S_Lissajous: CausticReactorAttacks.Lissajous(NPC, t, phase2, emit); break;
				case S_Hex:       CausticReactorAttacks.Hex(NPC, t, phase2, emit); break;
				case S_Spiral:    CausticReactorAttacks.Spiral(NPC, t, phase2, emit); break;
				case S_Cardioid:
					CausticReactorAttacks.Cardioid(NPC, t, phase2, emit);
					if (t == 0) SpawnCorrosivePools(player); // ground hazards, once
					break;
			}
		}

		if (NPC.ai[1] >= TelegraphTicks + spellDur)
			ReturnToReposition();
	}

	private void AmbientChaos(Player player, bool phase2)
	{
		if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient) return;
		int interval = phase2 ? ChaosIntervalP2 : ChaosIntervalP1;
		if (Main.GameUpdateCount % (uint)interval != 0) return;

		_activePalette = phase2 ? 7 : 0;
		int count = phase2 && Main.rand.NextBool(2) ? 2 : 1;
		Vector2 baseDir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
		float spread = MathHelper.ToRadians(ChaosSpreadDeg);
		for (int i = 0; i < count; i++)
		{
			float ang = Main.rand.NextFloat(-spread / 2f, spread / 2f);
			float speed = Main.rand.NextFloat(2.6f, 4.8f) * (phase2 ? 1.15f : 1f);
			SpawnDroplet(NPC.Center, baseDir.RotatedBy(ang) * speed);
		}
	}

	private void ReturnToReposition()
	{
		NPC.ai[0] = S_Reposition;
		NPC.ai[1] = 0f;
		NPC.netUpdate = true;
	}

	private void EnterPhase2(Player player)
	{
		NPC.ai[3] = 1f;
		SoundEngine.PlaySound(SoundID.Item62, NPC.Center);
		NPC.localAI[3] = 0.9f;

		if (!Main.dedServ)
			for (int i = 0; i < 30; i++)
			{
				var d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.GreenFairy, 0f, 0f, 100, default, 1.6f);
				d.noGravity = true;
				d.velocity *= 2.5f;
			}

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			_activePalette = 13;
			const int n = 18;
			for (int i = 0; i < n; i++)
				SpawnDroplet(NPC.Center, (MathHelper.TwoPi * i / n).ToRotationVector2() * 4f);

			NPC.ai[0] = S_Cardioid;
			NPC.ai[1] = 0f;
			NPC.ai[2] = S_Cardioid;
			NPC.netUpdate = true;
		}
	}

	private void Despawn()
	{
		BossAI.FlyAwayDespawn(NPC);
		if (NPC.ai[0] != 0f)
		{
			NPC.ai[0] = NPC.ai[1] = NPC.ai[2] = 0f;
			NPC.netUpdate = true;
		}
	}

	public string CurrentAttackLabel() => StateName((int)NPC.ai[0]);
	public int CurrentPhase() => (int)NPC.ai[3];

	private static string StateName(int s) => s switch
	{
		S_Reposition => "Reposition", S_Rose => "Rose", S_Phyllo => "Phyllotaxis",
		S_Lissajous => "Lissajous", S_Hex => "Hex", S_Spiral => "Spiral", S_Cardioid => "Cardioid",
		_ => $"?({s})",
	};

	public void BuildDebugLines(System.Collections.Generic.List<string> lines)
	{
		bool phase2 = NPC.ai[3] >= 1f;
		int state = (int)NPC.ai[0];
		int t = (int)NPC.ai[1];
		int dur = state == S_Reposition ? FadeOutTicks + FadeInTicks
			: TelegraphTicks + (phase2 ? SpellDurP2 : SpellDurP1);
		float hpPct = NPC.lifeMax > 0 ? 100f * NPC.life / NPC.lifeMax : 0f;
		Player p = Main.player[NPC.target];
		float dist = p?.active == true ? Vector2.Distance(NPC.Center, p.Center) : 0f;
		lines.Add($"Caustic Reactor  [{(phase2 ? "PHASE 2" : "PHASE 1")}]  HP {NPC.life}/{NPC.lifeMax} ({hpPct:0.0}%)");
		lines.Add($"Spell: {StateName(state)}   t={t}/{dur}");
		lines.Add($"Last: {StateName((int)NPC.ai[2])}   Standoff: {dist:0}/{(int)StandoffRadius}px");
	}

	public void DrawDebugGizmos(Microsoft.Xna.Framework.Graphics.SpriteBatch sb, Vector2 screenPos)
	{
		Player p = Main.player[NPC.target];
		if (p is null || !p.active) return;
		DebugOverlaySystem.DrawCrosshair(sb, p.Center, screenPos,
			new Color(180, 120, 230), 10f, 1);
	}

	private int _activePalette;

	private static int PaletteFor(int spell, bool phase2)
	{
		int idx = spell switch
		{
			S_Rose => 1, S_Phyllo => 2, S_Lissajous => 3, S_Hex => 4,
			S_Spiral => 5, S_Cardioid => 6, _ => 0,
		};
		return phase2 ? idx + 7 : idx;
	}

	private void SpawnDroplet(Vector2 pos, Vector2 vel)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) return;
		Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, vel,
			ModContent.ProjectileType<AcidDropletProjectile>(), DropletDamage, 1.5f, Main.myPlayer,
			ai0: 0f, ai1: _activePalette);
	}

	private void SpawnCorrosivePools(Player player)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) return;
		ReadOnlySpan<float> offsets = stackalloc float[] { -300f, 0f, 300f };
		foreach (float dx in offsets)
			Projectile.NewProjectile(NPC.GetSource_FromAI(),
				player.Center + new Vector2(dx, 120f), Vector2.Zero,
				ModContent.ProjectileType<CorrosivePoolProjectile>(), DropletDamage, 0f, Main.myPlayer);
	}

	private void Telegraph()
	{
		NPC.localAI[3] = Math.Max(NPC.localAI[3], 0.55f);
		EmitVentBubbles(2);
	}

	private void UpdateVisuals()
	{
		NPC.localAI[0]++;
		bool phase2 = NPC.ai[3] >= 1f;

		float baseG = phase2 ? 0.85f : 0.55f;
		float pulse = baseG + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f);
		if (NPC.localAI[3] > 0f)
		{
			pulse += NPC.localAI[3];
			NPC.localAI[3] *= 0.9f;
			if (NPC.localAI[3] < 0.02f) NPC.localAI[3] = 0f;
		}
		NPC.localAI[2] = MathHelper.Clamp(pulse, 0f, 1.5f);

		if (Main.rand.NextBool(phase2 ? 1 : 2))
			EmitVentBubbles(1);
	}

	private void EmitDissolveDust()
	{
		if (Main.dedServ) return;
		for (int i = 0; i < 3; i++)
		{
			float ang = Main.rand.NextFloat(MathHelper.TwoPi);
			Vector2 at = NPC.Center + ang.ToRotationVector2() * (CausticReactorRenderer.Width * 0.5f * NPC.scale);
			var d = Dust.NewDustPerfect(at, DustID.GreenFairy, (NPC.Center - at) * 0.04f, 100, default, 1.4f);
			d.noGravity = true;
		}
	}

	private void EmitVentBubbles(int count)
	{
		if (Main.dedServ) return;
		Vector2 vent = NPC.Center + new Vector2(0f, -CausticReactorRenderer.Height * 0.10f * NPC.scale);
		bool phase2 = NPC.ai[3] >= 1f;
		var col = phase2 ? new Color(190, 120, 230) : new Color(160, 215, 70);
		for (int i = 0; i < count; i++)
		{
			Vector2 at = vent + new Vector2(Main.rand.Next(-16, 17) * NPC.scale, Main.rand.Next(-4, 5));
			Vector2 vel = new(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.5f, 1.4f));
			var d = Dust.NewDustPerfect(at, DustID.GreenFairy, vel, 110, col, Main.rand.NextFloat(1.1f, 1.7f));
			d.noGravity = true;
			d.fadeIn = 0.4f;
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		int n = NPC.life <= 0 ? 34 : 4;
		for (int i = 0; i < n; i++)
		{
			var d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
				DustID.GreenFairy, hit.HitDirection, -1f, 100, default, 1.3f);
			d.noGravity = true;
		}
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		BossHeadHelper.SwapBakedHead(NPC, CausticReactorRenderer.BossHeadAsset, ref _headSwapped);

		var body = CausticReactorRenderer.Body;
		if (body is null) return true;

		Vector2 pos = NPC.Center - screenPos;
		float scale = NPC.scale;
		var origin = new Vector2(body.Width / 2f, body.Height / 2f);

		float fade = NPC.ai[0] == S_Reposition ? MathHelper.Clamp(NPC.localAI[1], 0f, 1f) : 1f;
		Color bodyColor = drawColor * fade;

		spriteBatch.Draw(body, pos, null, bodyColor, NPC.rotation, origin, scale, SpriteEffects.None, 0f);

		var glow = CausticReactorRenderer.Glow;
		if (glow is not null)
		{
			bool phase2 = NPC.ai[3] >= 1f;
			Color hue = phase2 ? new Color(200, 130, 245) : new Color(170, 230, 90);
			float g = NPC.localAI[2] * fade;
			spriteBatch.Draw(glow, pos, null, hue * MathHelper.Clamp(g, 0f, 1f),
				NPC.rotation, origin, scale, SpriteEffects.None, 0f);
			if (g > 1f)
				spriteBatch.Draw(glow, pos, null, hue * (g - 1f),
					NPC.rotation, origin, scale, SpriteEffects.None, 0f);
		}

		return false;
	}
}
