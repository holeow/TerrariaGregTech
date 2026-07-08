// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.networking.security.IActionSource), Forge 1.20.1. Original MIT
// header preserved verbatim below per AE2's license terms.
//
// The MIT License (MIT)
//
// Copyright (c) 2013 AlgorithmX2
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#nullable enable
using Terraria;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;

public interface IActionSource
{
	static IActionSource Empty() => ActionSource.EMPTY;

	static IActionSource OfPlayer(Player player) => new ActionSource(player);

	Player? GetPlayer();

	T? GetContext<T>() where T : class;
}

internal sealed class ActionSource : IActionSource
{
	internal static readonly ActionSource EMPTY = new(null);

	private readonly Player? _player;

	public ActionSource(Player? player) => _player = player;

	public Player? GetPlayer() => _player;

	public T? GetContext<T>() where T : class => null;
}
