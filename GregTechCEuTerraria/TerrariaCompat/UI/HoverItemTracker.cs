#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class HoverItemTracker : ModSystem
{
	public enum Kind { None, Item, Fluid }

	public static Kind LastKind { get; private set; } = Kind.None;
	public static int LastHoveredItemType { get; private set; }
	public static string? LastHoveredFluidId { get; private set; }

	private static bool _suppressNextHoverPick;

	public static void SuppressNextHoverPick() => _suppressNextHoverPick = true;

	public override void PostUpdateInput()
	{
		if (_suppressNextHoverPick)
		{
			_suppressNextHoverPick = false;
			return;
		}

		if (Main.HoverItem is { } h && !h.IsAir)
		{
			var vanilla = VanillaFluidBridge.StackFor(h.type);
			if (!vanilla.IsEmpty)
			{
				SetFluid(vanilla.Type!.Id);
			}
			else if (h.ModItem is Api.Capability.IFluidHandlerItem container
				&& container.GetTank(0) is { IsEmpty: false } stack)
			{
				SetFluid(stack.Type!.Id);
			}
			else
			{
				LastHoveredItemType = h.type;
				LastKind = Kind.Item;
			}
		}
	}

	public static void SetFluid(string fluidId)
	{
		if (string.IsNullOrEmpty(fluidId)) return;
		LastHoveredFluidId = fluidId;
		LastKind = Kind.Fluid;
	}

	public static void PushItem(int itemType)
	{
		if (itemType <= 0) return;
		LastHoveredItemType = itemType;
		LastKind = Kind.Item;
	}

	public static void PushFluid(string fluidId) => SetFluid(fluidId);

	public override void OnWorldUnload()
	{
		LastHoveredItemType = ItemID.None;
		LastHoveredFluidId = null;
		LastKind = Kind.None;
	}
}
