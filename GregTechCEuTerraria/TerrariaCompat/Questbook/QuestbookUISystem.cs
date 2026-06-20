#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.Questbook;

public sealed class QuestbookUISystem : ModalUISystem
{
	private static QuestbookUISystem? _instance;
	private QuestbookUIState? _state;

	protected override string LayerName => "GregTechCEuTerraria: Questbook";
	public override bool PinSupported => true;

	public override void Load()  { _instance = this; base.Load(); }
	public override void Unload() { _instance = null; base.Unload(); }

	public override void PostUpdateInput()
	{
		if (!Main.dedServ && Ui?.CurrentState is { } state
			&& ModalEscape.EscJustPressed && _state is { IsQuestPanelOpen: true } qs)
		{
			qs.CloseQuestPanel();
			ModalEscape.ConsumeEscape();
			ModalEscape.SuppressItemUse(state);
			return;
		}
		base.PostUpdateInput();
	}

	public static bool IsOpen => _instance?.IsOpenInternal ?? false;

	public static void Open()
	{
		if (_instance?.Ui is null)
			return;
		if (_instance._state is null)
		{
			_instance._state = new QuestbookUIState();
			_instance._state.Host = _instance;
			_instance._state.Activate();
		}
		_instance.Ui.SetState(_instance._state);
		_instance.PushModal();
		Main.playerInventory = true;
	}

	public static void Close() => _instance?.CloseInternal();

	public static void Toggle()
	{
		if (IsOpen)
			Close();
		else
			Open();
	}

	protected override void AddExtraLayers(List<GameInterfaceLayer> layers)
		=> UILayers.InsertButton(layers,
			"GregTechCEuTerraria: Questbook Button",
			() => { DrawInventoryButton(); return true; });

	private static void DrawInventoryButton()
		=> UILayers.DrawStackedButton(
			slot: 1,
			background: new Color(38, 42, 70),
			drawIcon: (sb, r) =>
			{
				Main.instance.LoadItem(ItemID.Book);
				Texture2D book = TextureAssets.Item[ItemID.Book].Value;
				float scale = 20f / System.Math.Max(book.Width, book.Height);
				sb.Draw(book, r.Center.ToVector2(), null, Color.White, 0f,
					book.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			},
			tooltip: "Open Questbook",
			onClick: Toggle,
			keybind: ModalToggleKeybinds.OpenQuestbook);
}
