#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.AppliedEnergistics.Api.Util;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public static class MeCableRenderer
{
	private const int FrameSize = 16;
	private const int GridSide = 4;
	private const int SheetSide = FrameSize * GridSide;
	private const int BodyThickness = 4;
	private const int CoreThickness = 2;

	private static Texture2D? _atlasBody, _atlasCore;

	public static void DrawVisible() => DrawAll(Main.spriteBatch, foreground: false);

	public static void DrawForegroundOverlay()
	{
		if (MeCableLayerSystem.Cables.Count == 0) return;
		var sb = Main.spriteBatch;
		sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
		try { DrawAll(sb, foreground: true); }
		finally { sb.End(); }
	}

	private static void DrawAll(SpriteBatch sb, bool foreground)
	{
		var cables = MeCableLayerSystem.Cables;
		if (cables.Count == 0) return;

		int firstX = (int)(Main.screenPosition.X / 16) - 1;
		int lastX = (int)((Main.screenPosition.X + Main.screenWidth) / 16) + 1;
		int firstY = (int)(Main.screenPosition.Y / 16) - 1;
		int lastY = (int)((Main.screenPosition.Y + Main.screenHeight) / 16) + 1;

		var bodyTex = BodyAtlas();
		var coreTex = CoreAtlas();
		foreach (var kv in cables.All)
		{
			int x = kv.Key.x;
			int y = kv.Key.y;
			if (x < firstX || x > lastX || y < firstY || y > lastY) continue;

			int frame = cables.ConnectionMask(x, y) | EndpointMask(x, y);
			var src = new Rectangle((frame % GridSide) * FrameSize, (frame / GridSide) * FrameSize, FrameSize, FrameSize);
			var pos = new Vector2(
				x * 16 - (int)Main.screenPosition.X,
				y * 16 - (int)Main.screenPosition.Y);

			Color light = foreground ? Color.White : Lighting.GetColor(x, y);

			sb.Draw(bodyTex, pos, src, Mul(new Color(64, 66, 78), light));

			Color coreBase = FromHex(kv.Value.Color.MediumVariant());
			float phase = Main.GameUpdateCount * 0.08f - (x + y) * 0.6f;
			float bright = 0.45f + 0.55f * (0.5f + 0.5f * (float)System.Math.Sin(phase));
			sb.Draw(coreTex, pos, src, Mul(MulS(coreBase, bright), light));

			DrawBusOverlays(sb, x, y, pos, foreground);
		}
	}

	private static Color Mul(Color a, Color b) =>
		new(a.R * b.R / 255, a.G * b.G / 255, a.B * b.B / 255);

	private static Color MulS(Color a, float s) =>
		new((byte)(a.R * s), (byte)(a.G * s), (byte)(a.B * s));

	private static Texture2D? _texStorage, _texImport, _texExport;

	private static Texture2D? BusTex(MeBusKind kind) => kind switch
	{
		MeBusKind.Storage => _texStorage ??= ReqTex("MeWorldStorageBus"),
		MeBusKind.Import  => _texImport  ??= ReqTex("MeWorldImportBus"),
		MeBusKind.Export  => _texExport  ??= ReqTex("MeWorldExportBus"),
		_                 => null,
	};

	private static Texture2D ReqTex(string name) =>
		ModContent.Request<Texture2D>(
			"GregTechCEuTerraria/Content/TerrariaCompat/" + name, AssetRequestMode.ImmediateLoad).Value;

	private static void DrawBusOverlays(SpriteBatch sb, int x, int y, Vector2 pos, bool foreground)
	{
		if (!MeBusLayerSystem.Buses.HasAny(x, y)) return;
		Color light = foreground ? Color.White : Lighting.GetColor(x, y);

		foreach (var (side, _, _) in IODirectionExtensions.Cardinal4)
		{
			var att = MeBusLayerSystem.Buses.Get(x, y, side);
			if (att is null) continue;
			var tex = BusTex(att.Kind);
			if (tex is null) continue;

			float rot = side switch
			{
				IODirection.Down  => 0f,
				IODirection.Left  => MathHelper.PiOver2,
				IODirection.Up    => MathHelper.Pi,
				IODirection.Right => MathHelper.Pi + MathHelper.PiOver2,
				_                 => 0f,
			};
			Vector2 offset = side switch
			{
				IODirection.Down  => new Vector2(8, 12),
				IODirection.Up    => new Vector2(8, 4),
				IODirection.Left  => new Vector2(4, 8),
				IODirection.Right => new Vector2(12, 8),
				_                 => new Vector2(8, 8),
			};
			sb.Draw(tex, pos + offset, null, light, rot, new Vector2(4, 4), 1f, SpriteEffects.None, 0f);
		}
	}

	private static int EndpointMask(int x, int y)
	{
		int m = 0;
		foreach (var (side, dx, dy) in IODirectionExtensions.Cardinal4)
		{
			int bit = side switch
			{
				IODirection.Up => 1, IODirection.Down => 2, IODirection.Left => 4, IODirection.Right => 8, _ => 0,
			};
			bool connect =
				(MachineCellResolver.TryFindMachineAt(x + dx, y + dy, out var machine) && IsMeConnectable(machine))
				|| MeBusLayerSystem.Buses.Get(x, y, side) != null;
			if (connect) m |= bit;
		}
		return m;
	}

	private static bool IsMeConnectable(MetaMachine machine) => machine is IMeNetworkConnected;

	private static Texture2D BodyAtlas() => _atlasBody ??= BuildAtlas(BodyThickness);
	private static Texture2D CoreAtlas() => _atlasCore ??= BuildAtlas(CoreThickness);

	private static Texture2D BuildAtlas(int thickness)
	{
		var pixels = new Color[SheetSide * SheetSide];
		int armLow = (FrameSize - thickness) / 2;
		int armHigh = armLow + thickness - 1;

		for (int mask = 0; mask < GridSide * GridSide; mask++)
		{
			int ox = (mask % GridSide) * FrameSize;
			int oy = (mask / GridSide) * FrameSize;
			FillRect(pixels, ox + armLow, oy + armLow, ox + armHigh, oy + armHigh);
			if ((mask & 1) != 0) FillRect(pixels, ox + armLow, oy + 0, ox + armHigh, oy + armLow);
			if ((mask & 2) != 0) FillRect(pixels, ox + armLow, oy + armHigh, ox + armHigh, oy + FrameSize - 1);
			if ((mask & 4) != 0) FillRect(pixels, ox + 0, oy + armLow, ox + armLow, oy + armHigh);
			if ((mask & 8) != 0) FillRect(pixels, ox + armHigh, oy + armLow, ox + FrameSize - 1, oy + armHigh);
		}

		var tex = RuntimeTextureRegistry.New(SheetSide, SheetSide);
		tex.SetData(pixels);
		return tex;
	}

	private static void FillRect(Color[] pixels, int x0, int y0, int x1, int y1)
	{
		for (int y = y0; y <= y1; y++)
			for (int x = x0; x <= x1; x++)
				pixels[y * SheetSide + x] = Color.White;
	}

	public static Color FromHex(int rgb) =>
		new Color((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
}
