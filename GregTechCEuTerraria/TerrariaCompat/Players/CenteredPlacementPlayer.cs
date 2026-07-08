#nullable enable
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Players;

public sealed class CenteredPlacementPlayer : ModPlayer
{
	public static readonly HashSet<int> CenteredPlacementTiles = new();

	public const int PlaceUseTime = 2;
	public const int PlaceUseAnimation = 5;

	private bool _gridLocked;
	private int _anchorX;
	private int _anchorY;
	private int _slotX;
	private int _slotY;

	public override bool PreItemCheck()
	{
		var held = Player.HeldItem;
		int createTile = held is not null && !held.IsAir ? held.createTile : 0;
		if (createTile <= 0 || !CenteredPlacementTiles.Contains(createTile))
		{
			_gridLocked = false;
			return true;
		}

		int rawX = (int)(Main.MouseWorld.X / 16f + 0.5f);
		int rawY = (int)(Main.MouseWorld.Y / 16f + 0.5f);

		if (!Player.controlUseItem)
		{
			_gridLocked = false;
			Player.tileTargetX = rawX;
			Player.tileTargetY = rawY;
			return true;
		}

		if (!_gridLocked)
		{
			_gridLocked = true;
			_anchorX = rawX;
			_anchorY = rawY;
			_slotX = 0;
			_slotY = 0;
		}

		_slotX = AdvanceSlot(rawX - _anchorX, _slotX);
		_slotY = AdvanceSlot(rawY - _anchorY, _slotY);
		Player.tileTargetX = _anchorX + _slotX;
		Player.tileTargetY = _anchorY + _slotY;
		return true;
	}

	private static int AdvanceSlot(int rel, int slot)
	{
		if (Math.Abs(rel - slot) >= 2)
			slot = (int)Math.Round(rel / 2.0, MidpointRounding.AwayFromZero) * 2;
		return slot;
	}
}
