#nullable enable
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class MeStationBar : UIElement
{
	public const int SlotSize = 40;
	public const int SlotGap = 2;
	public static int BarWidth => CraftingStationState.StationSlots * (SlotSize + SlotGap) - SlotGap;

	public MeStationBar(IMeCraftingHost term)
	{
		for (int i = 0; i < CraftingStationState.StationSlots; i++)
		{
			Append(new UISlot(term.Machine, term.StationSlotGroup, i, ItemSlot.Context.ChestItem)
			{
				Left   = StyleDimension.FromPixels(i * (SlotSize + SlotGap)),
				Top    = StyleDimension.FromPixels(0),
				Width  = StyleDimension.FromPixels(SlotSize),
				Height = StyleDimension.FromPixels(SlotSize),
			});
		}
	}
}
