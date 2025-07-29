using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace FFToD;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Configuration configuration)
        : base("Truth or Dare Configuration##ConfigWindow", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.configuration = configuration;

        Size = new Vector2(400, 450);
        SizeCondition = ImGuiCond.Always;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        // Game Settings
        if (ImGui.CollapsingHeader("Game Settings", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var minRolls = configuration.MinimumRolls;
            if (ImGui.SliderInt("Minimum Rolls", ref minRolls, 1, 10))
            {
                configuration.MinimumRolls = minRolls;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Minimum number of rolls required to process results");

            var waitTime = configuration.WaitTime;
            if (ImGui.SliderInt("Wait Time (seconds)", ref waitTime, 1, 30))
            {
                configuration.WaitTime = waitTime;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Time to wait before auto-processing results");

            var rollTimeout = configuration.RollTimeout;
            if (ImGui.SliderInt("Roll Timeout (seconds)", ref rollTimeout, 5, 60))
            {
                configuration.RollTimeout = rollTimeout;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Time to wait for rolls before closing");

            // Chat type dropdown
            var currentChatType = (int)configuration.ChatType;
            var chatTypes = Enum.GetNames(typeof(ChatType));
            if (ImGui.Combo("Chat Type", ref currentChatType, chatTypes, chatTypes.Length))
            {
                configuration.ChatType = (ChatType)currentChatType;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Chat channel to use for announcements");
        }

        ImGui.Separator();

        // Announcements
        if (ImGui.CollapsingHeader("Announcements", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var customAnnouncement = configuration.CustomAnnouncement ?? "";
            if (ImGui.InputText("Custom Announcement", ref customAnnouncement, 200))
            {
                configuration.CustomAnnouncement = customAnnouncement;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Optional custom message to include in game start announcements");
        }

        ImGui.Separator();

        // Player Settings
        if (ImGui.CollapsingHeader("Player Settings", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var localPlayerName = configuration.LocalPlayerName ?? "";
            if (ImGui.InputText("Your Character Name", ref localPlayerName, 50))
            {
                configuration.LocalPlayerName = localPlayerName;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Your character name (used to replace 'You' in roll messages)");

            ImGui.Text("Last Winner:");
            ImGui.SameLine();
            if (!string.IsNullOrEmpty(configuration.LastWinner))
            {
                ImGui.TextColored(new Vector4(0, 1, 0, 1), configuration.LastWinner);
                ImGui.SameLine();
                if (ImGui.SmallButton("Clear"))
                {
                    configuration.LastWinner = "";
                    configuration.Save();
                }
            }
            else
            {
                ImGui.TextDisabled("None");
            }
        }

        ImGui.Separator();

        // Debug Settings
        if (ImGui.CollapsingHeader("Debug Settings"))
        {
            var enableDebug = configuration.EnableDebugLogging;
            if (ImGui.Checkbox("Enable Debug Logging", ref enableDebug))
            {
                configuration.EnableDebugLogging = enableDebug;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Enable verbose logging to the Dalamud log");

            var logAllChat = configuration.LogAllChatTypes;
            if (ImGui.Checkbox("Log All Chat Types", ref logAllChat))
            {
                configuration.LogAllChatTypes = logAllChat;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Log all chat messages with 'Random!' to help debug roll detection");
        }

        ImGui.Separator();

        // Save button
        if (ImGui.Button("Save & Close", new Vector2(120, 30)))
        {
            configuration.Save();
            IsOpen = false;
        }

        ImGui.SameLine();

        if (ImGui.Button("Close", new Vector2(120, 30)))
        {
            IsOpen = false;
        }
    }
}
