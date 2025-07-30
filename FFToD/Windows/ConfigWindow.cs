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

        Size = new Vector2(400, 250);
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
            var rollTimeout = configuration.RollTimeout;
            if (ImGui.SliderInt("Roll Timeout (seconds)", ref rollTimeout, 10, 30))
            {
                configuration.RollTimeout = rollTimeout;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Time to collect rolls before auto-processing results");
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
