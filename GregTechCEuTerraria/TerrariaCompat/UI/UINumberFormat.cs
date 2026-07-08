#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class UINumberFormat
{
	public static string Amount(AEKey? what, long amount) =>
		what is AEFluidKey ? Fluid(amount) : Count(amount);

	public static string Count(long n)
	{
		if (n >= 1_000_000_000) return (n / 1_000_000_000.0).ToString("0.#") + "B";
		if (n >= 1_000_000)     return (n / 1_000_000.0).ToString("0.#") + "M";
		if (n >= 10_000)        return (n / 1_000.0).ToString("0.#") + "k";
		return n.ToString();
	}

	public static string Fluid(long mb)
	{
		if (mb < 1000) return mb + " mB";
		double buckets = mb / 1000.0;
		if (buckets < 1000)           return buckets.ToString("0.#") + " B";
		if (buckets < 1_000_000)      return (buckets / 1000.0).ToString("0.#") + " kB";
		if (buckets < 1_000_000_000)  return (buckets / 1_000_000.0).ToString("0.#") + " MB";
		return (buckets / 1_000_000_000.0).ToString("0.#") + " GB";
	}

	public static string Energy(long v)
	{
		if (v >= 1_000_000_000_000L) return (v / 1_000_000_000_000d).ToString("0.##") + "T";
		if (v >= 1_000_000_000L)     return (v / 1_000_000_000d).ToString("0.##") + "G";
		if (v >= 1_000_000L)         return (v / 1_000_000d).ToString("0.##") + "M";
		if (v >= 1_000L)             return (v / 1_000d).ToString("0.##") + "k";
		return v.ToString();
	}
}
