using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Linq;
using System.Numerics;

namespace FFToD;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Configuration configuration)
        : base("Truth or Dare Configuration##ConfigWindow", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.configuration = configuration;

        Size = new Vector2(600, 750);
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
                
            var numWinners = configuration.NumberOfWinners;
            if (ImGui.SliderInt("Number of Winners", ref numWinners, 1, 2))
            {
                configuration.NumberOfWinners = numWinners;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("How many winners to select each round (1-2)");
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

            ImGui.Text("Last Winners:");
            ImGui.SameLine();
            if (configuration.LastWinners != null && configuration.LastWinners.Count > 0)
            {
                ImGui.TextColored(new Vector4(0, 1, 0, 1), string.Join(", ", configuration.LastWinners));
                ImGui.SameLine();
                if (ImGui.SmallButton("Clear"))
                {
                    configuration.LastWinner = "";
                    configuration.LastWinners.Clear();
                    configuration.Save();
                }
            }
            else if (!string.IsNullOrEmpty(configuration.LastWinner))
            {
                // Fallback for backwards compatibility
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

        // Automation Settings
        if (ImGui.CollapsingHeader("Automation Settings", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var autoPostRules = configuration.AutoPostRules;
            if (ImGui.Checkbox("Auto-post rules when starting game", ref autoPostRules))
            {
                configuration.AutoPostRules = autoPostRules;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Automatically posts rules, countdown, and starts roll collection");

            var autoPostResults = configuration.AutoPostResults;
            if (ImGui.Checkbox("Auto-post results when rolls close", ref autoPostResults))
            {
                configuration.AutoPostResults = autoPostResults;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Automatically posts closing countdown and results to chat");

            var customWiFiMessage = configuration.CustomWiFiMessage ?? "";
            if (ImGui.InputText("Custom Wi-Fi/Discord Message", ref customWiFiMessage, 200))
            {
                configuration.CustomWiFiMessage = customWiFiMessage;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Customize the Wi-Fi/Discord line posted with rules\nExample: 'Discord: https://discord.gg/yourserver' or 'Linkshell: YourLS'");
        }

        ImGui.Separator();

        // Chat Channel Settings
        if (ImGui.CollapsingHeader("Chat Channel Settings", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 1.0f, 1.0f), "Configure which chat channels to use for different messages:");
            ImGui.Spacing();

            // Rules Channel
            var rulesChannel = configuration.ChatChannels.RulesChannel;
            if (ImGui.BeginCombo("Rules Channel", rulesChannel.ToString()))
            {
                foreach (var channel in Enum.GetValues<ChatChannelType>())
                {
                    bool isSelected = rulesChannel == channel;
                    if (ImGui.Selectable(channel.ToString(), isSelected))
                    {
                        configuration.ChatChannels.RulesChannel = channel;
                        configuration.Save();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Channel for posting game rules and instructions");

            // Results Channel
            var resultsChannel = configuration.ChatChannels.ResultsChannel;
            if (ImGui.BeginCombo("Results Channel", resultsChannel.ToString()))
            {
                foreach (var channel in Enum.GetValues<ChatChannelType>())
                {
                    bool isSelected = resultsChannel == channel;
                    if (ImGui.Selectable(channel.ToString(), isSelected))
                    {
                        configuration.ChatChannels.ResultsChannel = channel;
                        configuration.Save();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Channel for posting winners and results");

            // Status Channel
            var statusChannel = configuration.ChatChannels.StatusChannel;
            if (ImGui.BeginCombo("Status Channel", statusChannel.ToString()))
            {
                foreach (var channel in Enum.GetValues<ChatChannelType>())
                {
                    bool isSelected = statusChannel == channel;
                    if (ImGui.Selectable(channel.ToString(), isSelected))
                    {
                        configuration.ChatChannels.StatusChannel = channel;
                        configuration.Save();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Channel for posting status messages (e.g., 'Rolls are now closed')");

            // Countdown Channel
            var countdownChannel = configuration.ChatChannels.CountdownChannel;
            if (ImGui.BeginCombo("Countdown Channel", countdownChannel.ToString()))
            {
                foreach (var channel in Enum.GetValues<ChatChannelType>())
                {
                    bool isSelected = countdownChannel == channel;
                    if (ImGui.Selectable(channel.ToString(), isSelected))
                    {
                        configuration.ChatChannels.CountdownChannel = channel;
                        configuration.Save();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Channel for countdown messages (3... 2... 1... Go!)");

            ImGui.Spacing();
            ImGui.Separator();
            
            // Winner-specific channel settings
            var useWinnerChannels = configuration.ChatChannels.UseWinnerSpecificChannels;
            if (ImGui.Checkbox("Use Winner-Specific Channels", ref useWinnerChannels))
            {
                configuration.ChatChannels.UseWinnerSpecificChannels = useWinnerChannels;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("When enabled, each winner outputs to their own designated channel");
            
            if (useWinnerChannels)
            {
                ImGui.Indent();
                ImGui.TextColored(new Vector4(0.9f, 0.7f, 0.3f, 1.0f), "Winner Output Channels:");
                
                // Winner 1 Channel
                var winner1Channel = configuration.ChatChannels.Winner1Channel;
                if (ImGui.BeginCombo("Winner #1 Channel", winner1Channel.ToString()))
                {
                    foreach (var channel in Enum.GetValues<ChatChannelType>())
                    {
                        bool isSelected = winner1Channel == channel;
                        if (ImGui.Selectable(channel.ToString(), isSelected))
                        {
                            configuration.ChatChannels.Winner1Channel = channel;
                            configuration.Save();
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
                
                // Winner 2 Channel
                var winner2Channel = configuration.ChatChannels.Winner2Channel;
                if (ImGui.BeginCombo("Winner #2 Channel", winner2Channel.ToString()))
                {
                    foreach (var channel in Enum.GetValues<ChatChannelType>())
                    {
                        bool isSelected = winner2Channel == channel;
                        if (ImGui.Selectable(channel.ToString(), isSelected))
                        {
                            configuration.ChatChannels.Winner2Channel = channel;
                            configuration.Save();
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
                
                
                ImGui.Unindent();
            }
            
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.4f, 1.0f), "Active Channel Commands:");
            ImGui.Text($"Rules: {configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.RulesChannel)}");
            if (useWinnerChannels)
            {
                ImGui.Text($"Winner #1: {configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.Winner1Channel)}");
                if (configuration.NumberOfWinners >= 2)
                    ImGui.Text($"Winner #2: {configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.Winner2Channel)}");
            }
            else
            {
                ImGui.Text($"Results: {configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.ResultsChannel)}");
            }
            ImGui.Text($"Status: {configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.StatusChannel)}");
            ImGui.Text($"Countdown: {configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.CountdownChannel)}");
        }

        ImGui.Separator();

        // Jackpot Settings
        if (ImGui.CollapsingHeader("Jackpot Settings", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var enableJackpot = configuration.EnableJackpot;
            if (ImGui.Checkbox("Enable Jackpot System", ref enableJackpot))
            {
                configuration.EnableJackpot = enableJackpot;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("When enabled, rolling the jackpot number triggers a special win that supersedes normal winners");

            if (enableJackpot)
            {
                var jackpotValue = configuration.JackpotValue;
                if (ImGui.InputInt("Jackpot Number", ref jackpotValue, 1, 10))
                {
                    // Allow any non-negative value
                    jackpotValue = Math.Max(0, jackpotValue);
                    configuration.JackpotValue = jackpotValue;
                    configuration.Save();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("The exact roll number that triggers a jackpot win (0 or higher)");
                    
                var jackpotChannel = configuration.ChatChannels.JackpotChannel;
                if (ImGui.BeginCombo("Jackpot Channel", jackpotChannel.ToString()))
                {
                    foreach (var channel in Enum.GetValues<ChatChannelType>())
                    {
                        bool isSelected = jackpotChannel == channel;
                        if (ImGui.Selectable(channel.ToString(), isSelected))
                        {
                            configuration.ChatChannels.JackpotChannel = channel;
                            configuration.Save();
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Channel where jackpot wins are announced");
            }
        }

        ImGui.Separator();

        // Roll Detection Settings
        if (ImGui.CollapsingHeader("Roll Detection Settings", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var enableRandom = configuration.EnableRandomDetection;
            if (ImGui.Checkbox("Detect /random rolls", ref enableRandom))
            {
                configuration.EnableRandomDetection = enableRandom;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Enable detection of /random commands (e.g., 'Random! Player rolls a 123.')");

            var enableDice = configuration.EnableDiceDetection;
            if (ImGui.Checkbox("Detect /dice rolls", ref enableDice))
            {
                configuration.EnableDiceDetection = enableDice;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Enable detection of /dice commands (e.g., '(Player Name) Random! 923')");

            if (!enableRandom && !enableDice)
            {
                ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "Warning: At least one roll detection method must be enabled!");
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
