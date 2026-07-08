#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;

public sealed class MeCraftSystem : ModalUISystem
{
	private CraftAmountWindow? _window;
	protected override string LayerName => "GregTechCEuTerraria: ME Craft Amount";
	protected override bool CloseOnClickOutside => true;

	public override void Load()
	{
		base.Load();
		if (Ui != null) _window = new CraftAmountWindow();
	}

	public override void Unload()
	{
		base.Unload();
		_window = null;
	}

	public static bool IsOpen => ModContent.GetInstance<MeCraftSystem>()?.IsOpenInternal ?? false;

	public static void OpenFor(Point16 termPos, AEKey key, long defaultAmount)
	{
		var sys = ModContent.GetInstance<MeCraftSystem>();
		if (sys?.Ui is null || sys._window is null) return;
		sys._window.Bind(termPos, key, defaultAmount);
		sys.Ui.SetState(sys._window);
		sys.PushModal(MachineUISystem.LayerNameStr);
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void OpenForAmount(AEKey key, long defaultAmount, long minAmount, string title,
		string startLabel, string startTooltip, System.Action<long> onConfirm, string? parentLayer = null)
	{
		var sys = ModContent.GetInstance<MeCraftSystem>();
		if (sys?.Ui is null || sys._window is null) return;
		sys._window.Bind(key, defaultAmount, minAmount, title, startLabel, startTooltip, onConfirm);
		sys.Ui.SetState(sys._window);
		sys.PushModal(parentLayer ?? MachineUISystem.LayerNameStr);
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close() => ModContent.GetInstance<MeCraftSystem>()?.CloseInternal();

	protected override void OnClose()
	{
		_window?.Unbind();
		SoundEngine.PlaySound(SoundID.MenuClose);
	}
}
