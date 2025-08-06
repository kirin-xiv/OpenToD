using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace FFToD;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Configuration configuration)
        : base("Truth or Dare Configuration##ConfigWindow", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.configuration = configuration;

        Size = new Vector2(400, 320);
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

        // Debug Settings
        if (ImGui.CollapsingHeader("Debug Settings"))
        {
            var debugMode = configuration.DebugMode;
            if (ImGui.Checkbox("Enable Debug Mode", ref debugMode))
            {
                configuration.DebugMode = debugMode;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Enables detection of /random <number> rolls for easier tiebreaker testing.\nExample: 'Random! You roll a 2 (out of 2).' instead of 'Random! You roll a 123.'");

            if (configuration.DebugMode)
            {
                ImGui.Indent();
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "Debug mode active!");
                ImGui.Text("Use '/random 2' to test tiebreakers easily.");
                ImGui.Text("Pattern: 'Random! Player roll a X (out of Y).'");
                ImGui.Unindent();
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
