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
