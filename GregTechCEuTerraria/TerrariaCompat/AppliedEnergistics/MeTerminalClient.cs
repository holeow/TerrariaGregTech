#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Me.Common;
using Terraria.DataStructures;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;

public static class MeTerminalClient
{
	private static Point16 _pos;
	private static Repo? _repo;

	public static Repo? ActiveRepo => _repo;

	public static void Bind(Point16 pos, Repo repo)
	{
		_pos = pos;
		_repo = repo;
	}

	public static void Unbind(Repo repo)
	{
		if (ReferenceEquals(_repo, repo))
			_repo = null;
	}

	public static void Unbind(Point16 pos)
	{
		if (_repo != null && _pos == pos)
		{
			_repo = null;
			PinnedKeys.MarkCraftingPrunable(Crafting.PendingCraftingJobs.HasPendingJob);
		}
	}

	public static Repo? RepoFor(Point16 pos) =>
		_repo != null && _pos == pos ? _repo : null;
}
