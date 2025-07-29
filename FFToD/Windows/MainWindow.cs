using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

namespace FFToD;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;

    public MainWindow(Plugin plugin, Configuration configuration)
        : base("Truth or Dare##MainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        this.configuration = configuration;

        Size = new Vector2(450, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        // Status section
        ImGui.Text("Game Status:");
        ImGui.Indent();

        if (plugin.IsGameActive)
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), "Game is ACTIVE");
            if (plugin.IsRollingPhase)
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "Rolling phase is ACTIVE");
            else
                ImGui.Text("Rolling phase is CLOSED");
        }
        else
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "No game active");
        }

        var rolls = plugin.GetCurrentRolls();
        ImGui.Text($"Current rolls: {rolls.Count}");

        if (!string.IsNullOrEmpty(configuration.LastWinner))
            ImGui.Text($"Last winner: {configuration.LastWinner}");
        else
            ImGui.TextDisabled("Last winner: None");

        ImGui.Unindent();
        ImGui.Separator();

        // Control buttons
        ImGui.Text("Controls:");
        ImGui.Spacing();

        if (plugin.IsGameActive)
        {
            if (ImGui.Button("Stop Game", new Vector2(120, 30)))
            {
                plugin.StopGame();
            }
        }
        else
        {
            if (ImGui.Button("Start Game", new Vector2(120, 30)))
            {
                _ = plugin.StartGameAsync();
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("Clear Last Winner", new Vector2(150, 30)))
        {
            plugin.ClearLastWinner();
        }

        ImGui.SameLine();

        if (ImGui.Button("Configuration", new Vector2(120, 30)))
        {
            // Open configuration window
            plugin.OpenConfigWindow();
        }

        ImGui.Separator();

        // Rolls table
        if (rolls.Count > 0)
        {
            ImGui.Text("Current Rolls:");
            ImGui.Spacing();

            if (ImGui.BeginTable("RollsTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableHeadersRow();

                foreach (var (player, roll) in rolls.OrderByDescending(kvp => kvp.Value))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(player);
                    ImGui.TableNextColumn();

                    // Color code based on roll value
                    if (roll <= 100)
                        ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), roll.ToString());
                    else if (roll >= 900)
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), roll.ToString());
                    else
                        ImGui.Text(roll.ToString());
                }

                ImGui.EndTable();
            }

            // ✅ Winner & Stripper Summary
            var sorted = rolls.OrderByDescending(kvp => kvp.Value).ToList();

            // Determine winner
            string winnerDisplay = "None";
            if (sorted.Count > 0)
            {
                foreach (var roll in sorted)
                {
                    if (roll.Key != configuration.LastWinner)
                    {
                        winnerDisplay = $"{roll.Key} ({roll.Value})";
                        break;
                    }
                }

                if (winnerDisplay == "None")
                {
                    winnerDisplay = $"{sorted[0].Key} ({sorted[0].Value})"; // fallback
                }
            }

            // Determine strippers (roll <= 100)
            var strippers = rolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
            string stripListDisplay = strippers.Count > 0 ? string.Join(", ", strippers) : "None";

            ImGui.Separator();
            ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1), $"Winner: {winnerDisplay} | Strippers: {stripListDisplay}");
        }
        else
        {
            ImGui.TextDisabled("No rolls yet");
        }

        ImGui.Separator();

        // Commands help
        if (ImGui.CollapsingHeader("Commands"))
        {
            ImGui.Text("/tod - Open this window");
            ImGui.Text("/tod config - Open configuration");
            ImGui.Text("/todstart - Start a game");
            ImGui.Text("/todstop - Stop current game");
            ImGui.Text("/todstatus - Print status to chat");
        }
    }
}
