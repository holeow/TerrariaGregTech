#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Common.Materials;
using GregTechCEuTerraria.Api.Pipenet;
using GregTechCEuTerraria.TerrariaCompat.Cover;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.ItemPipe;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Fluid;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike;

public static class PipeRenderer
{
	private const string PipeSideTex = "GregTechCEuTerraria/Content/Textures/block/pipe/pipe_side";

	private static readonly Dictionary<PipeSize, int> _thicknessBySize = new()
	{
		{ PipeSize.Tiny,       4  },
		{ PipeSize.Small,      5  },
		{ PipeSize.Normal,     6  },
		{ PipeSize.Large,      7  },
		{ PipeSize.Huge,       8  },
		{ PipeSize.Quadruple,  9  },
		{ PipeSize.Nonuple,    10 },
	};

	private static int ThicknessFor(PipeSize size) =>
		_thicknessBySize.TryGetValue(size, out var t) ? t : 6;

	public static void DrawItemPipes()
	{
		var layer = ItemPipeLayerSystem.Pipes;
		if (layer.Count == 0) return;
		DrawLayer(Main.spriteBatch, layer, kind: PipeKind.Item, foreground: false);
	}

	public static void DrawFluidPipes()
	{
		var layer = FluidPipeLayerSystem.Pipes;
		if (layer.Count == 0) return;
		DrawLayer(Main.spriteBatch, layer, kind: PipeKind.Fluid, foreground: false);
	}

	public static void DrawItemForegroundOverlay()
	{
		var layer = ItemPipeLayerSystem.Pipes;
		if (layer.Count == 0) return;
		DrawForegroundFor(layer, PipeKind.Item);
	}

	public static void DrawFluidForegroundOverlay()
	{
		var layer = FluidPipeLayerSystem.Pipes;
		if (layer.Count == 0) return;
		DrawForegroundFor(layer, PipeKind.Fluid);
	}

