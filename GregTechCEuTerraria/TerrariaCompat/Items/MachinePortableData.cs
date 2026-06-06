#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

// Per-item NBT blob for machine-placer items. Universal - attaches to every
// item that places one of our machine tiles. Set by MetaMachineTile.GetItemDrops
public sealed class MachinePortableData : GlobalItem
{
	public override bool InstancePerEntity => true;

	public TagCompound? Data;

	public override bool AppliesToEntity(Item item, bool lateInstantiation) =>
		item.createTile >= TileID.Count && TileLoader.GetTile(item.createTile) is IMachineTextureSpec;

	public override void SaveData(Item item, TagCompound tag)
	{
		if (Data is { Count: > 0 }) tag["portable"] = Data;
	}

	public override void LoadData(Item item, TagCompound tag)
	{
		Data = tag.ContainsKey("portable") ? tag.GetCompound("portable") : null;
	}

	public override void NetSend(Item item, BinaryWriter writer)
	{
		bool has = Data is { Count: > 0 };
		writer.Write(has);
		if (has) TagIO.Write(Data!, writer);
	}

	public override void NetReceive(Item item, BinaryReader reader)
	{
		Data = reader.ReadBoolean() ? TagIO.Read(reader) : null;
	}

	public override bool CanStack(Item destination, Item source) => !EitherHasData(destination, source);

	public override bool CanStackInWorld(Item destination, Item source) => !EitherHasData(destination, source);

	private static bool EitherHasData(Item a, Item b) => HasData(a) || HasData(b);

	private static bool HasData(Item item) => item.TryGetGlobalItem<MachinePortableData>(out var d) && d.Data is { Count: > 0 };

	public override GlobalItem Clone(Item? from, Item to)
	{
		var clone = (MachinePortableData)base.Clone(from, to);
		clone.Data = Data is null ? null : (TagCompound)Data.Clone();
		return clone;
	}
}
