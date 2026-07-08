#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Api.Util.ValueProviders;

namespace GregTechCEuTerraria.Api.Recipe.Ingredient;

public class IntProviderFluidIngredient : FluidIngredient, IRangedIngredient
{
	public IntProvider CountProvider { get; }

	public int SampledCount { get; set; } = -1;

	public FluidIngredient Inner { get; }

	private FluidStack[]? _fluidStacks;

	private static readonly Random _rng = new();

	protected IntProviderFluidIngredient(FluidIngredient inner, IntProvider countProvider)
		: base(GetExactType(inner)!, 0)  // upstream's ctor super-calls with empty fluid array
	{
		Inner = inner;
		CountProvider = countProvider;
	}

	private static FluidType? GetExactType(FluidIngredient inner) =>
		inner.ExactType ?? (inner.GetFluids().Count > 0 ? inner.GetFluids()[0] : null);

	public static IntProviderFluidIngredient Of(FluidIngredient inner, IntProvider countProvider)
	{
		if (countProvider.GetMinValue() < 0)
			throw new ArgumentException("IntProviderFluidIngredient must have a min value of at least 0.");
		return new IntProviderFluidIngredient(inner, countProvider);
	}

	public FluidStack[] GetMaterialized()
	{
		if (_fluidStacks is null)
		{
			int cachedAmount = RollSampledCount();
			if (cachedAmount == 0)
			{
				_fluidStacks = Array.Empty<FluidStack>();
			}
			else
			{
				var innerFluids = Inner.GetFluids();
				_fluidStacks = new FluidStack[innerFluids.Count];
				for (int i = 0; i < innerFluids.Count; i++)
					_fluidStacks[i] = new FluidStack(innerFluids[i], cachedAmount);
			}
		}
		return _fluidStacks;
	}

	public FluidStack[] GetMaxSizeFluid()
	{
		int max = CountProvider.GetMaxValue();
		var innerFluids = Inner.GetFluids();
		var result = new FluidStack[innerFluids.Count];
		for (int i = 0; i < innerFluids.Count; i++)
			result[i] = new FluidStack(innerFluids[i], max);
		return result;
	}

	public override FluidIngredient Copy()
	{
		var ipfi = new IntProviderFluidIngredient(Inner, CountProvider);
		ipfi.SampledCount = SampledCount;
		return ipfi;
	}

	public int RollSampledCount() => RollSampledCount(_rng);

	public int RollSampledCount(Random random)
	{
		if (SampledCount < 0)
			SampledCount = CountProvider.Sample(random);
		return SampledCount;
	}

	public void SetFluidStacks(FluidStack[]? stacks) => _fluidStacks = stacks;

	public IntProvider GetCountProvider() => CountProvider;
	public int GetSampledCount() => SampledCount;
	public void SetSampledCount(int count) => SampledCount = count;
	public double GetMidRoll() => (CountProvider.GetMaxValue() + CountProvider.GetMinValue()) / 2.0;
	public void Reset()
	{
		SampledCount = -1;
		_fluidStacks = null;
	}

	public override bool IsEmpty => Inner.IsEmpty || CountProvider.GetMaxValue() == 0;

	public override string GetTypeName() => "gtceu:int_provider_fluid";

	public override string ToString() => $"IntProviderFluidIngredient(inner={Inner}, count={CountProvider})";
}
