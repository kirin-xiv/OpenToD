// ========================================
// Configuration.cs - REPLACE ENTIRE CONTENT
// ========================================
using Dalamud.Configuration;
using System;
using Dalamud.Game.Text;

namespace TruthOrDarePlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool IsConfigWindowMovable { get; set; } = true;

        public int MinimumRolls { get; set; } = 2;
        public int MinimumWaitTime { get; set; } = 5;
        public int RollTimeout { get; set; } = 15;

        public XivChatType AnnouncementChatType { get; set; } = XivChatType.Yell;
        public XivChatType ResultChatType { get; set; } = XivChatType.Yell;

        public string CustomAnnouncement { get; set; } = string.Empty;

        public string? LastWinner { get; set; } = null;

        public bool ShowDebugInfo { get; set; } = false;

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
