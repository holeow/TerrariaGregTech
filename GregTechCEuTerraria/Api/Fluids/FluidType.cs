#nullable enable
using System.Collections.Generic;
using System.Linq;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Api.Fluids.Store;
using GregTechCEuTerraria.Api.Data.Chemical.Material.Properties;
using GregTechCEuTerraria.Api.Fluids.Attribute;

namespace GregTechCEuTerraria.Api.Fluids;

public sealed class FluidType
{
	public string Id { get; }
	public string DisplayName => ResolveDisplayName();
	private readonly string _bakedDisplayName;
	public uint Color { get; }
	public bool IsColorEnabled { get; }
	public FluidState State { get; }
	public int Temperature { get; }
	public int Density { get; }
	public int Luminosity { get; }
	public int Viscosity { get; }
	public int BurnTime { get; }
	public bool HasFluidBlock { get; }
	public bool HasBucket { get; }
	public FluidStorageKey? SourceKey { get; }
	public string? SourceMaterialId { get; }

	private readonly List<FluidAttribute> _attributes;
	public IReadOnlyList<FluidAttribute> Attributes => _attributes;

	internal FluidType(
		string id,
		string displayName,
		uint color,
		bool isColorEnabled,
		FluidState state,
		int temperature,
		int density,
		int luminosity,
		int viscosity,
		int burnTime,
		bool hasFluidBlock,
		bool hasBucket,
		FluidStorageKey? sourceKey,
		string? sourceMaterialId,
		IEnumerable<FluidAttribute>? attributes)
	{
		Id = id;
		_bakedDisplayName = displayName;
		Color = color;
		IsColorEnabled = isColorEnabled;
		State = state;
		Temperature = temperature;
		Density = density;
		Luminosity = luminosity;
		Viscosity = viscosity;
		BurnTime = burnTime;
		HasFluidBlock = hasFluidBlock;
		HasBucket = hasBucket;
		SourceKey = sourceKey;
		SourceMaterialId = sourceMaterialId;
		_attributes = attributes?.ToList() ?? new List<FluidAttribute>();
	}

	public FluidType(string id, string displayName, uint color)
		: this(id, displayName, color, isColorEnabled: true, FluidState.LIQUID,
			temperature: FluidConstants.ROOM_TEMPERATURE, density: FluidConstants.DEFAULT_LIQUID_DENSITY,
			luminosity: 0, viscosity: FluidConstants.DEFAULT_LIQUID_VISCOSITY,
			burnTime: -1, hasFluidBlock: false, hasBucket: true,
			sourceKey: null, sourceMaterialId: null, attributes: null)
	{ }

	public bool HasAttribute(FluidAttribute attr) => _attributes.Contains(attr);

	public bool IsGaseous => State == FluidState.GAS;
	public bool IsPlasma  => State == FluidState.PLASMA;

	private string ResolveDisplayName()
	{
		if (SourceMaterialId != null && Id == SourceMaterialId)
		{
			string key = "Mods.GregTechCEuTerraria.Materials." + SourceMaterialId;
			var text = Terraria.Localization.Language.GetText(key);
			if (text.Value != key)
				return text.Value;
		}
		return _bakedDisplayName;
	}

	public override string ToString() => Id;
}

public static class FluidRegistry
{
	private static readonly Dictionary<string, FluidType> _byId = new();

	public static readonly FluidType Water           = Register(new FluidType("water", "Water", 0x3C64DC));
	public static readonly FluidType Lava            = Register(new FluidType("lava", "Lava", 0xFF6818));
	public static readonly FluidType Steam           = Register(new FluidType("steam", "Steam", 0xC8D8E8));
	public static readonly FluidType DistilledWater  = Register(new FluidType("distilled_water", "Distilled Water", 0x88BBE0));
	public static readonly FluidType Honey           = Register(new FluidType("honey", "Honey", 0xE9A700));
	public static readonly FluidType Shimmer         = Register(new FluidType("shimmer", "Shimmer", 0xF06ED6));

	public static FluidType Register(FluidType fluid)
	{
		_byId[fluid.Id] = fluid;
		return fluid;
	}

	public static FluidType? Get(string id) => _byId.GetValueOrDefault(id);

	public static bool TryGet(string id, out FluidType type)
	{
		if (_byId.TryGetValue(id, out var found))
		{
			type = found;
			return true;
		}
		type = null!;
		return false;
	}

	public static IReadOnlyCollection<FluidType> All => _byId.Values;
}
