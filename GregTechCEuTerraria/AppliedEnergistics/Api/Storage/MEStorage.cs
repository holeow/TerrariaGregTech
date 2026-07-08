// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.storage.MEStorage), Forge 1.20.1. Original MIT header preserved
// verbatim below per AE2's license terms.
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
using System;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

public interface MEStorage
{
	bool IsPreferredStorageFor(AEKey what, IActionSource source) => false;

	long Insert(AEKey what, long amount, Actionable mode, IActionSource source) => 0;

	long Extract(AEKey what, long amount, Actionable mode, IActionSource source) => 0;

	void GetAvailableStacks(KeyCounter @out) { }

	string GetDescription();

	KeyCounter GetAvailableStacks()
	{
		var result = new KeyCounter();
		GetAvailableStacks(result);
		return result;
	}

	static void CheckPreconditions(AEKey what, long amount, Actionable mode, IActionSource source)
	{
		if (what is null) throw new ArgumentNullException(nameof(what), "Cannot pass a null key");
		if (source is null) throw new ArgumentNullException(nameof(source), "Cannot pass a null source");
		if (amount < 0) throw new ArgumentException("Cannot pass a negative amount", nameof(amount));
	}
}
