#nullable enable
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class MeUpgradeBar : UIElement
{
	public const int SlotSize = 40;
	public const int SlotGap = 2;
	public static int BarWidth => MeModularTerminalMachine.UpgradeSlots * (SlotSize + SlotGap) - SlotGap;

	public MeUpgradeBar(MeModularTerminalMachine term)
	{
		for (int i = 0; i < MeModularTerminalMachine.UpgradeSlots; i++)
		{
			Append(new UISlot(term, SlotGroup.InventoryInput, i, ItemSlot.Context.ChestItem)
			{
				Left   = StyleDimension.FromPixels(i * (SlotSize + SlotGap)),
				Top    = StyleDimension.FromPixels(0),
				Width  = StyleDimension.FromPixels(SlotSize),
				Height = StyleDimension.FromPixels(SlotSize),
			});
		}
	}
}
