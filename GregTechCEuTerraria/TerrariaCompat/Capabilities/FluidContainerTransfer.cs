#nullable enable
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Capabilities;

public static class FluidContainerTransfer
{
	public static int FilledBucketTypeFor(FluidType fluid)
	{
		int t = VanillaFluidBridge.FilledVersion(ItemID.EmptyBucket, fluid);
		if (t == 0) t = FluidBucketRegistry.Get(fluid.Id) ?? 0;
		return t;
	}

	public static bool TryTransfer(IFluidHandler tank, ref Item cursor,
		(bool allowFill, bool allowDrain) caps, out Item? overflow)
	{
		var op = new Op(cursor);
		bool changed = op.RunAuto(tank, caps.allowFill, caps.allowDrain);
		cursor = op.Cursor;
		overflow = op.Overflow;
		return changed;
	}

	public static bool TryFillHeld(IFluidHandler tank, ref Item cursor, out Item? overflow,
		bool allowDrain = true)
	{
		var op = new Op(cursor);
		bool changed = op.RunFill(tank, allowDrain);
		cursor = op.Cursor;
		overflow = op.Overflow;
		return changed;
	}

	public static bool TryEmptyHeld(IFluidHandler tank, ref Item cursor, out Item? overflow,
		bool allowFill = true)
	{
		var op = new Op(cursor);
		bool changed = op.RunEmpty(tank, allowFill);
		cursor = op.Cursor;
		overflow = op.Overflow;
		return changed;
	}

	private sealed class Op
	{
		public Item Cursor;
		public Item? Overflow;

		public Op(Item cursor) => Cursor = cursor;

		public bool RunAuto(IFluidHandler tank, bool allowFill, bool allowDrain)
		{
			if (Cursor.IsAir) return false;
			return IsFillTarget(Cursor) ? RunFill(tank, allowDrain) : RunEmpty(tank, allowFill);
		}

		private static bool IsFillTarget(Item c) =>
			c.type == ItemID.EmptyBucket
			|| (c.ModItem is not FluidBucketItem && c.ModItem is IFluidHandlerItem h && h.GetTank(0).IsEmpty);

		public bool RunFill(IFluidHandler tank, bool allowDrain)
		{
			if (Cursor.IsAir) return false;

			if (Cursor.ModItem is not FluidBucketItem && Cursor.ModItem is IFluidHandlerItem container
				&& container.GetTank(0).IsEmpty)
			{
				if (!allowDrain) return false;
				FillContainerFromTank(tank, container);
				return true;
			}

			if (Cursor.type == ItemID.EmptyBucket)
			{
				if (!allowDrain) return false;
				var stored = tank.GetTank(0);
				if (stored.IsEmpty) return false;
				int filledType = FilledBucketTypeFor(stored.Type!);
				if (filledType == 0) return false;
				if (tank.Drain(VanillaFluidBridge.BucketAmount, simulate: true).Amount
					< VanillaFluidBridge.BucketAmount)
					return false;
				tank.Drain(VanillaFluidBridge.BucketAmount, simulate: false);
				SwapOneFromCursorByType(filledType);
				return true;
			}

			return false;
		}

		public bool RunEmpty(IFluidHandler tank, bool allowFill)
		{
			if (Cursor.IsAir) return false;

			if (Cursor.ModItem is FluidBucketItem gtBucket && gtBucket.Fluid is { } bucketFluidType)
			{
				if (!allowFill) return false;
				var stack = new FluidStack(bucketFluidType, VanillaFluidBridge.BucketAmount);
				if (tank.Fill(stack, simulate: true) < VanillaFluidBridge.BucketAmount)
					return false;
				tank.Fill(stack, simulate: false);
				SwapOneFromCursorByType(ItemID.EmptyBucket);
				return true;
			}

			if (Cursor.ModItem is IFluidHandlerItem container && !container.GetTank(0).IsEmpty)
			{
				if (!allowFill) return false;
				DrainContainerIntoTank(tank, container);
				return true;
			}

			var bucketFluid = VanillaFluidBridge.StackFor(Cursor.type);
			if (!bucketFluid.IsEmpty)
			{
				if (!allowFill) return false;
				int accepted = tank.Fill(bucketFluid, simulate: true);
				if (accepted < bucketFluid.Amount) return false;
				tank.Fill(bucketFluid, simulate: false);
				int emptyType = VanillaFluidBridge.EmptyVersion(Cursor.type);
				SwapOneFromCursorByType(emptyType);
				return true;
			}

			return false;
		}

		private void FillContainerFromTank(IFluidHandler tank, IFluidHandlerItem container)
		{
			var tankStack = tank.GetTank(0);
			if (tankStack.IsEmpty) return;

			var simDrained = tank.Drain(container.GetCapacity(0), simulate: true);
			if (simDrained.IsEmpty) return;
			int canFill = container.Fill(simDrained, simulate: true);
			if (canFill <= 0) return;
			var moved = simDrained.WithAmount(System.Math.Min(simDrained.Amount, canFill));
			tank.Drain(moved.Amount, simulate: false);

			if (Cursor.stack == 1)
			{
				container.Fill(moved, simulate: false);
			}
			else
			{
				Cursor.stack -= 1;
				Overflow = MakeFilledContainer(moved);
			}
		}

		private void DrainContainerIntoTank(IFluidHandler tank, IFluidHandlerItem container)
		{
			var contents = container.GetTank(0);
			if (contents.IsEmpty) return;
			int accepted = tank.Fill(contents, simulate: true);
			if (accepted <= 0) return;

			tank.Fill(contents.WithAmount(accepted), simulate: false);

			if (Cursor.stack == 1)
			{
				container.Drain(accepted, simulate: false);
			}
			else
			{
				Cursor.stack -= 1;
				Overflow = accepted < contents.Amount
					? MakeFilledContainer(contents.WithAmount(contents.Amount - accepted))
					: MakeEmptyContainer();
			}
		}

		private Item MakeEmptyContainer()
		{
			var item = Cursor.Clone();
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
			if (Cursor.stack == 1)
			{
				Cursor.SetDefaults(swapToType);
			}
			else
			{
				Cursor.stack -= 1;
				Overflow = new Item();
				Overflow.SetDefaults(swapToType);
			}
		}
	}
}
