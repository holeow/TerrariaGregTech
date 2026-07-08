#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Tiles.NeonSign;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI.NeonSign;

public sealed class NeonSignUISystem : ModalUISystem
{
	private NeonSignUIState? _state;

	protected override string LayerName => "GregTechCEuTerraria: Neon Sign UI";
	protected override bool CloseOnEscape => true;
	protected override bool CloseOnClickOutside => true;

	public override void Load()
	{
		base.Load();
		if (Ui != null) _state = new NeonSignUIState();
	}

	public override void Unload()
	{
		base.Unload();
		_state = null;
	}

	public static void OpenFor(NeonSignEntity sign)
	{
		var sys = ModContent.GetInstance<NeonSignUISystem>();
		if (sys?.Ui is null || sys._state is null) return;
		sys._state.Bind(sign);
		sys.Ui.SetState(sys._state);
		sys.PushModal();
		Main.playerInventory = true;
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close() => ModContent.GetInstance<NeonSignUISystem>()?.CloseInternal();

	public static bool IsOpen => ModContent.GetInstance<NeonSignUISystem>()?.IsOpenInternal ?? false;

	protected override bool ShouldAutoClose()
	{
		var sign = _state?.Sign;
		if (sign is null) return true;
		if (NeonSignEntity.At(sign.Position.X, sign.Position.Y) is null) return true;
		return !Main.LocalPlayer.IsInTileInteractionRange(sign.Position.X, sign.Position.Y,
			Terraria.DataStructures.TileReachCheckSettings.Simple);
	}

	protected override void OnClose()
	{
		_state?.Unbind();
		UITextField.UnfocusAll();
		SoundEngine.PlaySound(SoundID.MenuClose);
	}
}
