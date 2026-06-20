#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class MagnetUISystem : ModalUISystem
{
	private MagnetUIState? _state;

	protected override string LayerName => "GregTechCEuTerraria: Magnet UI";
	protected override bool CloseOnEscape => false;

	public override void Load()
	{
		base.Load();
		if (Ui != null) _state = new MagnetUIState();
	}

	public override void Unload()
	{
		base.Unload();
		_state = null;
	}

	public static void OpenFor(Item magnet)
	{
		var sys = ModContent.GetInstance<MagnetUISystem>();
		if (sys?.Ui is null || sys._state is null) return;
		sys._state.Bind(magnet);
		sys.Ui.SetState(sys._state);
		sys.PushModal();
		Main.playerInventory = true;
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close() => ModContent.GetInstance<MagnetUISystem>()?.CloseInternal();

	public static bool IsOpen => ModContent.GetInstance<MagnetUISystem>()?.IsOpenInternal ?? false;

	protected override bool ShouldAutoClose()
		=> _state is null || !MagnetStillHeld(_state.Magnet);

	protected override void OnClose()
	{
		_state?.Unbind();
		Widgets.UITextField.UnfocusAll();
		SoundEngine.PlaySound(SoundID.MenuClose);
	}

	private static bool MagnetStillHeld(Item? magnet)
	{
		if (magnet is null || magnet.IsAir) return false;
		var inv = Main.LocalPlayer.inventory;
		for (int i = 0; i < inv.Length; i++)
			if (ReferenceEquals(inv[i], magnet)) return true;
		return false;
	}
}
