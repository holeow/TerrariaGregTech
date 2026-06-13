#nullable enable
using System.IO;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class SlotAction : IMachineAction
{
	public enum Kind : byte
	{
		Deposit       = 0,   // cursor -> slot; _cursor = the locally-removed stack
		Pickup        = 1,   // slot -> cursor; _amount: 0 = full, 1 = half
		ShiftClickOut = 2,   // slot -> player inventory; _amount = inv capacity
		ShiftClickIn  = 3,   // player inventory -> machine input slots; _cursor = moved stack
	}

	public PacketType Type => PacketType.SlotAction;

	private SlotGroup _group;
	private byte _index;
	private Kind _kind;
	private Item _cursor = new();
	private int _amount;

	public SlotAction() { }
	public SlotAction(SlotGroup group, int index, Kind kind, Item cursor)
	{
		_group = group;
		_index = (byte)index;
		_kind = kind;
		_cursor = cursor.Clone();
	}

	public SlotAction(SlotGroup group, int index, Kind kind, int amount)
	{
		_group = group;
		_index = (byte)index;
		_kind = kind;
		_amount = amount;
	}

	public void Write(BinaryWriter w)
	{
		w.Write((byte)_group);
		w.Write(_index);
		w.Write((byte)_kind);
		w.WriteItem(_cursor);
		w.Write(_amount);
	}

	public void Read(BinaryReader r)
	{
		_group = (SlotGroup)r.ReadByte();
		_index = r.ReadByte();
		_kind = (Kind)r.ReadByte();
		_cursor = r.ReadItem();
		_amount = r.ReadInt32();
	}

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is global::GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock.Part.ObjectHolderMachine holder && holder.IsLocked)
		{
			if (_kind == Kind.Deposit)
				WriteBackCursor(byWhoAmI, _cursor, CursorUpdatePacket.Delivery.CursorMerge);
			else if (_kind == Kind.ShiftClickIn)
				WriteBackCursor(byWhoAmI, _cursor, CursorUpdatePacket.Delivery.PlayerInventory);
			return;
		}

		if (_kind == Kind.ShiftClickIn) { ApplyShiftIn(entity, byWhoAmI); return; }

		var slots = entity.GetSlotGroup(_group);
		if (slots is null || _index >= slots.Length)
		{
			if (_kind == Kind.Deposit)
				WriteBackCursor(byWhoAmI, _cursor, CursorUpdatePacket.Delivery.CursorMerge);
			return;
		}

		switch (_kind)
		{
			case Kind.Deposit:       ApplyDeposit(slots, entity, byWhoAmI); break;
			case Kind.Pickup:        ApplyPickup(slots, entity, byWhoAmI); break;
			case Kind.ShiftClickOut: ApplyShiftOut(slots, entity, byWhoAmI); break;
		}
	}

	private void ApplyDeposit(Item[] slots, MetaMachine entity, int byWhoAmI)
	{
		if (_group != SlotGroup.InventoryOutput && !_cursor.IsAir)
		{
			ref Item slot = ref slots[_index];
			int before = _cursor.stack;
			if (slot.IsAir)
			{
				var placed = _cursor.Clone();
				placed.stack = System.Math.Min(_cursor.stack, _cursor.maxStack);
				slot = placed;
				_cursor.stack -= placed.stack;
			}
			else if (slot.type == _cursor.type && slot.stack < slot.maxStack && CanStack(slot, _cursor))
			{
				int moved = System.Math.Min(slot.maxStack - slot.stack, _cursor.stack);
				slot.stack += moved;
				_cursor.stack -= moved;
			}
			if (_cursor.stack <= 0) _cursor.TurnToAir();
			if (_cursor.stack != before) entity.NotifySlotGroupChanged(_group);
		}
		WriteBackCursor(byWhoAmI, _cursor, CursorUpdatePacket.Delivery.CursorMerge);
	}

	private void ApplyPickup(Item[] slots, MetaMachine entity, int byWhoAmI)
	{
		ref Item slot = ref slots[_index];
		var taken = new Item();
		if (!slot.IsAir)
		{
			int take = _amount == 1 ? (slot.stack + 1) / 2 : slot.stack;
			taken = slot.Clone();
			taken.stack = take;
			slot.stack -= take;
			if (slot.stack <= 0) slot = new Item();
			entity.NotifySlotGroupChanged(_group);
		}
		WriteBackCursor(byWhoAmI, taken, CursorUpdatePacket.Delivery.CursorMerge);
	}

	private void ApplyShiftOut(Item[] slots, MetaMachine entity, int byWhoAmI)
	{
		ref Item slot = ref slots[_index];
		if (slot.IsAir) return;
		int take = System.Math.Min(_amount, slot.stack);
		if (take <= 0) return;
		var detached = slot.Clone();
		detached.stack = take;
		slot.stack -= take;
		if (slot.stack <= 0) slot = new Item();
		entity.NotifySlotGroupChanged(_group);
		WriteBackCursor(byWhoAmI, detached, CursorUpdatePacket.Delivery.PlayerInventory);
	}

	private void ApplyShiftIn(MetaMachine entity, int byWhoAmI)
	{
		var (slots, group) = ResolveShiftInSlots(entity);
		if (slots is not null)
		{
			DepositInto(slots, _cursor);
			entity.NotifySlotGroupChanged(group);
		}
		WriteBackCursor(byWhoAmI, _cursor, CursorUpdatePacket.Delivery.PlayerInventory);
	}

	public static (Item[]? slots, SlotGroup group) ResolveShiftInSlots(MetaMachine entity)
	{
		if (entity.GetSlotGroup(SlotGroup.InventoryInput) is { } input)
			return (input, SlotGroup.InventoryInput);
		if (entity.GetSlotGroup(SlotGroup.Inventory) is { } inv)
			return (inv, SlotGroup.Inventory);
		return (null, default);
	}

	public static int FitCapacity(Item[] slots, Item item, int from = 0, int count = -1)
	{
		if (item.IsAir) return 0;
		int end = count < 0 ? slots.Length : System.Math.Min(slots.Length, from + count);
		long cap = 0;
		for (int i = from; i < end && cap < item.stack; i++)
		{
			var s = slots[i];
			if (s.IsAir) cap += item.maxStack;
			else if (s.type == item.type && s.stack < s.maxStack && CanStack(s, item))
				cap += s.maxStack - s.stack;
		}
		return (int)System.Math.Min(cap, item.stack);
	}

	public static void DepositInto(Item[] slots, Item item)
	{
		for (int i = 0; i < slots.Length && item.stack > 0; i++)
		{
			ref Item s = ref slots[i];
			if (s.IsAir || s.type != item.type || s.stack >= s.maxStack || !CanStack(s, item)) continue;
			int moved = System.Math.Min(s.maxStack - s.stack, item.stack);
			s.stack += moved;
			item.stack -= moved;
		}
		for (int i = 0; i < slots.Length && item.stack > 0; i++)
		{
			ref Item s = ref slots[i];
			if (!s.IsAir) continue;
			var placed = item.Clone();
			placed.stack = System.Math.Min(item.maxStack, item.stack);
			item.stack -= placed.stack;
			s = placed;
		}
		if (item.stack <= 0) item.TurnToAir();
	}

	private static bool CanStack(Item a, Item b) =>
		a.type == b.type && a.prefix == b.prefix && a.maxStack > 1 &&
		Terraria.ModLoader.ItemLoader.CanStack(a, b);

	private static void WriteBackCursor(int byWhoAmI, Item result, CursorUpdatePacket.Delivery delivery)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			CursorUpdatePacket.SendTo(byWhoAmI, result, delivery);
			return;
		}
		switch (delivery)
		{
			case CursorUpdatePacket.Delivery.Cursor:
				Main.mouseItem = result;
				break;
			case CursorUpdatePacket.Delivery.CursorMerge:
				MergeOntoCursor(result);
				break;
			case CursorUpdatePacket.Delivery.PlayerInventory:
				if (result.IsAir) return;
				global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(
					Main.LocalPlayer, Main.LocalPlayer.GetSource_Misc("gtceu_cursor_overflow"), result);
				break;
		}
	}

	public static void MergeOntoCursor(Item item)
	{
		if (item is null || item.IsAir) return;
		if (Main.mouseItem.IsAir) { Main.mouseItem = item; return; }
		if (Main.mouseItem.type == item.type && CanStack(Main.mouseItem, item))
		{
			int add = System.Math.Min(Main.mouseItem.maxStack - Main.mouseItem.stack, item.stack);
			Main.mouseItem.stack += add;
			item.stack -= add;
			if (item.stack <= 0) return;
		}
		global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(
			Main.LocalPlayer, Main.LocalPlayer.GetSource_Misc("gtceu_cursor_overflow"), item);
	}
}
