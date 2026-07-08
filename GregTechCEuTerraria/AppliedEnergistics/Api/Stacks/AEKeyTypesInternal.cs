// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.api.stacks.AEKeyTypesInternal), Forge 1.20.1. Original is unheadered;
// AE2 is LGPL-3.0-only (older API files MIT). See AE2's LICENSE.

#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Core;

namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

internal static class AEKeyTypesInternal
{
	private static readonly List<AEKeyType> _byRawId = new();
	private static readonly Dictionary<string, AEKeyType> _byId = new();

	public static void Register(AEKeyType keyType)
	{
		if (_byId.ContainsKey(keyType.Id))
		{
			AELog.Warn("Duplicate AEKeyType registration for id '%s' ignored", keyType.Id);
			return;
		}
		_byId[keyType.Id] = keyType;
		_byRawId.Add(keyType);
	}

	public static AEKeyType? GetValue(int rawId) =>
		rawId >= 0 && rawId < _byRawId.Count ? _byRawId[rawId] : null;

	public static AEKeyType? GetValue(string id) =>
		_byId.TryGetValue(id, out var t) ? t : null;

	public static int GetID(AEKeyType keyType) => _byRawId.IndexOf(keyType);

	public static IReadOnlyList<AEKeyType> GetAll() => _byRawId;

	public static void Clear()
	{
		_byRawId.Clear();
		_byId.Clear();
	}
}
