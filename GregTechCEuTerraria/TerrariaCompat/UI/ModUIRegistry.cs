#nullable enable
using System;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class ModUIRegistry
{
	private static Action? _activeCloser;

	public static void OnOpen(Action closeSelf)
	{
		if (ReferenceEquals(_activeCloser, closeSelf)) return;

		if (_activeCloser is { } prev)
		{
			_activeCloser = null;
			prev();
		}
		_activeCloser = closeSelf;
	}

	public static void OnClose(Action closeSelf)
	{
		if (ReferenceEquals(_activeCloser, closeSelf)) _activeCloser = null;
	}
}
