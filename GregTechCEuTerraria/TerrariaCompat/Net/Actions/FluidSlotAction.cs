#nullable enable
using System.IO;
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class FluidSlotAction : IMachineAction
{
	public PacketType Type => PacketType.FluidSlotAction;

	private byte _tankIndex;
	private Item _cursor = new();

	public FluidSlotAction() { }
	public FluidSlotAction(int tankIndex, Item cursor)
	{
		_tankIndex = (byte)tankIndex;
		_cursor = cursor.Clone();
	}

	public void Write(BinaryWriter w)
	{
		w.Write(_tankIndex);
		w.WriteItem(_cursor);
	}

	public void Read(BinaryReader r)
	{
		_tankIndex = r.ReadByte();
		_cursor = r.ReadItem();
	}

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is not IFluidHandler handler) return;
		if (_cursor.IsAir) return;
		if (_tankIndex >= handler.TankCount) return;

		var tank = handler.GetTankAccess(_tankIndex);

		(bool allowFill, bool allowDrain) = handler.GetTankClickCaps(_tankIndex);

		if (_cursor.ModItem is FluidBucketItem gtBucket && gtBucket.Fluid is { } bucketFluidType)
		{
			if (!allowFill) return;
			var stack = new FluidStack(bucketFluidType, VanillaFluidBridge.BucketAmount);
			if (tank.Fill(stack, simulate: true) < VanillaFluidBridge.BucketAmount)
				return; // not enough room for a full bucket
			tank.Fill(stack, simulate: false);
			SwapOneFromCursorByType(ItemID.EmptyBucket);
			DeliverCursor(byWhoAmI);
			return;
		}

		if (_cursor.ModItem is IFluidHandlerItem container)
		{
			if (container.GetTank(0).IsEmpty)
			{
				if (!allowDrain) return;
				ApplyFillContainerFromTank(tank, container);
			}
			else
			{
				if (!allowFill) return;
				ApplyDrainContainerIntoTank(tank, container);
			}
			DeliverCursor(byWhoAmI);
			return;
		}

		var bucketFluid = VanillaFluidBridge.StackFor(_cursor.type);
		if (!bucketFluid.IsEmpty)
		{
			if (!allowFill) return;
			int accepted = tank.Fill(bucketFluid, simulate: true);
			if (accepted < bucketFluid.Amount) return;
			tank.Fill(bucketFluid, simulate: false);
			int emptyType = VanillaFluidBridge.EmptyVersion(_cursor.type);
			SwapOneFromCursorByType(emptyType);
			DeliverCursor(byWhoAmI);
			return;
		}
		if (_cursor.type == ItemID.EmptyBucket)
		{
			if (!allowDrain) return;
			var stored = tank.GetTank(0);
			if (stored.IsEmpty) return;
			int filledType = VanillaFluidBridge.FilledVersion(_cursor.type, stored.Type!);
			if (filledType == 0)
				filledType = FluidBucketRegistry.Get(stored.Type!.Id) ?? 0;
			if (filledType == 0) return;
			if (tank.Drain(VanillaFluidBridge.BucketAmount, simulate: true).Amount
			    < VanillaFluidBridge.BucketAmount)
				return;
			tank.Drain(VanillaFluidBridge.BucketAmount, simulate: false);
			SwapOneFromCursorByType(filledType);
			DeliverCursor(byWhoAmI);
		}
	}

	private void ApplyFillContainerFromTank(IFluidHandler tank, IFluidHandlerItem container)
	{
		var tankStack = tank.GetTank(0);
		if (tankStack.IsEmpty) return;

		var simDrained = tank.Drain(container.GetCapacity(0), simulate: true);
		if (simDrained.IsEmpty) return;
		int canFill = container.Fill(simDrained, simulate: true);
		if (canFill <= 0) return;
		var moved = simDrained.WithAmount(System.Math.Min(simDrained.Amount, canFill));
		tank.Drain(moved.Amount, simulate: false);

		if (_cursor.stack == 1)
		{
			container.Fill(moved, simulate: false);
		}
		else
		{
			_cursor.stack -= 1;
			_extraDelivery = MakeFilledContainer(moved);
		}
	}

	private void ApplyDrainContainerIntoTank(IFluidHandler tank, IFluidHandlerItem container)
	{
		var contents = container.GetTank(0);
		if (contents.IsEmpty) return;
		int accepted = tank.Fill(contents, simulate: true);
		if (accepted <= 0) return;

		tank.Fill(contents.WithAmount(accepted), simulate: false);

		if (_cursor.stack == 1)
		{
			container.Drain(accepted, simulate: false);
		}
		else
		{
			_cursor.stack -= 1;
			_extraDelivery = accepted < contents.Amount
				? MakeFilledContainer(contents.WithAmount(contents.Amount - accepted))
				: MakeEmptyContainer();
		}
	}

	private Item MakeEmptyContainer()
	{
		var item = _cursor.Clone();
		item.stack = 1;
		if (item.ModItem is IFluidHandlerItem f)
		{
			var s = f.GetTank(0);
			if (!s.IsEmpty) f.Drain(s.Amount, simulate: false);
		}
		return item;
	}

	private Item MakeFilledContainer(FluidStack contents)
	{
		var item = MakeEmptyContainer();
		if (item.ModItem is IFluidHandlerItem f)
			f.Fill(contents, simulate: false);
		return item;
	}

	private void SwapOneFromCursorByType(int swapToType)
	{
		if (swapToType <= 0) return;
		if (_cursor.stack == 1)
		{
			_cursor.SetDefaults(swapToType);
		}
		else
		{
			_cursor.stack -= 1;
			_extraDelivery = new Item();
			_extraDelivery.SetDefaults(swapToType);
		}
	}

	private Item? _extraDelivery;

	private void DeliverCursor(int byWhoAmI)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			CursorUpdatePacket.SendTo(byWhoAmI, _cursor, CursorUpdatePacket.Delivery.Cursor);
			if (_extraDelivery is { IsAir: false } extra)
				CursorUpdatePacket.SendTo(byWhoAmI, extra, CursorUpdatePacket.Delivery.PlayerInventory);
			return;
		}
		Main.mouseItem = _cursor;
		if (_extraDelivery is { IsAir: false } e)
			global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(
				Main.LocalPlayer, Main.LocalPlayer.GetSource_Misc("gtceu_bucket_overflow"), e);
	}
}
