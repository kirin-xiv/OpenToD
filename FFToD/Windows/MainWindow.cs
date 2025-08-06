using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;

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

        var gameRolls = plugin.GetCurrentRolls();
        ImGui.Text($"Current rolls: {gameRolls.Count}");

        if (!string.IsNullOrEmpty(configuration.LastWinner))
            ImGui.Text($"Last winner: {configuration.LastWinner}");
        else
            ImGui.TextDisabled("Last winner: None");

        // Debug mode indicator
        if (configuration.DebugMode)
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "DEBUG MODE: Use /random 2");
        }

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
                plugin.StartGame();
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
            plugin.OpenConfigWindow();
        }

        ImGui.SameLine();

        // Pass button - always visible but disabled when not possible
        bool canPass = plugin.CanPass();
        if (!canPass) ImGui.BeginDisabled();
        if (ImGui.Button("Pass to Next Winner", new Vector2(150, 30)))
        {
            plugin.PassToNextWinner();
        }
        if (!canPass) ImGui.EndDisabled();
        if (ImGui.IsItemHovered())
        {
            if (canPass)
                ImGui.SetTooltip("Pass the win to the next highest roller");
            else
                ImGui.SetTooltip("Pass the win to the next highest roller (available after game ends)");
        }

        ImGui.Separator();

        // Rolls table
        if (gameRolls.Count > 0)
        {
            ImGui.Text("Current Rolls:");
            ImGui.Spacing();

            if (ImGui.BeginTable("RollsTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableHeadersRow();

                // Convert to list and sort manually to avoid lambda conflicts
                var rollsList = new List<KeyValuePair<string, int>>();
                foreach (var item in gameRolls)
                {
                    rollsList.Add(new KeyValuePair<string, int>(item.Key, item.Value));
                }
                rollsList.Sort((a, b) => b.Value.CompareTo(a.Value)); // Sort by value descending

                foreach (var rollItem in rollsList)
                {
                    var playerName = rollItem.Key;
                    var rollValue = rollItem.Value;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(playerName);
                    ImGui.TableNextColumn();

                    // Color code based on roll value
                    if (rollValue <= 100)
                        ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), rollValue.ToString());
                    else if (rollValue >= 900)
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), rollValue.ToString());
                    else
                        ImGui.Text(rollValue.ToString());
                }

                ImGui.EndTable();
            }

            // Generate winner and stripper info for display and copying
            string winnerDisplay = "None";
            string winnerForCopy = "";
            int winnerRollForCopy = 0;

            // If the round is complete, show the actual winner
            var currentRoundWinner = plugin.GetCurrentRoundWinner();
            if (!string.IsNullOrEmpty(currentRoundWinner))
            {
                // Find the roll value for the current winner
                if (gameRolls.TryGetValue(currentRoundWinner, out int winnerRoll))
                {
                    winnerDisplay = $"{currentRoundWinner} ({winnerRoll})";
                    winnerForCopy = currentRoundWinner;
                    winnerRollForCopy = winnerRoll;
                }
                else
                {
                    winnerDisplay = currentRoundWinner;
                    winnerForCopy = currentRoundWinner;
                }
            }
            else if (plugin.IsRollingPhase && gameRolls.Count > 0)
            {
                // During rolling phase, show tentative winner (applying last winner exclusion)
                var winnerList = new List<KeyValuePair<string, int>>();
                foreach (var item in gameRolls)
                {
                    winnerList.Add(new KeyValuePair<string, int>(item.Key, item.Value));
                }
                winnerList.Sort((a, b) => b.Value.CompareTo(a.Value)); // Sort by value descending

                foreach (var candidate in winnerList)
                {
                    if (candidate.Key != configuration.LastWinner)
                    {
                        winnerDisplay = $"{candidate.Key} ({candidate.Value}) [Tentative]";
                        winnerForCopy = candidate.Key;
                        winnerRollForCopy = candidate.Value;
                        break;
                    }
                }

                if (winnerDisplay == "None" && winnerList.Count > 0)
                {
                    winnerDisplay = $"{winnerList[0].Key} ({winnerList[0].Value}) [Tentative]"; // fallback
                    winnerForCopy = winnerList[0].Key;
                    winnerRollForCopy = winnerList[0].Value;
                }
            }

            // Determine strippers (roll <= 100)
            var stripperList = new List<string>();
            foreach (var rollData in gameRolls)
            {
                if (rollData.Value <= 100)
                {
                    stripperList.Add(rollData.Key);
                }
            }
            string stripListDisplay = stripperList.Count > 0 ? string.Join(", ", stripperList) : "None";

            ImGui.Separator();
            ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1), $"Winner: {winnerDisplay} | Strippers: {stripListDisplay}");

            // Add copy button when we have a winner (completed or tentative)
            if (!string.IsNullOrEmpty(winnerForCopy))
            {
                ImGui.Spacing();
                if (ImGui.Button("Copy Results", new Vector2(120, 25)))
                {
                    // Generate the copy text using the same format as the chat output
                    var copyText = $"/yell Winner: {winnerForCopy} ({winnerRollForCopy}) | Strippers: {stripListDisplay}";
                    ImGui.SetClipboardText(copyText);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Copy the results to clipboard for pasting in chat");
            }
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
