#nullable enable
using System.Collections.Generic;
using System.Text;
using GregTechCEuTerraria.Api.Data.Chemical.Material;
using GregTechCEuTerraria.Api.Fluids.Attribute;
using GregTechCEuTerraria.Api.Fluids.Store;

namespace GregTechCEuTerraria.Api.Fluids;

public sealed class FluidBuilder
{
	internal const int  INFER_TEMPERATURE = -1;
	internal const uint INFER_COLOR       = 0xFFFFFFFF;
	internal const int  INFER_DENSITY     = -1;
	internal const int  INFER_LUMINOSITY  = -1;
	internal const int  INFER_VISCOSITY   = -1;

	private string? _name;
	private string? _translation;
	private readonly List<FluidAttribute> _attributes = new();

	private FluidState? _state;
	private int  _temperature   = INFER_TEMPERATURE;
	private uint _color         = INFER_COLOR;
	private bool _isColorEnabled = true;
	private int  _density       = INFER_DENSITY;
	private int  _luminosity    = INFER_LUMINOSITY;
	private int  _viscosity     = INFER_VISCOSITY;
	private int  _burnTime      = -1;
	private bool _hasFluidBlock;
	private bool _hasBucket = true;

	public FluidBuilder Name(string name)             { _name = name; return this; }
	public FluidBuilder Translation(string t)         { _translation = t; return this; }
	public FluidBuilder State(FluidState state)       { _state = state; return this; }

	public FluidBuilder Temperature(int kelvin)
	{
		if (kelvin < 0) throw new System.ArgumentException("temperature must be >= 0");
		_temperature = kelvin;
		return this;
	}

	public FluidBuilder Color(uint color)
	{
		_color = ConvertRgbToArgb(color);
		if (_color == INFER_COLOR) return DisableColor();
		return this;
	}

	public FluidBuilder DisableColor() { _isColorEnabled = false; return this; }

	public FluidBuilder Density(int density) { _density = density; return this; }

	public FluidBuilder Luminosity(int luminosity)
	{
		if (luminosity < 0 || luminosity >= 16)
			throw new System.ArgumentException("luminosity must be >= 0 and < 16");
		_luminosity = luminosity;
		return this;
	}

	public FluidBuilder Viscosity(int mcViscosity)
	{
		if (mcViscosity < 0) throw new System.ArgumentException("viscosity must be >= 0");
		_viscosity = mcViscosity;
		return this;
	}

	public FluidBuilder BurnTime(int burnTime) { _burnTime = burnTime; return this; }

	public FluidBuilder Attribute(FluidAttribute attr) { _attributes.Add(attr); return this; }
	public FluidBuilder Block() { _hasFluidBlock = true; return this; }
	public FluidBuilder DisableBucket() { _hasBucket = false; return this; }

	public string ResolveName(Material material, FluidStorageKey key) =>
		_name ?? key.FluidIdFor(material);

	public FluidType Build(Material? material, FluidStorageKey? key)
	{
		if (_name is null && material is not null && key is not null)
			_name = key.FluidIdFor(material);
		if (_name is null)
			throw new System.InvalidOperationException("Could not determine fluid name");

		FluidState state = _state ?? key?.DefaultState ?? FluidState.LIQUID;

		DetermineTemperature(material, state);
		DetermineColor(material);
		DetermineDensity(state);
		DetermineLuminosity(material, state);
		DetermineViscosity(material, state);

		string displayName = _translation ?? ResolveDisplayName(material, key, _name);
		return new FluidType(_name, displayName, _color, _isColorEnabled, state,
			_temperature, _density, _luminosity, _viscosity, _burnTime,
			_hasFluidBlock, _hasBucket, key, material?.Id, _attributes);
	}

