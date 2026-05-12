using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Linq;
using System.Numerics;
using System.IO;
using Dalamud.Interface.Utility;
using Dalamud.Interface;

namespace FFToD;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private string newProfileName = "";
    private string selectedProfile = "";
    private bool showExportDialog = false;
    private bool showImportDialog = false;
    private string exportPath = "";
    private string importPath = "";
    private bool exportAllProfiles = false;
    private string lastImportError = "";

    public ConfigWindow(Configuration configuration)
        : base("Truth or Dare Configuration##ConfigWindow", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.configuration = configuration;

        Size = new Vector2(600, 850);
        SizeCondition = ImGuiCond.Always;
    }

    public void Dispose()
    {
    }

    private static class ModernStyle
    {
        public static Vector4 TextPrimary = new(1.0f, 1.0f, 1.0f, 1.0f);
        public static Vector4 TextSecondary = new(0.8f, 0.8f, 0.8f, 1.0f);
        public static Vector4 AccentPurple = new(0.8f, 0.6f, 1.0f, 1.0f);
        public static Vector4 SuccessGreen = new(0.4f, 1.0f, 0.4f, 1.0f);
        public static Vector4 WarningYellow = new(1.0f, 0.9f, 0.2f, 1.0f);
        public static Vector4 DangerRed = new(1.0f, 0.3f, 0.3f, 1.0f);
        public static Vector4 CardBackground = new(0.15f, 0.15f, 0.2f, 0.9f);

        public static void ApplyCardStyle()
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, CardBackground);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1.0f);
            ImGui.PushStyleColor(ImGuiCol.Border, AccentPurple);
        }

        public static void PopCardStyle()
        {
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
        }
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

        // Profile Management
        if (ImGui.CollapsingHeader("Profile Management", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawProfileManagementSection();
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

        DrawExportDialog();
        DrawImportDialog();
    }

    private void DrawProfileManagementSection()
    {
        ModernStyle.ApplyCardStyle();
        var cardSize = new Vector2(ImGui.GetContentRegionAvail().X, 200);
        if (ImGui.BeginChild("ProfileCard", cardSize, true))
        {
            ImGui.Spacing();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.User.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.TextPrimary, "Profile Management");
            ImGui.Spacing();

            ImGui.Text("Current Profile:");
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.SuccessGreen, string.IsNullOrEmpty(configuration.CurrentProfileName) ? "Default" : configuration.CurrentProfileName);
            ImGui.Spacing();

            var availableProfiles = configuration.GetProfileNames();
            
            // Profile Selection
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("##ProfileSelect", string.IsNullOrEmpty(selectedProfile) ? "Select Profile..." : selectedProfile))
            {
                foreach (var profile in availableProfiles)
                {
                    bool isSelected = selectedProfile == profile;
                    if (ImGui.Selectable(profile, isSelected))
                    {
                        selectedProfile = profile;
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.Button("Load", new Vector2(60, 0)) && !string.IsNullOrEmpty(selectedProfile))
            {
                if (configuration.LoadProfile(selectedProfile))
                {
                    selectedProfile = "";
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Delete", new Vector2(60, 0)) && !string.IsNullOrEmpty(selectedProfile))
            {
                if (configuration.DeleteProfile(selectedProfile))
                {
                    selectedProfile = "";
                }
            }

            // Save New Profile
            ImGui.SetNextItemWidth(200);
            ImGui.InputTextWithHint("##NewProfileName", "Enter profile name...", ref newProfileName, 50);
            
            ImGui.Spacing();
            
            bool hasProfileName = !string.IsNullOrWhiteSpace(newProfileName);
            
            // Save Current Settings button
            if (!hasProfileName)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.5f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.5f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.3f, 0.3f, 0.3f, 0.5f));
            }
            
            if (ImGui.Button("Save Current Settings", new Vector2(140, 0)) && hasProfileName)
            {
                if (configuration.SaveCurrentAsProfile(newProfileName.Trim()))
                {
                    newProfileName = "";
                }
            }
            
            if (!hasProfileName)
            {
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("A profile name is required to save");
            }
            
            ImGui.SameLine();
            
            // Save from Clipboard button
            if (!hasProfileName)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.5f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.5f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.3f, 0.3f, 0.3f, 0.5f));
            }
            
            if (ImGui.Button("Save from Clipboard", new Vector2(130, 0)) && hasProfileName)
            {
                if (configuration.SaveFromClipboard(newProfileName.Trim()))
                {
                    newProfileName = "";
                }
            }
            
            if (!hasProfileName)
            {
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("A profile name is required to save");
            }

            // Export/Import Section
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            if (ImGui.Button("Export Profile", new Vector2(110, 0)))
            {
                exportAllProfiles = false;
                showExportDialog = true;
            }
            if (ImGui.IsItemHovered())
            {
                var hoverText = string.IsNullOrEmpty(selectedProfile) 
                    ? "Export current configuration settings" 
                    : $"Export selected profile '{selectedProfile}'";
                ImGui.SetTooltip(hoverText);
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Export All", new Vector2(85, 0)))
            {
                exportAllProfiles = true;
                showExportDialog = true;
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Import", new Vector2(65, 0)))
            {
                showImportDialog = true;
            }
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
    }

    private void DrawExportDialog()
    {
        if (!showExportDialog) return;

        ImGui.SetNextWindowSize(new Vector2(400, 200), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Export Profile", ref showExportDialog))
        {
            if (exportAllProfiles)
            {
                ImGui.Text("Export all profiles to file:");
            }
            else
            {
                var profileToExport = string.IsNullOrEmpty(selectedProfile) ? "Current Settings" : selectedProfile;
                ImGui.Text($"Export profile '{profileToExport}' to file:");
            }
            
            ImGui.Spacing();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##ExportPath", "Enter file path...", ref exportPath, 300);
            
            ImGui.Spacing();
            if (ImGui.Button("Browse", new Vector2(70, 0)))
            {
                // TODO: Add file dialog here if available
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Export", new Vector2(70, 0)) && !string.IsNullOrWhiteSpace(exportPath))
            {
                try
                {
                    if (exportAllProfiles)
                    {
                        configuration.ExportAllProfiles(exportPath);
                    }
                    else
                    {
                        var profileToExport = string.IsNullOrEmpty(selectedProfile) ? "Default" : selectedProfile;
                        configuration.ExportProfile(profileToExport, exportPath);
                    }
                    showExportDialog = false;
                    exportPath = "";
                }
                catch (Exception)
                {
                    // TODO: Show error message
                }
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(70, 0)))
            {
                showExportDialog = false;
                exportPath = "";
            }
        }
        ImGui.End();
    }

    private void DrawImportDialog()
    {
        if (!showImportDialog) return;

        ImGui.SetNextWindowSize(new Vector2(400, 200), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Import Profile", ref showImportDialog))
        {
            ImGui.Text("Import profile(s) from file:");
            ImGui.Spacing();
            
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##ImportPath", "Enter file path...", ref importPath, 300);
            
            ImGui.Spacing();
            if (ImGui.Button("Browse", new Vector2(70, 0)))
            {
                // TODO: Add file dialog here if available
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Import", new Vector2(70, 0)) && !string.IsNullOrWhiteSpace(importPath))
            {
                try
                {
                    if (File.Exists(importPath))
                    {
                        var content = File.ReadAllText(importPath);
                        
                        // Try to determine if it's a single profile or multiple profiles
                        bool success = false;
                        if (content.TrimStart().StartsWith("["))
                        {
                            // Array - multiple profiles
                            success = configuration.ImportAllProfiles(importPath);
                        }
                        else
                        {
                            // Object - single profile
                            success = configuration.ImportProfile(importPath);
                        }
                        
                        if (success)
                        {
                            showImportDialog = false;
                            importPath = "";
                            lastImportError = "";
                        }
                        else
                        {
                            lastImportError = "Failed to import profile. Check that the file contains valid profile data.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    lastImportError = $"Import failed: {ex.Message}";
                }
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(70, 0)))
            {
                showImportDialog = false;
                importPath = "";
                lastImportError = "";
            }
            
            // Show error message if import failed
            if (!string.IsNullOrEmpty(lastImportError))
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(1.0f, 0.3f, 0.3f, 1.0f), lastImportError);
            }
        }
        ImGui.End();
    }
}
