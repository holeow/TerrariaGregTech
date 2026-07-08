#nullable enable
using System.IO;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using GregTechCEuTerraria.AppliedEnergistics.Helpers;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MeTerminalAction : IMachineAction
{
	private enum Op : byte { Grid = 0, ShiftInsert = 1 }

	public PacketType Type => PacketType.MeTerminalAction;

	private Op _op;
	private long _serial;
	private InventoryAction _action;
	private Item _cursor = new();

	public MeTerminalAction() { }

	public static MeTerminalAction OfGrid(long serial, InventoryAction action, Item cursor) =>
		new() { _op = Op.Grid, _serial = serial, _action = action, _cursor = cursor.Clone() };

	public static MeTerminalAction OfShiftInsert(Item stack) =>
		new() { _op = Op.ShiftInsert, _cursor = stack.Clone() };

	public void Write(BinaryWriter w)
	{
		w.Write((byte)_op);
		if (_op == Op.Grid)
		{
			w.Write7BitEncodedInt64(_serial);
			w.Write((byte)_action);
		}
		w.WriteItem(_cursor);
	}

	public void Read(BinaryReader r)
	{
		_op = (Op)r.ReadByte();
		if (_op == Op.Grid)
		{
			_serial = r.Read7BitEncodedInt64();
			_action = (InventoryAction)r.ReadByte();
		}
		_cursor = r.ReadItem();
	}

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is not MeTerminalMachine term) return;
		var net = term.Network;

		if (_op == Op.ShiftInsert)
		{
			var leftover = net is null ? _cursor : InsertWhole(net.GetStorage(), IActionSource.Empty(), _cursor);
			DeliverToInventory(byWhoAmI, leftover);
			return;
		}

		if (net is null) return;
		var storage = net.GetStorage();
		var source = IActionSource.Empty();

		AEKey? clickedKey = _serial == -1 ? null : term.ResolveSerial(byWhoAmI, _serial);
		HandleNetworkInteraction(byWhoAmI, storage, source, clickedKey, _action);
	}

	private void HandleNetworkInteraction(int whoAmI, MEStorage storage, IActionSource source,
		AEKey? clickedKey, InventoryAction action)
	{
		if (action == InventoryAction.PICKUP_OR_SET_DOWN && clickedKey is AEFluidKey)
			action = InventoryAction.FILL_ITEM;

		if (action == InventoryAction.SPLIT_OR_PLACE_SINGLE && CursorHasContainedFluid())
			action = InventoryAction.EMPTY_ITEM;

		if (action == InventoryAction.FILL_ITEM)
			TryFillContainer(whoAmI, storage, source, clickedKey, moveToPlayer: false);
		else if (action == InventoryAction.SHIFT_CLICK)
			TryFillContainer(whoAmI, storage, source, clickedKey, moveToPlayer: true);
		else if (action == InventoryAction.EMPTY_ITEM)
			HandleEmptyHeldItem(whoAmI, storage, source);
		else if (action == InventoryAction.AUTO_CRAFT)
			return;

		if (clickedKey == null)
		{
			if (action == InventoryAction.SPLIT_OR_PLACE_SINGLE || action == InventoryAction.ROLL_DOWN)
				PutCarriedIntoNetwork(whoAmI, storage, source, singleItem: true);
			else if (action == InventoryAction.PICKUP_OR_SET_DOWN)
				PutCarriedIntoNetwork(whoAmI, storage, source, singleItem: false);
			return;
		}

		if (clickedKey is not AEItemKey clickedItem)
			return;

		switch (action)
		{
			case InventoryAction.SHIFT_CLICK:
				MoveOneStackToPlayer(whoAmI, storage, source, clickedItem);
				break;

			case InventoryAction.ROLL_DOWN:
			{
				if (!_cursor.IsAir)
				{
					var what = AEItemKey.Of(_cursor);
					if (what != null && storage.Insert(what, 1, Actionable.MODULATE, source) > 0)
					{
						var c = _cursor.Clone(); c.stack -= 1;
						CursorUpdatePacket.SetCursor(whoAmI, c.stack <= 0 ? new Item() : c);
					}
				}
				break;
			}

			case InventoryAction.ROLL_UP:
			case InventoryAction.PICKUP_SINGLE:
			{
				if (!_cursor.IsAir)
				{
					if (_cursor.stack >= _cursor.maxStack) return;
					if (!clickedItem.Matches(_cursor)) return;
				}
				long extracted = storage.Extract(clickedItem, 1, Actionable.MODULATE, source);
				if (extracted > 0)
				{
					if (_cursor.IsAir) CursorUpdatePacket.SetCursor(whoAmI, clickedItem.ToStack(1));
					else { var c = _cursor.Clone(); c.stack += 1; CursorUpdatePacket.SetCursor(whoAmI, c); }
				}
				break;
			}

			case InventoryAction.PICKUP_OR_SET_DOWN:
			{
				if (!_cursor.IsAir)
				{
					PutCarriedIntoNetwork(whoAmI, storage, source, singleItem: false);
				}
				else
				{
					long extracted = storage.Extract(clickedItem, clickedItem.GetMaxStackSize(),
						Actionable.MODULATE, source);
					CursorUpdatePacket.SetCursor(whoAmI, extracted > 0 ? clickedItem.ToStack((int)extracted) : new Item());
				}
				break;
			}

			case InventoryAction.SPLIT_OR_PLACE_SINGLE:
			{
				if (!_cursor.IsAir)
				{
					PutCarriedIntoNetwork(whoAmI, storage, source, singleItem: true);
				}
				else
				{
					long extracted = storage.Extract(clickedItem, clickedItem.GetMaxStackSize(),
						Actionable.SIMULATE, source);
					if (extracted > 0)
					{
						extracted = (extracted + 1) >> 1;
						extracted = storage.Extract(clickedItem, extracted, Actionable.MODULATE, source);
					}
					CursorUpdatePacket.SetCursor(whoAmI, extracted > 0 ? clickedItem.ToStack((int)extracted) : new Item());
				}
				break;
			}

			case InventoryAction.CREATIVE_DUPLICATE:
				if (Main.GameModeInfo.IsJourneyMode)
					CursorUpdatePacket.SetCursor(whoAmI, clickedItem.ToStack(clickedItem.GetMaxStackSize()));
				break;

			case InventoryAction.MOVE_REGION:
			{
				const int playerInvSlots = 50;
				for (int i = 0; i < playerInvSlots; i++)
					if (!MoveOneStackToPlayer(whoAmI, storage, source, clickedItem))
						break;
				break;
			}
		}
	}

	private bool CursorHasContainedFluid()
	{
		if (_cursor.IsAir) return false;
		if (_cursor.ModItem is FluidBucketItem b && b.Fluid is not null) return true;
		if (_cursor.ModItem is IFluidHandlerItem h && !h.GetTank(0).IsEmpty) return true;
		return !VanillaFluidBridge.StackFor(_cursor.type).IsEmpty;
	}

	private void TryFillContainer(int whoAmI, MEStorage storage, IActionSource source,
		AEKey? clickedKey, bool moveToPlayer)
	{
		if (clickedKey is not AEFluidKey fluidKey) return;
		var tank = new MeStorageFluidTank(storage, source, fluidKey);

		bool grabbedEmptyBucket = false;
		if (_cursor.IsAir && FluidContainerTransfer.FilledBucketTypeFor(fluidKey.GetFluid()) != 0)
		{
			var emptyBucket = new Item();
			emptyBucket.SetDefaults(ItemID.EmptyBucket);
			var bucketKey = AEItemKey.Of(emptyBucket);
			if (bucketKey != null && storage.Extract(bucketKey, 1, Actionable.MODULATE, source) >= 1)
			{
				_cursor = emptyBucket;
				grabbedEmptyBucket = true;
			}
		}

		int carriedBeforeType = _cursor.type;
		bool filled = FluidContainerTransfer.TryFillHeld(tank, ref _cursor, out var overflow);

		if (grabbedEmptyBucket && _cursor.type == ItemID.EmptyBucket)
		{
			var key = AEItemKey.Of(_cursor);
			if (key != null)
			{
				long inserted = storage.Insert(key, _cursor.stack, Actionable.MODULATE, source);
				var c = _cursor.Clone(); c.stack -= (int)inserted;
				_cursor = c.stack <= 0 ? new Item() : c;
			}
		}

		if (!filled && !grabbedEmptyBucket) return;

		if (overflow is { IsAir: false } o) DeliverToInventory(whoAmI, o);
		if (moveToPlayer && _cursor.type != carriedBeforeType && !_cursor.IsAir)
		{
			DeliverToInventory(whoAmI, _cursor);
			_cursor = new Item();
		}
		CursorUpdatePacket.SetCursor(whoAmI, _cursor);
	}

	private void HandleEmptyHeldItem(int whoAmI, MEStorage storage, IActionSource source)
	{
		var tank = new MeStorageFluidTank(storage, source);
		if (!FluidContainerTransfer.TryEmptyHeld(tank, ref _cursor, out var overflow)) return;
		if (overflow is { IsAir: false } o) DeliverToInventory(whoAmI, o);
		CursorUpdatePacket.SetCursor(whoAmI, _cursor);
	}

	private void PutCarriedIntoNetwork(int whoAmI, MEStorage storage, IActionSource source, bool singleItem)
	{
		if (_cursor.IsAir) return;
		var what = AEItemKey.Of(_cursor);
		if (what == null) return;
		int amount = singleItem ? 1 : _cursor.stack;
		long inserted = storage.Insert(what, amount, Actionable.MODULATE, source);
		long leftover = _cursor.stack - inserted;
		var rem = new Item();
		if (leftover > 0) { rem = _cursor.Clone(); rem.stack = (int)leftover; }
		CursorUpdatePacket.SetCursor(whoAmI, rem);
	}

	private bool MoveOneStackToPlayer(int whoAmI, MEStorage storage, IActionSource source, AEItemKey key)
	{
		int max = key.GetMaxStackSize();
		long extracted = storage.Extract(key, max, Actionable.MODULATE, source);
		if (extracted <= 0) return false;
		DeliverToInventory(whoAmI, key.ToStack((int)extracted));
		return true;
	}

	private static Item InsertWhole(MEStorage storage, IActionSource source, Item stack)
	{
		if (stack.IsAir) return new Item();
		var key = AEItemKey.Of(stack);
		if (key == null) return stack;
		long inserted = storage.Insert(key, stack.stack, Actionable.MODULATE, source);
		long leftover = stack.stack - inserted;
		if (leftover <= 0) return new Item();
		var rem = stack.Clone(); rem.stack = (int)leftover;
		return rem;
	}


	private static void DeliverToInventory(int whoAmI, Item item)
	{
		if (item.IsAir) return;
		if (Main.netMode == NetmodeID.Server)
			CursorUpdatePacket.SendTo(whoAmI, item, CursorUpdatePacket.Delivery.PlayerInventory);
		else if (whoAmI >= 0 && whoAmI < Main.maxPlayers)
			global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(
				Main.player[whoAmI], Main.player[whoAmI].GetSource_OpenItem(item.type), item);
	}
}
