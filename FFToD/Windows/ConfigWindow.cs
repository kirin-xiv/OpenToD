// ========================================
// Windows/ConfigWindow.cs - REPLACE ENTIRE CONTENT
// ========================================
using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Dalamud.Game.Text;

namespace TruthOrDarePlugin.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration _configuration;
        private readonly Plugin _plugin;

        public ConfigWindow(Plugin plugin) : base("Truth or Dare Configuration###TruthOrDareConfig")
        {
            Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse;

            Size = new Vector2(500, 600);
            SizeCondition = ImGuiCond.Always;

            _plugin = plugin;
            _configuration = plugin.Configuration;
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            ImGui.Text("Game Settings");
            ImGui.Separator();

            var minRolls = _configuration.MinimumRolls;
            if (ImGui.SliderInt("Minimum Rolls", ref minRolls, 1, 8))
            {
                _configuration.MinimumRolls = minRolls;
                _configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Minimum number of rolls before auto-processing");

            var minWait = _configuration.MinimumWaitTime;
            if (ImGui.SliderInt("Minimum Wait Time (seconds)", ref minWait, 1, 30))
            {
                _configuration.MinimumWaitTime = minWait;
                _configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Minimum time to wait before auto-processing rolls");

            var timeout = _configuration.RollTimeout;
            if (ImGui.SliderInt("Roll Timeout (seconds)", ref timeout, 10, 60))
            {
                _configuration.RollTimeout = timeout;
                _configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Maximum time to wait for rolls before processing");

            ImGui.Spacing();

            ImGui.Text("Chat Settings");
            ImGui.Separator();

            var chatTypes = new[] { "Say", "Yell", "Party", "Echo" };

            var currentAnnouncement = GetChatTypeIndex(_configuration.AnnouncementChatType);
            if (ImGui.Combo("Announcement Chat Type", ref currentAnnouncement, chatTypes, chatTypes.Length))
            {
                _configuration.AnnouncementChatType = GetChatTypeFromIndex(currentAnnouncement);
                _configuration.Save();
            }

            var currentResult = GetChatTypeIndex(_configuration.ResultChatType);
            if (ImGui.Combo("Result Chat Type", ref currentResult, chatTypes, chatTypes.Length))
            {
                _configuration.ResultChatType = GetChatTypeFromIndex(currentResult);
                _configuration.Save();
            }

            ImGui.Spacing();

            ImGui.Text("Custom Messages");
            ImGui.Separator();

            var customAnnouncement = _configuration.CustomAnnouncement;
            if (ImGui.InputTextMultiline("Custom Announcement", ref customAnnouncement, 500, new Vector2(-1, 100)))
            {
                _configuration.CustomAnnouncement = customAnnouncement;
                _configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Leave empty to use default announcement");

            ImGui.Spacing();

            ImGui.Text("Debug Settings");
            ImGui.Separator();

            var showDebug = _configuration.ShowDebugInfo;
            if (ImGui.Checkbox("Show Debug Info", ref showDebug))
            {
                _configuration.ShowDebugInfo = showDebug;
                _configuration.Save();
            }

            ImGui.Spacing();

            ImGui.Text("Current State");
            ImGui.Separator();
            ImGui.Text($"Last Winner: {_configuration.LastWinner ?? "None"}");

            if (ImGui.Button("Clear Last Winner"))
            {
                _plugin.ClearLastWinner();
            }
        }

        private static int GetChatTypeIndex(XivChatType chatType)
        {
            return chatType switch
            {
                XivChatType.Say => 0,
                XivChatType.Yell => 1,
                XivChatType.Party => 2,
                _ => 3
            };
        }

        private static XivChatType GetChatTypeFromIndex(int index)
        {
            return index switch
            {
                0 => XivChatType.Say,
                1 => XivChatType.Yell,
                2 => XivChatType.Party,
                _ => XivChatType.Echo
            };
        }
    }
}
