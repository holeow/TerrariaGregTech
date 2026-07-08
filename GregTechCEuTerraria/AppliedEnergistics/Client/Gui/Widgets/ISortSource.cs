#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;

namespace GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Widgets;

public interface ISortSource
{
	SortOrder GetSortBy();
	SortDir GetSortDir();
	ViewItems GetSortDisplay();
	TypeFilter GetTypeFilter();
}