	private void DetermineTemperature(Material? material, FluidState state)
	{
		if (_temperature != INFER_TEMPERATURE) return;
		if (material is null)
		{
			_temperature = FluidConstants.ROOM_TEMPERATURE;
			return;
		}
		if (material.BlastTemperatureK is null)
		{
			_temperature = state switch
			{
				FluidState.LIQUID => material.Forms.Contains("DUST")
					? FluidConstants.SOLID_LIQUID_TEMPERATURE
					: FluidConstants.ROOM_TEMPERATURE,
				FluidState.GAS => FluidConstants.ROOM_TEMPERATURE,
				FluidState.PLASMA => DeterminePlasmaTemperature(material),
				_ => FluidConstants.ROOM_TEMPERATURE,
			};
		}
		else
		{
			_temperature = material.BlastTemperatureK.Value + state switch
			{
				FluidState.LIQUID => FluidConstants.LIQUID_TEMPERATURE_OFFSET,
				FluidState.GAS    => FluidConstants.GAS_TEMPERATURE_OFFSET,
				FluidState.PLASMA => FluidConstants.BASE_PLASMA_TEMPERATURE,
				_ => 0,
			};
		}
	}

	private static int DeterminePlasmaTemperature(Material material)
	{
		if (material.HasFluid())
		{
			var primary = material.GetFluidBuilder();
			if (primary is not null && !ReferenceEquals(primary, material.GetFluidBuilder(FluidStorageKey.PLASMA)))
				return FluidConstants.BASE_PLASMA_TEMPERATURE + primary._temperature;
		}
		return FluidConstants.BASE_PLASMA_TEMPERATURE;
	}

	private void DetermineColor(Material? material)
	{
		if (_color != INFER_COLOR) return;
		if (_isColorEnabled && material is not null)
			_color = ConvertRgbToArgb(material.Color ?? 0xFFFFFFu);
	}

	private void DetermineDensity(FluidState state)
	{
		if (_density != INFER_DENSITY) return;
		_density = state switch
		{
			FluidState.LIQUID => FluidConstants.DEFAULT_LIQUID_DENSITY,
			FluidState.GAS    => FluidConstants.DEFAULT_GAS_DENSITY,
			FluidState.PLASMA => FluidConstants.DEFAULT_PLASMA_DENSITY,
			_ => FluidConstants.DEFAULT_LIQUID_DENSITY,
		};
	}

	private void DetermineLuminosity(Material? material, FluidState state)
	{
		if (_luminosity != INFER_LUMINOSITY) return;
		if (state == FluidState.PLASMA)
			_luminosity = 15;
		else if (material is not null)
		{
			if (material.Flags.Contains("PHOSPHORESCENT"))
				_luminosity = 15;
			else if (state == FluidState.LIQUID && material.Forms.Contains("DUST"))
				_luminosity = 10;
			else
				_luminosity = 0;
		}
		else
			_luminosity = 0;
	}

	private void DetermineViscosity(Material? material, FluidState state)
	{
		if (_viscosity != INFER_VISCOSITY) return;
		_viscosity = state switch
		{
			FluidState.LIQUID => material is not null && material.Flags.Contains("STICKY")
				? FluidConstants.STICKY_LIQUID_VISCOSITY
				: FluidConstants.DEFAULT_LIQUID_VISCOSITY,
			FluidState.GAS    => FluidConstants.DEFAULT_GAS_VISCOSITY,
			FluidState.PLASMA => FluidConstants.DEFAULT_PLASMA_VISCOSITY,
			_ => FluidConstants.DEFAULT_LIQUID_VISCOSITY,
		};
	}

	private static uint ConvertRgbToArgb(uint color) =>
		(color & 0xFF000000u) == 0 ? color | 0xFF000000u : color;

	private static string ResolveDisplayName(Material? material, FluidStorageKey? key, string id)
	{
		if (material is null || key is null) return HumanizeId(id);
		string name = HumanizeId(material.Id);
		return key.TranslationKeyFor(material) switch
		{
			"gtceu.fluid.molten"         => $"Molten {name}",
			"gtceu.fluid.liquid_generic" => $"Liquid {name}",
			"gtceu.fluid.plasma"         => $"{name} Plasma",
			"gtceu.fluid.gas_vapor"      => $"{name} Vapor",
			"gtceu.fluid.gas_generic"    => $"{name} Gas",
			_                            => name,
		};
	}

	// "molten_iron" -> "Molten Iron", "iron_plasma" -> "Iron Plasma".
	private static string HumanizeId(string id)
	{
		var sb = new StringBuilder(id.Length);
		bool capNext = true;
		foreach (char c in id)
		{
			if (c == '_') { sb.Append(' '); capNext = true; continue; }
			sb.Append(capNext ? char.ToUpperInvariant(c) : c);
			capNext = false;
		}
		return sb.ToString();
	}
}
