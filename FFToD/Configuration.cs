using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace FFToD;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // Game settings
    public int RollTimeout { get; set; } = 17; // Matches your macro timing
    public string LocalPlayerName { get; set; } = "";
    public string LastWinner { get; set; } = "";
    
    // Debug settings
    public bool DebugMode { get; set; } = false; // Enable debug roll pattern for testing

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
