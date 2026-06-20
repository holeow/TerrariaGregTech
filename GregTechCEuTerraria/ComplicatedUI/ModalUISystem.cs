#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public abstract class ModalUISystem : ModSystem, IModalHost
{
	protected UserInterface? Ui;

	protected abstract string LayerName { get; }

	protected virtual bool CloseOnEscape => true;

	public virtual bool PinSupported => false;
	private bool _pinned;
	public bool Pinned
	{
		get => _pinned;
		set => _pinned = value && PinSupported;
	}
	void IModalHost.RequestClose() => CloseInternal();

	private UIState? _pinHidden;

	protected virtual bool ShouldAutoClose() => false;

	protected virtual void OnClose() { }

	protected virtual void AddExtraLayers(List<GameInterfaceLayer> layers) { }

	protected bool IsOpenInternal => Ui?.CurrentState != null;

	protected void PushModal() => UILayers.Push(LayerName);

	protected void CloseInternal()
	{
		if (Ui?.CurrentState == null && _pinHidden == null) return;
		Pinned = false;
		_pinHidden = null;
		OnClose();
		Ui?.SetState(null);
	}

	public override void Load()
	{
		if (Main.dedServ) return;
		Ui = new UserInterface();
		UILayers.RegisterModal(LayerName, () => IsOpenInternal);
		UILayers.RegisterModalCursorProbe(LayerName,
			() => Ui?.CurrentState is UIModalWindow w && w.ContainsCursor());
	}

	public override void Unload() => Ui = null;

	public override void UpdateUI(GameTime gameTime)
	{
		if (PinSupported && Pinned)
		{
			bool blocked = Main.ingameOptionsWindow || Main.gameMenu;
			bool show = Main.playerInventory && !blocked;
			if (show && Ui?.CurrentState == null && _pinHidden != null)
			{ Ui!.SetState(_pinHidden); _pinHidden = null; PushModal(); }
			else if (!show && Ui?.CurrentState is { } cur)
			{ _pinHidden = cur; Ui.SetState(null); }
		}

		if (Ui?.CurrentState is not { } state) return;
		if (state is FreeModalWindow fmw) fmw.Host ??= this;
		bool pinned = PinSupported && Pinned;
		if (!pinned && (!Main.playerInventory || Main.ingameOptionsWindow || Main.gameMenu
			|| ShouldAutoClose()))
		{ CloseInternal(); return; }
		ModalEscape.SuppressVanillaUIClicks(state);
		bool covered = UILayers.IsCursorOverHigherModal(LayerName);
		if (covered) ModalEscape.WithCursorParked(() => Ui.Update(gameTime));
		else         Ui.Update(gameTime);
	}

	public override void PostUpdateInput()
	{
		if (Main.dedServ || Ui?.CurrentState is not { } state) return;
		ModalEscape.SuppressItemUse(state);
		if (CloseOnEscape && !Pinned && ModalEscape.EscJustPressed && UILayers.IsTopModal(LayerName))
		{
			CloseInternal();
			ModalEscape.ConsumeEscape();
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		AddExtraLayers(layers);
		UILayers.InsertModal(layers, LayerName, () =>
		{
			if (Ui?.CurrentState != null)
			{
				bool occluded = UILayers.IsCursorOverHigherModal(LayerName);
				if (occluded) ModalEscape.WithCursorParked(() => Ui.Draw(Main.spriteBatch, new GameTime()));
				else          Ui.Draw(Main.spriteBatch, new GameTime());
			}
			return true;
		});
	}
}
