#nullable enable
using System.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI.Chat;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class WrappedGenericStack : ModItem, IWrappedGenericStack
{
	public AEKey? What { get; set; }
	public long Amount { get; set; }

	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/blank_pattern";

	protected override bool CloneNewInstances => true;

	public static Item Wrap(AEKey what, long amount)
	{
		var item = new Item();
		item.SetDefaults(ModContent.ItemType<WrappedGenericStack>());
		if (item.ModItem is WrappedGenericStack w) { w.What = what; w.Amount = amount; }
		return item;
	}

	public override void SetStaticDefaults()
	{
		Terraria.Localization.Language.GetOrRegister(
			$"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName", () => "Wrapped Stack");
		ItemID.Sets.Deprecated[Type] = true;
		GenericStack.WrapFactory = Wrap;
	}

	public override void SetDefaults()
	{
		Item.maxStack = 1;
		Item.width = 32;
		Item.height = 32;
		Item.rare = ItemRarityID.LightPurple;
	}

	public override bool CanStack(Item source) => false;

	public override void SaveData(TagCompound tag)
	{
		if (What == null) return;
		tag["k"] = What.ToTagGeneric();
		if (Amount != 0) tag["#"] = Amount;
	}

	public override void LoadData(TagCompound tag)
	{
		What = tag.ContainsKey("k") ? AEKey.FromTagGeneric(tag.GetCompound("k")) : null;
		Amount = tag.ContainsKey("#") ? tag.GetLong("#") : 0;
	}

	public override void NetSend(BinaryWriter writer)
	{
		AEKey.WriteOptionalKey(writer, What);
		writer.Write7BitEncodedInt64(Amount);
	}

	public override void NetReceive(BinaryReader reader)
	{
		What = AEKey.ReadOptionalKey(reader);
		Amount = reader.Read7BitEncodedInt64();
	}

	public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
	{
		if (What == null) return;
		tooltips.Clear();
		tooltips.Add(new TooltipLine(Mod, "wgs_name", What.GetDisplayName()));
		if (Amount != 0)
			tooltips.Add(new TooltipLine(Mod, "wgs_amount", $"[c/AAAAAA:{Amount:N0}]"));
	}

	public override bool PreDrawInInventory(SpriteBatch sb, Vector2 position, Rectangle frame,
		Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		DrawResource(sb, position, scale);
		return false;
	}

	public override bool PreDrawInWorld(SpriteBatch sb, Color lightColor, Color alphaColor,
		ref float rotation, ref float scale, int whoAmI)
	{
		DrawResource(sb, Item.Center - Main.screenPosition, scale);
		return false;
	}

	private void DrawResource(SpriteBatch sb, Vector2 center, float scale)
	{
		if (What is AEItemKey ik)
		{
			int type = ik.GetItem();
			Main.instance.LoadItem(type);
			var tex = TextureAssets.Item[type].Value;
			if (tex != null)
			{
				Rectangle src = Main.itemAnimations[type] is { } anim ? anim.GetFrame(tex) : tex.Frame();
				sb.Draw(tex, center, src, Color.White, 0f, src.Size() / 2f, scale, SpriteEffects.None, 0f);
			}
		}
		else if (What is AEFluidKey fk)
		{
			int half = (int)(16 * scale);
			var bounds = new Rectangle((int)center.X - half, (int)center.Y - half, half * 2, half * 2);
			BrowserFluidSlot.Draw(sb, bounds, fk.GetFluid());
		}

		if (Amount != 0)
		{
			string text = UI.UINumberFormat.Count(Amount);
			var font = FontAssets.ItemStack.Value;
			var size = ChatManager.GetStringSize(font, text, new Vector2(0.7f));
			ChatManager.DrawColorCodedStringWithShadow(sb, font, text,
				center + new Vector2(14f * scale - size.X, 14f * scale - size.Y),
				Color.White, 0f, Vector2.Zero, new Vector2(0.7f));
		}
	}
}
