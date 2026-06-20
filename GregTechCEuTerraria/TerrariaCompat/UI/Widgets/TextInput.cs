#nullable enable
using Microsoft.Xna.Framework.Input;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

internal interface ITextInput
{
	void CommitInput();
}

internal static class TextInputFocus
{
	private static ITextInput? _current;
	internal static ITextInput? Current => _current;

	internal static void Set(ITextInput input)
	{
		if (_current == input) return;
		ITextInput? prev = _current;
		_current = input;
		prev?.CommitInput();
	}

	internal static void Clear(ITextInput input)
	{
		if (_current == input) _current = null;
	}

	internal static void UnfocusAll()
	{
		ITextInput? c = _current;
		_current = null;
		c?.CommitInput();
	}
}

internal static class TextInputUtil
{
	internal static char? CharFor(Keys k, bool shift, bool forceUpper)
	{
		if (k >= Keys.A && k <= Keys.Z)
		{
			char c = (char)('a' + (k - Keys.A));
			return (shift || forceUpper) ? char.ToUpperInvariant(c) : c;
		}
		if (k >= Keys.D0 && k <= Keys.D9)
		{
			if (!shift) return (char)('0' + (k - Keys.D0));
			return k switch
			{
				Keys.D1 => '!', Keys.D2 => '@', Keys.D3 => '#', Keys.D4 => '$', Keys.D5 => '%',
				Keys.D6 => '^', Keys.D7 => '&', Keys.D8 => '*', Keys.D9 => '(', Keys.D0 => ')',
				_ => (char?)null,
			};
		}
		if (k >= Keys.NumPad0 && k <= Keys.NumPad9) return (char)('0' + (k - Keys.NumPad0));
		return k switch
		{
			Keys.Space        => ' ',
			Keys.Multiply     => '*',
			Keys.OemSemicolon => shift ? ':' : ';',
			Keys.OemQuestion  => shift ? '?' : '/',
			Keys.OemPipe      => shift ? '|' : '\\',
			Keys.OemMinus     => shift ? '_' : '-',
			Keys.OemPlus      => shift ? '+' : '=',
			Keys.OemPeriod    => shift ? '>' : '.',
			Keys.OemComma     => shift ? '<' : ',',
			Keys.OemQuotes    => shift ? '"' : '\'',
			Keys.OemOpenBrackets  => shift ? '{' : '[',
			Keys.OemCloseBrackets => shift ? '}' : ']',
			_                 => (char?)null,
		};
	}
}
