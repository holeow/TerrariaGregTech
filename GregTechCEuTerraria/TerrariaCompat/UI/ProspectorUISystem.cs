#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class ProspectorUISystem : ModalUISystem
{
	private ProspectorUIState? _state;

	protected override string LayerName => "GregTechCEuTerraria: Prospector UI";
	protected override bool CloseOnEscape => false;

	public override void Load()
	{
		base.Load();
		if (Ui != null) _state = new ProspectorUIState();
	}

	public override void Unload()
	{
		base.Unload();
		_state = null;
	}

	public static void OpenFor(Item prospector)
	{
		var sys = ModContent.GetInstance<ProspectorUISystem>();
		if (sys?.Ui is null || sys._state is null) return;
		sys._state.Bind(prospector);
		sys.Ui.SetState(sys._state);
		sys.PushModal();
		Main.playerInventory = true;
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close() => ModContent.GetInstance<ProspectorUISystem>()?.CloseInternal();

	public static bool IsOpen => ModContent.GetInstance<ProspectorUISystem>()?.IsOpenInternal ?? false;

	protected override bool ShouldAutoClose()
		=> _state is null || !ProspectorStillHeld(_state.Prospector);

	protected override void OnClose()
	{
		_state?.Unbind();
		Widgets.UITextField.UnfocusAll();
		SoundEngine.PlaySound(SoundID.MenuClose);
	}

	private static bool ProspectorStillHeld(Item? prospector)
	{
		if (prospector is null || prospector.IsAir) return false;
		var inv = Main.LocalPlayer.inventory;
		for (int i = 0; i < inv.Length; i++)
			if (ReferenceEquals(inv[i], prospector)) return true;
		return false;
	}
}
