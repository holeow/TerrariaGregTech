// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.util.ReadableNumberConverter), Forge 1.20.1. AE2 is LGPL-3.0-only. See
// AE2's LICENSE.

#nullable enable
using System;
using System.Globalization;

namespace GregTechCEuTerraria.AppliedEnergistics.Util;

public static class ReadableNumberConverter
{
	private const int DivisionBase = 1000;
	private static readonly char[] EncodedPostfixes = "KMGTPE".ToCharArray();

	public static string Format(long number, int width)
	{
		if (number < 0)
			throw new ArgumentException("Non-negative numbers cannot be formatted by this method");

		var numberString = number.ToString(CultureInfo.InvariantCulture);
		int numberSize = numberString.Length;
		if (numberSize <= width)
			return numberString;

		long @base = number;
		double last = @base * 1000.0;
		int exponent = -1;
		string postFix = "";

		while (numberSize > width)
		{
			last = @base;
			@base /= DivisionBase;
			exponent++;
			numberSize = @base.ToString(CultureInfo.InvariantCulture).Length + 1;
			postFix = EncodedPostfixes[exponent].ToString();
		}

		string withPrecision = FormatFraction(last / DivisionBase, 1) + postFix;
		string withoutPrecision = @base.ToString(CultureInfo.InvariantCulture) + postFix;
		return withPrecision.Length <= width ? withPrecision : withoutPrecision;
	}

	public static string Format(double number, int width)
	{
		if (number < 0)
			throw new ArgumentException("Non-negative numbers cannot be formatted by this method");

		int integerDigits = (int)Math.Max(0, Math.Log10(number) + 1);
		int fractionalDigits = width - integerDigits - 1;
		double minFractional = Math.Pow(10, -fractionalDigits);
		double fractional = number - Math.Floor(number);

		if (fractional < 1e-9 || integerDigits > width - 1)
			return Format((long)number, width);

		if (fractional + 1e-9 < minFractional && integerDigits - 1 <= width)
			return "~" + Format((long)number, width - 1);

		return FormatFraction(number, fractionalDigits);
	}

	private static string FormatFraction(double value, int maxFractionDigits)
	{
		double factor = Math.Pow(10, maxFractionDigits);
		double truncated = Math.Truncate(value * factor) / factor;
		return truncated.ToString("#." + new string('#', maxFractionDigits), CultureInfo.CurrentCulture);
	}
}
