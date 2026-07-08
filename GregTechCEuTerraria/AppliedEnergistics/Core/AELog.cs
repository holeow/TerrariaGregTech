// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.core.AELog), Forge 1.20.1. AE2 is LGPL-3.0-only. See AE2's LICENSE.

#nullable enable
using System;
using System.Text;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.AppliedEnergistics.Core;

public static class AELog
{
	private const string DefaultExceptionMessage = "Exception: ";
	private static readonly object?[] EmptyParams = Array.Empty<object?>();

	private static log4net.ILog? _logger;
	private static log4net.ILog Logger => _logger ??= ModContent.GetInstance<GregTechCEuTerraria>().Logger;

	private static bool _craftingLogEnabled;
	private static bool _debugLogEnabled;
	private static bool _gridLogEnabled;

	private enum Level { Info, Warn, Error, Debug }

	public static bool IsLogEnabled() => true;

	public static bool IsDebugLogEnabled() => _debugLogEnabled;

	public static void SetDebugLogEnabled(bool newValue) => _debugLogEnabled = newValue;

	private static void Log(Level level, string message, object?[] @params)
	{
		if (IsLogEnabled())
			Emit(level, Format(message, @params), null);
	}

	private static void Log(Level level, Exception exception, string message, object?[] @params)
	{
		if (IsLogEnabled())
			Emit(level, Format(message, @params), exception);
	}

	private static void Emit(Level level, string message, Exception? exception)
	{
		var log = Logger;
		switch (level)
		{
			case Level.Info: if (exception != null) log.Info(message, exception); else log.Info(message); break;
			case Level.Warn: if (exception != null) log.Warn(message, exception); else log.Warn(message); break;
			case Level.Error: if (exception != null) log.Error(message, exception); else log.Error(message); break;
			case Level.Debug: if (exception != null) log.Debug(message, exception); else log.Debug(message); break;
		}
	}

	public static void Info(string format, params object?[] @params) => Log(Level.Info, format, @params);

	public static void Info(Exception exception) => Log(Level.Info, exception, DefaultExceptionMessage, EmptyParams);

	public static void Info(Exception exception, string message) => Log(Level.Info, exception, message, EmptyParams);

	public static void Warn(string format, params object?[] @params) => Log(Level.Warn, format, @params);

	public static void Warn(Exception exception) => Log(Level.Warn, exception, DefaultExceptionMessage, EmptyParams);

	public static void Warn(Exception exception, string message) => Log(Level.Warn, exception, message, EmptyParams);

	public static void Error(string format, params object?[] @params) => Log(Level.Error, format, @params);

	public static void Error(Exception exception) => Log(Level.Error, exception, DefaultExceptionMessage, EmptyParams);

	public static void Error(Exception exception, string message) => Log(Level.Error, exception, message, EmptyParams);

	public static void Debug(string format, params object?[] data)
	{
		if (IsDebugLogEnabled())
			Log(Level.Debug, format, data);
	}

	public static bool IsCraftingLogEnabled() => _craftingLogEnabled;

	public static void Crafting(string message, params object?[] @params)
	{
		if (IsCraftingLogEnabled())
			Log(Level.Info, message, @params);
	}

	public static bool IsCraftingDebugLogEnabled() => IsCraftingLogEnabled() && IsDebugLogEnabled();

	public static void CraftingDebug(string message, params object?[] @params)
	{
		if (IsCraftingDebugLogEnabled())
			Log(Level.Debug, message, @params);
	}

	public static bool IsGridLogEnabled() => _gridLogEnabled;

	public static void Grid(string message, params object?[] @params)
	{
		if (IsGridLogEnabled())
			Log(Level.Info, "[AE2 Grid Log] " + message, @params);
	}

	public static void SetCraftingLogEnabled(bool newValue) => _craftingLogEnabled = newValue;

	public static void SetGridLogEnabled(bool newValue) => _gridLogEnabled = newValue;

	private static string Format(string message, object?[] @params)
	{
		if (@params.Length == 0 || message.IndexOf('%') < 0)
			return message;

		var sb = new StringBuilder(message.Length + 16);
		int argIndex = 0;
		for (int i = 0; i < message.Length; i++)
		{
			char c = message[i];
			if (c == '%' && i + 1 < message.Length)
			{
				char n = message[i + 1];
				if (n == '%')
				{
					sb.Append('%');
					i++;
					continue;
				}
				if (IsConversion(n))
				{
					sb.Append(argIndex < @params.Length ? @params[argIndex]?.ToString() ?? "null" : "%" + n);
					argIndex++;
					i++;
					continue;
				}
			}
			sb.Append(c);
		}
		return sb.ToString();
	}

	private static bool IsConversion(char c) =>
		c is 's' or 'd' or 'f' or 'x' or 'X' or 'o' or 'b' or 'c' or 'e' or 'E' or 'g' or 'G' or 'n';
}
