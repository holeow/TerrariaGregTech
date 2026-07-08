#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Terminal;

public sealed class MeCraftingStationsSystem : ModalUISystem
{
	private CraftingStationsWindow? _window;
	protected override string LayerName => "GregTechCEuTerraria: ME Crafting Stations";
	protected override bool CloseOnClickOutside => false;

	public override void Load()
	{
		base.Load();
		if (Ui != null) _window = new CraftingStationsWindow();
	}

	public override void Unload()
	{
		base.Unload();
		_window = null;
	}

	public static bool IsOpen => ModContent.GetInstance<MeCraftingStationsSystem>()?.IsOpenInternal ?? false;

	public static void OpenFor(MetaMachine machine)
	{
		var sys = ModContent.GetInstance<MeCraftingStationsSystem>();
		if (sys?.Ui is null || sys._window is null) return;
		sys._window.Bind(machine);
		sys.Ui.SetState(sys._window);
		sys.PushModal(MachineUISystem.LayerNameStr);
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close() => ModContent.GetInstance<MeCraftingStationsSystem>()?.CloseInternal();

	protected override void OnClose()
	{
		_window?.Unbind();
		SoundEngine.PlaySound(SoundID.MenuClose);
	}
}
