#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

// Tied to Main.playerInventory so the inventory panel shows alongside
public sealed class MachineUISystem : ModalUISystem
{
	private MachineUIState? _state;

	private const string LayerNameStr = "GregTechCEuTerraria: Machine UI";
	protected override string LayerName => LayerNameStr;
	protected override bool CloseOnEscape => false;

	public override void Load()
	{
		base.Load();
		if (Ui != null) _state = new MachineUIState();
	}

	public override void Unload()
	{
		base.Unload();
		_state = null;
	}

	public static void OpenFor(MetaMachine entity, MachineUILayout layout)
	{
		var sys = ModContent.GetInstance<MachineUISystem>();
		if (sys?.Ui is null || sys._state is null) return;
		ModUIRegistry.OnOpen(Close);
		CloseVanillaChest();
		sys._state.Bind(entity, layout);
		sys.Ui.SetState(sys._state);
		sys.PushModal();
		Main.playerInventory = true;
		SoundEngine.PlaySound(SoundID.MenuOpen);
		if (Main.netMode == NetmodeID.MultiplayerClient)
			TerrariaCompat.Net.MachineViewPacket.SendBegin(entity.Position);
	}

	public static void Close() => ModContent.GetInstance<MachineUISystem>()?.CloseInternal();

	protected override void OnClose()
	{
		var entity = _state?.Entity;
		_state?.Unbind();
		ModUIRegistry.OnClose(Close);
		Widgets.UISearchBar.UnfocusAll();
		Widgets.UITextField.UnfocusAll();
		SoundEngine.PlaySound(SoundID.MenuClose);
		if (Main.netMode == NetmodeID.MultiplayerClient && entity != null)
			TerrariaCompat.Net.MachineViewPacket.SendEnd(entity.Position);
	}

	private static void CloseVanillaChest()
	{
		var plr = Main.LocalPlayer;
		if (plr.chest == -1) return;
		plr.chest = -1;
		Main.recBigList = false;
		Terraria.Recipe.FindRecipes();
		SoundEngine.PlaySound(SoundID.MenuClose);
		if (Main.netMode == NetmodeID.MultiplayerClient)
			NetMessage.SendData(MessageID.SyncPlayerChestIndex, -1, -1, null, Main.myPlayer, -1f);
	}

	public static bool IsOpen => ModContent.GetInstance<MachineUISystem>()?.IsOpenInternal ?? false;

public static MetaMachine? CurrentEntity
		=> ModContent.GetInstance<MachineUISystem>()?._state?.Entity;

	protected override bool ShouldAutoClose()
	{
		if (Main.LocalPlayer.chest != -1) return true;

		var bound = _state?.Entity;
		if (bound == null) return false;
		if (!TileEntity.ByID.ContainsKey(bound.ID)) return true;

		foreach (var (cx, cy) in bound.Cells())
			if (Main.LocalPlayer.IsInTileInteractionRange(cx, cy, TileReachCheckSettings.Simple))
				return false;
		return true;
	}
}
