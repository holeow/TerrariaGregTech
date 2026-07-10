#nullable enable
using System.Collections.Generic;
using System.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Patterns;

public sealed class EncodedPatternItem : ModItem, ITextureWarmUp
{
	public MePattern? Pattern;

	public override string Texture =>
		"GregTechCEuTerraria/Content/TerrariaCompat/encoded_pattern";

	protected override bool CloneNewInstances => true;

	public override void SetStaticDefaults() =>
		Terraria.Localization.Language.GetOrRegister(
			$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => "ME Pattern");

	public override void SetDefaults()
	{
		Item.maxStack = 1;
		Item.width = 32;
		Item.height = 32;
		Item.rare = ItemRarityID.LightPurple;
	}

	public override bool CanStack(Item source) => false;

	public static Item Create(MePattern pattern)
	{
		var item = new Item();
		item.SetDefaults(ModContent.ItemType<EncodedPatternItem>());
		if (item.ModItem is EncodedPatternItem e) e.Pattern = pattern;
		return item;
	}

	public override void SaveData(TagCompound tag)
	{
		if (Pattern != null) tag["p"] = Pattern.Encode();
	}

	public override void LoadData(TagCompound tag)
	{
		Pattern = tag.ContainsKey("p") ? MePattern.Decode(tag.GetCompound("p")) : null;
	}

	public override void NetSend(BinaryWriter writer)
	{
		bool has = Pattern != null;
		writer.Write(has);
		if (has) TagIO.Write(Pattern!.Encode(), writer);
	}

	public override void NetReceive(BinaryReader reader)
	{
		Pattern = reader.ReadBoolean() ? MePattern.Decode(TagIO.Read(reader)) : null;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		if (Pattern is null)
		{
			tooltips.Add(new TooltipLine(Mod, "me_pattern_empty", "[c/AAAAAA:Unencoded]"));
			return;
		}

		string kind = Pattern.Type == MePatternType.Crafting ? "Crafting Pattern" : "Processing Pattern";
		tooltips.Add(new TooltipLine(Mod, "me_pattern_kind", $"[c/B24CFF:{kind}]"));

		if (Pattern.Inputs.Count > 0)
		{
			tooltips.Add(new TooltipLine(Mod, "me_pattern_in_h", "[c/88CCFF:Input:]"));
			for (int i = 0; i < Pattern.Inputs.Count; i++)
			{
				var (what, amount) = Pattern.Inputs[i];
				string line = $"  {amount}x {what.GetDisplayName()}";
				var tag = Pattern.InputTag(i);
				if (tag != null)
					line += $"  [c/7AA0FF:#{ShortTag(tag)}]";
				tooltips.Add(new TooltipLine(Mod, "me_pattern_in", line));
			}
		}
		if (Pattern.Outputs.Count > 0)
		{
			tooltips.Add(new TooltipLine(Mod, "me_pattern_out_h", "[c/88FF88:Output:]"));
			foreach (var (what, amount) in Pattern.Outputs)
				tooltips.Add(new TooltipLine(Mod, "me_pattern_out", $"  {amount}x {what.GetDisplayName()}"));
		}

		if (Pattern.Type == MePatternType.Crafting && Pattern.StationTile >= 0)
			tooltips.Add(new TooltipLine(Mod, "me_pattern_station",
				$"  station: {StationName(Pattern.StationTile)}"));
	}

	private static string ShortTag(string tag)
	{
		int colon = tag.IndexOf(':');
		return colon >= 0 ? tag[(colon + 1)..] : tag;
	}

	private static Dictionary<int, string>? _stationNames;

	private static string StationName(int tileId)
	{
		_stationNames ??= BuildStationNames();
		return _stationNames.TryGetValue(tileId, out var n) ? n : $"tile {tileId}";
	}

	private static Dictionary<int, string> BuildStationNames()
	{
		var map = new Dictionary<int, string>();
		foreach (var kv in ContentSamples.ItemsByType)
		{
			var it = kv.Value;
			if (it != null && it.createTile != -1 && !map.ContainsKey(it.createTile))
				map[it.createTile] = it.Name;
		}
		return map;
	}

	private int _previewType = -2;
	private MePattern? _previewFor;

	private int PreviewItemType()
	{
		if (_previewType != -2 && ReferenceEquals(_previewFor, Pattern)) return _previewType;
		_previewFor = Pattern;
		_previewType = Pattern?.PrimaryOutput is AEItemKey ik ? ik.GetItem() : -1;
		return _previewType;
	}

	public override void PostDrawInInventory(SpriteBatch sb, Vector2 position,
		Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		DrawOutputPreview(sb, position, scale * 0.55f);
		DrawKindBadge(sb, position, scale);
	}

	public override void PostDrawInWorld(SpriteBatch sb, Color lightColor,
		Color alphaColor, float rotation, float scale, int whoAmI)
		=> DrawOutputPreview(sb, Item.Center - Main.screenPosition, scale * 0.55f);

	private void DrawOutputPreview(SpriteBatch sb, Vector2 center, float scale)
	{
		if (Pattern?.PrimaryOutput is AEFluidKey fk)
		{
			var fluid = fk.GetFluid();
			int size = (int)(32f * scale);
			if (size < 2) return;
			var rect = new Rectangle((int)(center.X - size / 2f), (int)(center.Y - size / 2f), size, size);
			if (!FluidIconRenderer.Draw(sb, fluid, rect))
				sb.Draw(TextureAssets.MagicPixel.Value, rect, FluidIconRenderer.RgbColor(fluid.Color));
			return;
		}

		int type = PreviewItemType();
		if (type <= 0) return;
		Main.instance.LoadItem(type);
		var tex = TextureAssets.Item[type].Value;
		if (tex is null) return;
		Rectangle src = Main.itemAnimations[type] is { } anim ? anim.GetFrame(tex) : tex.Frame();
		sb.Draw(tex, center, src, Color.White, 0f, src.Size() / 2f, scale, SpriteEffects.None, 0f);
	}

	private void DrawKindBadge(SpriteBatch sb, Vector2 center, float scale)
	{
		if (Pattern is null) return;
		bool crafting = Pattern.Type == MePatternType.Crafting;
		string letter = crafting ? "C" : "P";
		Color color = crafting ? new Color(120, 200, 255) : new Color(255, 170, 70);
		Vector2 pos = center + new Vector2(9f, 7f) * scale;
		Terraria.Utils.DrawBorderString(sb, letter, pos, color, scale, 0.5f, 0.5f);
	}

	public override void HoldItem(Player player) => EnsureBaked();
	void ITextureWarmUp.WarmUpTexture() => EnsureBaked();

	private void EnsureBaked()
	{
		if (Main.dedServ) return;
		ItemIconBaker.Install(Item.type, new IconLayer(Texture, Color.White, 1f));
	}
}
