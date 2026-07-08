#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.UI;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Lore;

public sealed class LoreUISystem : ModalUISystem
{
	private static LoreUISystem? _instance;
	private LoreUIState? _state;

	protected override string LayerName => "GregTechCEuTerraria: Lore";
	public override bool PinSupported => true;

	public override void Load()  { _instance = this; base.Load(); }
	public override void Unload() { _instance = null; base.Unload(); }

	public static bool IsOpen => _instance?.IsOpenInternal ?? false;

	public static void Open(string title, IReadOnlyList<string> lines)
	{
		if (_instance?.Ui is null)
			return;
		_instance._state = new LoreUIState(title, lines) { Host = _instance };
		_instance._state.Activate();
		_instance.Ui.SetState(_instance._state);
		_instance.PushModal();
		Main.playerInventory = true;
	}

	public static void Close() => _instance?.CloseInternal();
}
