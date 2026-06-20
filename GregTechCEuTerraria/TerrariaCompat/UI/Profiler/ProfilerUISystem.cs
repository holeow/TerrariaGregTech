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
using ProfilerCore = GregTechCEuTerraria.TerrariaCompat.Profiler.Profiler;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Profiler;

public sealed class ProfilerUISystem : ModalUISystem
{
	private static ProfilerUISystem? _instance;
	private ProfilerUIState? _state;

	protected override string LayerName => "GregTechCEuTerraria: Profiler";
	public override bool PinSupported => true;

	public override void Load()  { _instance = this; base.Load(); }
	public override void Unload() { _instance = null; base.Unload(); }

	public static bool IsOpen => _instance?.IsOpenInternal ?? false;

	public static void Open()
	{
		if (!ProfilerCore.Enabled) return;
		if (_instance?.Ui is null) return;
		if (_instance._state is null)
		{
			_instance._state = new ProfilerUIState();
			_instance._state.Activate();
		}
		_instance.Ui.SetState(_instance._state);
		_instance.PushModal();
	}

	public static void Close() => _instance?.CloseInternal();

	public static void Toggle() { if (IsOpen) Close(); else Open(); }

	protected override void AddExtraLayers(List<GameInterfaceLayer> layers)
		=> UILayers.InsertButton(layers,
			"GregTechCEuTerraria: Profiler Button",
			() => { DrawInventoryButton(); return true; });

	private static void DrawInventoryButton()
		=> UILayers.DrawStackedButton(
			slot: 2,
			background: new Color(30, 50, 40),
			drawIcon: (sb, r) =>
			{
				var px = TextureAssets.MagicPixel.Value;
				int gx = r.X + 6, gy = r.Y + 6, gh = 18;
				var bar = new Color(140, 220, 180);
				sb.Draw(px, new Rectangle(gx,      gy + gh - 6,  4, 6),  bar);
				sb.Draw(px, new Rectangle(gx + 7,  gy + gh - 12, 4, 12), bar);
				sb.Draw(px, new Rectangle(gx + 14, gy + gh - 18, 4, 18), bar);
			},
			tooltip: "Open Profiler  (Shift+Click: snapshot, panel closed)",
			onClick: () =>
			{
				bool shift = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift)
				          || Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
				if (shift)
				{
					string path = global::GregTechCEuTerraria.TerrariaCompat.Profiler.ProfilerSystem.DumpToFile();
					Main.NewText($"[GregTech] Profile saved to {path}", 180, 220, 255);
				}
				else Toggle();
			},
			visible: () => ProfilerCore.Enabled);
}