	private static void DrawForegroundFor<TCell>(GridLayer<TCell> layer, PipeKind kind) where TCell : struct
	{
		var sb = Main.spriteBatch;
		sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, RasterizerState.CullCounterClockwise, null,
			Main.GameViewMatrix.TransformationMatrix);
		try { DrawLayer(sb, layer, kind, foreground: true); }
		finally { sb.End(); }
	}

	private struct PipeGeom
	{
		public int Mask;
		public int Thickness;
		public int FluidCapacity;
		public Color BaseTint;
		public Texture2D? ConnUp, ConnDown, ConnLeft, ConnRight;
	}

	private const int GeomTtlFrames = 300;
	private static readonly Dictionary<(int x, int y), PipeGeom> _fluidGeom = new();
	private static readonly Dictionary<(int x, int y), PipeGeom> _itemGeom  = new();

	public static void ClearGeomCaches()
	{
		_fluidGeom.Clear();
		_itemGeom.Clear();
	}

	public static void InvalidateGeom(int x, int y)
	{
		Drop(x, y);
		Drop(x, y - 1); Drop(x, y + 1);
		Drop(x - 1, y); Drop(x + 1, y);
	}

	public static void InvalidateGeomAround(int x, int y)
	{
		for (int dy = -3; dy <= 3; dy++)
		for (int dx = -3; dx <= 3; dx++)
			Drop(x + dx, y + dy);
	}

	private static void Drop(int x, int y)
	{
		var key = (x, y);
		_fluidGeom.Remove(key);
		_itemGeom.Remove(key);
	}

	private static void DrawLayer<TCell>(SpriteBatch sb, GridLayer<TCell> layer, PipeKind kind, bool foreground)
		where TCell : struct
	{
		int firstX = (int)(Main.screenPosition.X / 16) - 1;
		int lastX  = (int)((Main.screenPosition.X + Main.screenWidth) / 16) + 1;
		int firstY = (int)(Main.screenPosition.Y / 16) - 1;
		int lastY  = (int)((Main.screenPosition.Y + Main.screenHeight) / 16) + 1;

		var tex = PipeBodyArt.Tex(PipeSideTex);
		if (tex is null) return;

		var geomCache = kind == PipeKind.Fluid ? _fluidGeom : _itemGeom;
		uint frame = Main.GameUpdateCount;

		foreach (var kv in layer.All)
		{
			int x = kv.Key.x;
			int y = kv.Key.y;
			if (x < firstX || x > lastX || y < firstY || y > lastY) continue;

			uint phase = (uint)(((x * 13 + y * 7) % GeomTtlFrames + GeomTtlFrames) % GeomTtlFrames);
			if (!geomCache.TryGetValue(kv.Key, out var g) || (frame + phase) % GeomTtlFrames == 0)
			{
				(string materialId, PipeSize size, bool restrictive) = ReadCell(kv.Value, kind);
				g = BuildGeom(kind, x, y, materialId, size, restrictive, layer.ConnectionMask(x, y));
				geomCache[kv.Key] = g;
			}

			Vector2 pos = new Vector2(
				x * 16 - (int)Main.screenPosition.X,
				y * 16 - (int)Main.screenPosition.Y);

			Color light = foreground ? Color.White : Lighting.GetColor(x, y);
			Color tint  = Mul(g.BaseTint, light);

			PipeBodyArt.DrawCell(sb, tex, pos, g.Mask, g.Thickness, tint);

			if (kind == PipeKind.Fluid)
				DrawFluidFill(sb, x, y, pos, g.Mask, g.Thickness, g.FluidCapacity, light);
			else
				DrawItemDots(sb, x, y, pos, g.Mask, light);

			DrawConnectors(sb, pos, in g, Lighting.GetColor(x, y));
		}
	}

	private static PipeGeom BuildGeom(PipeKind kind, int x, int y, string materialId, PipeSize size,
	                                  bool restrictive, int connMask)
	{
		var pcv = kind == PipeKind.Fluid
			? FluidPipeLayerSystem.GetSides(x, y)
			: ItemPipeLayerSystem .GetSides(x, y);

		Color baseTint = MaterialColor(materialId);
		if (restrictive) baseTint = Darken(baseTint, 0.55f);

		var g = new PipeGeom { Thickness = ThicknessFor(size), BaseTint = baseTint };

		if (kind == PipeKind.Fluid)
		{
			var c = FluidPipeLayerSystem.Pipes.CellAt(x, y);
			g.FluidCapacity = c.HasValue ? System.Math.Max(1, c.Value.Throughput * 20) : 1;
		}

		int endpointMask = 0;
		if (pcv is not null)
		{
			foreach (var side in CoverSides.All)
			{
				var mode = pcv.GetMode(side);
				if (mode == PipeSideMode.Off) continue;
				if (PipeNeighborProbe.ProbeAt(x, y, side, kind) != SideNeighbourKind.Inventory) continue;

				endpointMask |= side switch
				{
					CoverSide.Up    => 1,
					CoverSide.Down  => 2,
					CoverSide.Left  => 4,
					CoverSide.Right => 8,
					_               => 0,
				};

				var conn = ConnectorTex(kind, mode, pcv, side);
				switch (side)
				{
					case CoverSide.Up:    g.ConnUp    = conn; break;
					case CoverSide.Down:  g.ConnDown  = conn; break;
					case CoverSide.Left:  g.ConnLeft  = conn; break;
					case CoverSide.Right: g.ConnRight = conn; break;
				}
			}
		}

		g.Mask = connMask | endpointMask;
		return g;
	}

	private static void DrawConnectors(SpriteBatch sb, Vector2 pos, in PipeGeom g, Color light)
	{
		if (g.ConnUp is null && g.ConnDown is null && g.ConnLeft is null && g.ConnRight is null) return;
		Vector2 center = pos + new Vector2(8, 8);
		Vector2 origin = new Vector2(8, 8);
		if (g.ConnDown  is not null) sb.Draw(g.ConnDown,  center, null, light, 0f,                                origin, 1f, SpriteEffects.None, 0f);
		if (g.ConnLeft  is not null) sb.Draw(g.ConnLeft,  center, null, light, MathHelper.PiOver2,                origin, 1f, SpriteEffects.None, 0f);
		if (g.ConnUp    is not null) sb.Draw(g.ConnUp,    center, null, light, MathHelper.Pi,                     origin, 1f, SpriteEffects.None, 0f);
		if (g.ConnRight is not null) sb.Draw(g.ConnRight, center, null, light, MathHelper.Pi + MathHelper.PiOver2, origin, 1f, SpriteEffects.None, 0f);
	}

	private const string ConnectorDir = "GregTechCEuTerraria/Content/TerrariaCompat/Connectors";

	private static Texture2D? ConnectorTex(PipeKind kind, PipeSideMode mode, PipeCoverable pcv, CoverSide side)
	{
		string kindWord = kind == PipeKind.Fluid ? "fluid" : "item";
		var filterType = pcv.GetFilterType(side);
		string modeWord;
		if (mode == PipeSideMode.Passive)
		{
			if (filterType == PipeCoverable.PipeFilterType.None) return null;
			modeWord = "passive";
		}
		else
		{
			var io = PipeCoverable.ActiveIoAt(pcv, side);
			modeWord = io == IO.OUT ? "push" : io == IO.IN ? "pull" : "passive";
		}
		string flt = filterType == PipeCoverable.PipeFilterType.None
			? "blacklist"
			: (SideFilterIsBlacklist(kind, pcv, side) ? "blacklist" : "whitelist");
		return PipeBodyArt.Tex($"{ConnectorDir}/{kindWord}_{modeWord}_{flt}");
	}

	private static bool SideFilterIsBlacklist(PipeKind kind, PipeCoverable pcv, CoverSide side)
	{
		var cover = pcv.GetCoverAtSide(side);
		if (cover is null) return false;
		return kind == PipeKind.Fluid
			? (cover.UiFluidFilter?.IsBlackList ?? false)
			: (cover.UiItemFilter?.IsBlackList ?? false);
	}

	private static (string, PipeSize, bool) ReadCell<TCell>(TCell cell, PipeKind kind) where TCell : struct
	{
		if (kind == PipeKind.Item)
		{
			var c = (ItemPipeCell)(object)cell;
			return (c.MaterialId, c.Size, c.Restrictive);
		}
		else
		{
			var c = (FluidPipeCell)(object)cell;
			return (c.MaterialId, c.Size, false);
		}
	}

	private const float DotSpeed   = 0.9f;
	private const int   DotsPerArm = 2;

	private static readonly (CoverSide side, int dx, int dy, int bit)[] ArmDirs =
	{
		(CoverSide.Up, 0, -1, 1), (CoverSide.Down, 0, 1, 2),
		(CoverSide.Left, -1, 0, 4), (CoverSide.Right, 1, 0, 8),
	};

	private static void DrawItemDots(SpriteBatch sb, int x, int y, Vector2 pos, int mask, Color light)
	{
		if (!ItemBusy(x, y)) return;
		if (!ItemPipe.ItemPipeFlow.TryGetOutflow(x, y, out var outDir)) return;

		var px  = Terraria.GameContent.TextureAssets.MagicPixel.Value;
		Color dot = Mul(new Color(255, 140, 0), light);
		Vector2 center = pos + new Vector2(8f, 8f);
		float baseT = Main.GameUpdateCount * DotSpeed / 8f + (x + y) * 0.13f;

		foreach (var (side, dx, dy, bit) in ArmDirs)
		{
			if ((mask & bit) == 0) continue;
			bool outward = side == outDir;
			Vector2 edge = center + new Vector2(dx * 8f, dy * 8f);
			for (int i = 0; i < DotsPerArm; i++)
			{
				float t = Frac(baseT + (float)i / DotsPerArm);
				float p = outward ? t : 1f - t;
				Vector2 dp = Vector2.Lerp(center, edge, p);
				sb.Draw(px, dp, new Rectangle(0, 0, 1, 1), dot, 0f,
					new Vector2(0.5f, 0.5f), 2f, SpriteEffects.None, 0f);
			}
		}
	}

	private static bool ItemBusy(int x, int y)
	{
		if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
			return ItemPipe.ItemPipeNetSystem.ClientTransferStats.TryGetValue((x, y), out int v) && v > 0;
		var pcv = ItemPipeLayerSystem.GetSides(x, y);
		return pcv is not null && pcv.TransferredItems > 0;
	}

	private static float Frac(float v) => v - (float)System.Math.Floor(v);

	private static void DrawFluidFill(SpriteBatch sb, int x, int y, Vector2 pos, int mask, int thickness,
	                                  int capacity, Color light)
	{
		var (fluid, fill) = GetPipeFluid(x, y, capacity);
		if (fluid is null || fill <= 0f) return;
		if (!FluidIconRenderer.TryGetFrame(fluid, out var ftex, out var fsrc, out var fbase) || ftex is null)
			return;
		int maxInner = System.Math.Max(2, thickness - 2);
		int inner    = System.Math.Max(1, (int)(maxInner * fill + 0.5f));
		PipeBodyArt.DrawCellStretch(sb, ftex, fsrc, pos, mask, inner, Mul(fbase, light));
	}

	private static (FluidType? fluid, float fill) GetPipeFluid(int x, int y, int capacity)
	{
		if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
		{
			if (Fluid.FluidPipeLayerSystem.ClientTankSnapshots.TryGetValue((x, y), out var stacks) && stacks != null)
				foreach (var f in stacks) if (!f.IsEmpty) return (f.Type, Frac(f.Amount, capacity));
			return (null, 0f);
		}
		var st = Fluid.FluidPipeLayerSystem.GetState(x, y);
		if (st is null) return (null, 0f);
		int cap2 = st.CapacityPerTank;
		foreach (var f in st.GetContainedFluids()) if (!f.IsEmpty) return (f.Type, Frac(f.Amount, cap2));
		return (null, 0f);
	}

	private static float Frac(int amount, int cap)
	{
		if (cap <= 0) return 1f;
		float f = amount / (float)cap;
		return f < 0f ? 0f : f > 1f ? 1f : f;
	}

	private static Color MaterialColor(string materialId)
	{
		var mat = MaterialRegistry.Get(materialId);
		uint c = mat?.Color ?? 0xFFFFFFu;
		return new Color((byte)((c >> 16) & 0xFF), (byte)((c >> 8) & 0xFF), (byte)(c & 0xFF));
	}

	private static Color Mul(Color a, Color b) =>
		new Color(a.R * b.R / 255, a.G * b.G / 255, a.B * b.B / 255);

	private static Color Darken(Color c, float factor) =>
		new Color((byte)(c.R * factor), (byte)(c.G * factor), (byte)(c.B * factor));
}
