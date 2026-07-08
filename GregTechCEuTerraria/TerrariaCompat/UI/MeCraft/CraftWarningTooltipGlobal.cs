#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;

public sealed class StationRecipePreview
{
	public GTRecipe[] GtRecipes = System.Array.Empty<GTRecipe>();
	public HashSet<int> Covered = new();
	public bool HasMore;
}

public sealed class CraftWarningTooltipGlobal : GlobalItem
{
	public const int MaxRecipes = 4;
	private const int MetaWidth = 210;

	private static string? _pending;
	private static string? _pendingInfo;

	private static StationRecipePreview? _pendingPreview;
	private static StationRecipePreview? _drawPreview;

	public static void Push(string reason) => _pending = reason;
	public static void PushInfo(string info) => _pendingInfo = info;
	public static void PushStationRecipe(StationRecipePreview preview) => _pendingPreview = preview;

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (_pending is { } reason)
		{
			_pending = null;
			tooltips.Add(new TooltipLine(Mod, "GTCraftBlocked", reason)
			{
				OverrideColor = new Color(255, 100, 100),
			});
		}
		if (_pendingInfo is { } info)
		{
			_pendingInfo = null;
			int n = 0;
			foreach (var line in info.Split('\n'))
				tooltips.Add(new TooltipLine(Mod, "GTCraftInfo" + n++, line)
				{
					OverrideColor = new Color(200, 210, 230),
				});
		}

		_drawPreview = _pendingPreview;
		_pendingPreview = null;
	}

	public override void PostDrawTooltip(Item item, ReadOnlyCollection<DrawableTooltipLine> lines)
	{
		if (_drawPreview is not { } p) return;
		_drawPreview = null;
		if (p.GtRecipes.Length == 0 || lines.Count == 0) return;

		int left = int.MaxValue, bottom = 0;
		var font = FontAssets.MouseText.Value;
		foreach (var l in lines)
		{
			if (l.X < left) left = l.X;
			int h = (int)font.MeasureString(string.IsNullOrEmpty(l.Text) ? " " : l.Text).Y;
			if (l.Y + h > bottom) bottom = l.Y + h;
		}

		int n = System.Math.Min(p.GtRecipes.Length, MaxRecipes);
		int maxW = 0;
		for (int i = 0; i < n; i++)
			maxW = System.Math.Max(maxW, RecipeRowRenderer.MeasureWidth(p.GtRecipes[i]) + MetaWidth);
		int totalH = n * RecipeRowRenderer.RowHeight;

		int x = left, y = bottom + 6;
		var sb = Main.spriteBatch;
		Terraria.Utils.DrawInvBG(sb, new Rectangle(x - 10, y - 6, maxW + 20, totalH + 12),
			new Color(23, 25, 81, 255) * 0.925f);

		RecipeRowRenderer.CoveredStationTiles = p.Covered;
		try
		{
			for (int i = 0; i < n; i++)
			{
				var bounds = new Rectangle(x, y + i * RecipeRowRenderer.RowHeight,
					RecipeRowRenderer.MeasureWidth(p.GtRecipes[i]) + MetaWidth, RecipeRowRenderer.RowHeight);
				RecipeRowRenderer.Draw(sb, bounds, p.GtRecipes[i], Color.White, craftButton: false);
			}
		}
		finally { RecipeRowRenderer.CoveredStationTiles = null; }
	}
}
