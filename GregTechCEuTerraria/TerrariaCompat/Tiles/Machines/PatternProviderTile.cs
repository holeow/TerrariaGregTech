#nullable enable
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.Machines;

public class PatternProviderTile : TieredMachineTile
{
	public PatternProviderTile() { }
	public PatternProviderTile(VoltageTier tier, MachineDefinition def) : base(tier, def) { }

	private static Texture2D? _arrow;

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		base.PostDraw(i, j, spriteBatch);

		if (!MachineCellResolver.TryFindAt<PatternProviderMachine>(i, j, out var provider)) return;
		if (provider.PushDirection == IODirection.None) return;

		var (w, h) = provider.Size;
		int originX = provider.Position.X, originY = provider.Position.Y;
		if (i != originX + w - 1 || j != originY + h - 1) return;

		_arrow ??= ModContent.Request<Texture2D>(
			"GregTechCEuTerraria/Content/TerrariaCompat/me_pattern_provider_direction",
			AssetRequestMode.ImmediateLoad).Value;

		DirectionArrowOverlay.Draw(spriteBatch, _arrow, originX, originY, w, h,
			provider.PushDirection, Lighting.GetColor(originX, originY));
	}
}
