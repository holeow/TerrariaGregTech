#nullable enable
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class MachineShiftClickPlayer : ModPlayer
{
	public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
	{
		if (!MachineUISystem.IsOpen) return false;
		if (MachineUISystem.CurrentEntity is not MetaMachine machine) return false;

		if (context != ItemSlot.Context.InventoryItem
		    && context != ItemSlot.Context.InventoryCoin
		    && context != ItemSlot.Context.InventoryAmmo
		    && context != ItemSlot.Context.ChestItem)
			return false;

		if (slot < 0 || slot >= inventory.Length) return false;
		var src = inventory[slot];
		if (src.IsAir) return false;
		if (src.favorited) return false;

		if (machine is TerrariaCompat.AppliedEnergistics.MeTerminalMachine)
		{
			var movingAll = src.Clone();
			inventory[slot].TurnToAir();
			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Main.myPlayer, slot);
			MachineActions.Send(MeTerminalAction.OfShiftInsert(movingAll), machine);
			SoundEngine.PlaySound(SoundID.Grab);
			return true;
		}

		var (slots, group) = SlotAction.ResolveShiftInSlots(machine);
		if (slots is null) return false;

		int amount = SlotAction.FitCapacity(slots, src, 0, -1, machine.GetSlotLimitFor(group, 0));
		if (amount <= 0) return true;

		var moving = src.Clone();
		moving.stack = amount;
		if (amount >= src.stack) inventory[slot].TurnToAir();
		else inventory[slot].stack -= amount;

		if (Main.netMode == NetmodeID.MultiplayerClient)
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Main.myPlayer, slot);

		MachineActions.Send(
			new SlotAction(SlotGroup.Inventory, 0, SlotAction.Kind.ShiftClickIn, moving),
			machine);
		SoundEngine.PlaySound(SoundID.Grab);
		return true;
	}
}
