// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.stacks.AEKeyType), Forge 1.20.1. Original MIT header preserved
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using GregTechCEuTerraria.AppliedEnergistics.Util;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

// Defines the properties of a specific subclass of AEKey (e.g. AEItemKey -> AEItemKeys).
public abstract class AEKeyType
{
	private readonly AEKeyFilter _filter;
	private readonly LocalizedText _description;

	public string Id { get; }
	public Type KeyClass { get; }

	protected AEKeyType(string id, Type keyClass, LocalizedText description)
	{
		if (keyClass == typeof(AEKey))
			throw new ArgumentException("Can't register a key type for AEKey itself");
		Id = id;
		KeyClass = keyClass;
		_filter = new TypeFilter(this);
		_description = description;
	}

	public static AEKeyType Items() => AEItemKeys.INSTANCE;

	public static AEKeyType Fluids() => AEFluidKeys.INSTANCE;

	public static AEKeyType? FromRawId(int id) => AEKeyTypesInternal.GetValue(id);

	public string GetId() => Id;

	public Type GetKeyClass() => KeyClass;

	public byte GetRawId()
	{
		var id = AEKeyTypesInternal.GetID(this);
		if (id < 0 || id > 127)
			throw new InvalidOperationException("Key type " + this + " has an invalid numeric id: " + id);
		return (byte)id;
	}

	public virtual int GetAmountPerOperation() => 1;

	public virtual int GetAmountPerByte() => 8;

	public abstract AEKey? ReadFromPacket(BinaryReader input);

	public abstract AEKey? LoadKeyFromTag(TagCompound tag);

	public AEKey? TryCast(AEKey key) => KeyClass.IsInstanceOfType(key) ? key : null;

	public bool Contains(AEKey key) => KeyClass.IsInstanceOfType(key);

	public AEKeyFilter Filter() => _filter;

	public override string ToString() => Id;

	public string GetDescription() => _description.Value;

	public virtual string? GetUnitSymbol() => null;

	public virtual int GetAmountPerUnit() => 1;

	public virtual IReadOnlyCollection<string> GetTagNames() => Array.Empty<string>();

	public string FormatAmount(long amount, AmountFormat format) => format switch
	{
		AmountFormat.FULL => FormatFullAmount(amount),
		AmountFormat.SLOT => FormatShortAmount(amount, 4),
		AmountFormat.SLOT_LARGE_FONT => FormatShortAmount(amount, 3),
		_ => FormatFullAmount(amount),
	};

	private string FormatFullAmount(long amount)
	{
		var result = new StringBuilder();
		if (GetAmountPerUnit() > 1)
		{
			var units = amount / (double)GetAmountPerUnit();
			result.Append(units.ToString("#,##0.###", CultureInfo.CurrentCulture));
		}
		else
		{
			result.Append(amount.ToString("N0", CultureInfo.CurrentCulture));
		}
		var unit = GetUnitSymbol();
		if (unit != null)
			result.Append(' ').Append(unit);
		return result.ToString();
	}

	private string FormatShortAmount(long amount, int maxWidth)
	{
		if (GetAmountPerUnit() > 1)
		{
			var units = amount / (double)GetAmountPerUnit();
			return ReadableNumberConverter.Format(units, maxWidth);
		}
		return ReadableNumberConverter.Format(amount, maxWidth);
	}

	private sealed class TypeFilter : AEKeyFilter
	{
		private readonly AEKeyType _type;
		public TypeFilter(AEKeyType type) { _type = type; }
		public bool Matches(AEKey what) => what.KeyType == _type;
	}
}
