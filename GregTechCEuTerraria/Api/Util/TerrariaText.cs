#nullable enable
using System.Text.RegularExpressions;

namespace GregTechCEuTerraria.Api.Util;

public static class TerrariaText
{
	private static readonly Regex ColorTag =
		new(@"\[c/[0-9A-Fa-f]+:(.*?)\]", RegexOptions.Compiled);

	public static string StripColorTags(string s) => ColorTag.Replace(s, "$1");

	public static string ItemName(int itemType) =>
		StripColorTags(Terraria.Lang.GetItemName(itemType).Value);
}
