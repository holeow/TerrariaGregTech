#nullable enable
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;

public sealed class MeStationCraftSystem : ModalUISystem
{
	private StationCraftWindow? _window;
	protected override string LayerName => "GregTechCEuTerraria: ME Station Craft";
	protected override bool CloseOnClickOutside => true;

	public override void Load()
	{
		base.Load();
		if (Ui != null) _window = new StationCraftWindow();
	}

	public override void Unload()
	{
		base.Unload();
		_window = null;
	}

	public static bool IsOpen => ModContent.GetInstance<MeStationCraftSystem>()?.IsOpenInternal ?? false;

	public static void OpenFor(Point16 termPos, int itemType)
	{
		var sys = ModContent.GetInstance<MeStationCraftSystem>();
		if (sys?.Ui is null || sys._window is null) return;
		sys._window.Bind(termPos, itemType);
		sys.Ui.SetState(sys._window);
		sys.PushModal(MachineUISystem.LayerNameStr);
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close() => ModContent.GetInstance<MeStationCraftSystem>()?.CloseInternal();

	protected override void OnClose()
	{
		_window?.Unbind();
		SoundEngine.PlaySound(SoundID.MenuClose);
	}
}
