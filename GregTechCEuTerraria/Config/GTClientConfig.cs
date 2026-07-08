#nullable enable
using System.ComponentModel;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace GregTechCEuTerraria.Config;

public sealed class GTClientConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;

	[DefaultValue(true)]
	public bool ShowDiscordInvite { get; set; } = true;

	[DefaultValue(false)]
	public bool QuestbookEditMode { get; set; } = false;

	[DefaultValue(true)]
	public bool PinAutoCraftedItems { get; set; } = true;

	[DefaultValue(true)]
	public bool OpenDockedBrowserOnLaunch { get; set; } = true;

	[DefaultValue(true)]
	public bool ShowFavoritesPanel { get; set; } = true;

	[DefaultValue(true)]
	public bool ShowHistoryPanel { get; set; } = true;

	[DefaultValue(false)]
	public bool DebugScreenLayout { get; set; } = false;

	public static GTClientConfig Instance => ModContent.GetInstance<GTClientConfig>();

	public void DismissDiscordInvite()
	{
		ShowDiscordInvite = false;
		Persist();
	}

	public void Persist()
	{
		typeof(ConfigManager)
			.GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic)
			?.Invoke(null, new object[] { this });
	}
}
