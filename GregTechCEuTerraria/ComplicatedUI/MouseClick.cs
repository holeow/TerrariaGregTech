#nullable enable
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class MouseClick : ModSystem
{
	private static bool _prevLeft, _prevRight, _prevMiddle;

	public static bool LeftPressed { get; private set; }
	public static bool RightPressed { get; private set; }
	public static bool MiddlePressed { get; private set; }
	public static bool LeftReleased { get; private set; }

	public override void PostUpdateInput()
	{
		if (Main.dedServ) return;
		LeftPressed   = Main.mouseLeft   && !_prevLeft;
		RightPressed  = Main.mouseRight  && !_prevRight;
		MiddlePressed = Main.mouseMiddle && !_prevMiddle;
		LeftReleased  = !Main.mouseLeft  && _prevLeft;
		_prevLeft   = Main.mouseLeft;
		_prevRight  = Main.mouseRight;
		_prevMiddle = Main.mouseMiddle;
	}
}
