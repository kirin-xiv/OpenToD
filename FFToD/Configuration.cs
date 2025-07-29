using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace FFToD;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // Game settings
    public int MinimumRolls { get; set; } = 2;
    public int WaitTime { get; set; } = 5;
    public int RollTimeout { get; set; } = 10;
    public ChatType ChatType { get; set; } = ChatType.Yell;
    public string CustomAnnouncement { get; set; } = "";
    public string LocalPlayerName { get; set; } = "";
    public string LastWinner { get; set; } = "";

    // Debug settings
    public bool EnableDebugLogging { get; set; } = false;
    public bool LogAllChatTypes { get; set; } = false;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface?.SavePluginConfig(this);
    }
}

public enum ChatType
{
    Say,
    Yell,
    Shout,
    Party,
    Alliance,
    FreeCompany,
    Echo
}
