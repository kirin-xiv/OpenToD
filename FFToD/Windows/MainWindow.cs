using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using HexaImGui = Hexa.NET.ImGui.ImGui;

namespace FFToD;

public class MainWindow : Window, IDisposable
{
    #region UI Styling System
    public enum ThemeType
    {
        SabinePurple,
        OceanBlue,
        ForestGreen,
        FoxxiOrange,
        RoseGold,
        MidnightBlue,
        KayaNoir
    }
    
    public static string GetThemeDisplayName(ThemeType theme)
    {
        return theme switch
        {
            ThemeType.SabinePurple => "Sabine Purple",
            ThemeType.OceanBlue => "Ocean Blue",
            ThemeType.ForestGreen => "Forest Green",
            ThemeType.FoxxiOrange => "Foxxi Orange",
            ThemeType.RoseGold => "Rose Gold",
            ThemeType.MidnightBlue => "Midnight Blue",
            ThemeType.KayaNoir => "Kaya Noir",
            _ => theme.ToString()
        };
    }
    
    private static class ModernStyle
    {
        // Current theme colors (will be updated based on selected theme)
        public static Vector4 BackgroundPrimary { get; private set; } = new(0.12f, 0.12f, 0.15f, 1.0f);
        public static Vector4 BackgroundSecondary { get; private set; } = new(0.16f, 0.16f, 0.20f, 1.0f);
        public static Vector4 BackgroundCard { get; private set; } = new(0.20f, 0.20f, 0.25f, 0.95f);
        public static Vector4 AccentPurple { get; private set; } = new(0.6f, 0.4f, 0.8f, 1.0f);
        public static Vector4 AccentPurpleHover { get; private set; } = new(0.7f, 0.5f, 0.9f, 1.0f);
        public static Vector4 AccentPurpleActive { get; private set; } = new(0.5f, 0.3f, 0.7f, 1.0f);
        public static Vector4 SuccessGreen { get; private set; } = new(0.2f, 0.8f, 0.3f, 1.0f);
        public static Vector4 SuccessGreenHover { get; private set; } = new(0.3f, 0.9f, 0.4f, 1.0f);
        public static Vector4 DangerRed { get; private set; } = new(0.8f, 0.2f, 0.3f, 1.0f);
        public static Vector4 DangerRedHover { get; private set; } = new(0.9f, 0.3f, 0.4f, 1.0f);
        public static Vector4 WarningYellow { get; private set; }
        public static Vector4 WarningYellowHover { get; private set; } = new(1.0f, 0.8f, 0.2f, 1.0f);
        public static Vector4 TextPrimary { get; private set; } = new(0.95f, 0.95f, 0.98f, 1.0f);
        public static Vector4 TextSecondary { get; private set; } = new(0.7f, 0.7f, 0.8f, 1.0f);
        
        public static void ApplyTheme(ThemeType theme)
        {
            switch (theme)
            {
                case ThemeType.SabinePurple:
                    BackgroundPrimary = new(0.12f, 0.12f, 0.15f, 1.0f);
                    BackgroundSecondary = new(0.16f, 0.16f, 0.20f, 1.0f);
                    BackgroundCard = new(0.20f, 0.20f, 0.25f, 0.95f);
                    AccentPurple = new(0.6f, 0.4f, 0.8f, 1.0f);
                    AccentPurpleHover = new(0.7f, 0.5f, 0.9f, 1.0f);
                    AccentPurpleActive = new(0.5f, 0.3f, 0.7f, 1.0f);
                    break;
                case ThemeType.OceanBlue:
                    BackgroundPrimary = new(0.08f, 0.12f, 0.16f, 1.0f);
                    BackgroundSecondary = new(0.12f, 0.18f, 0.24f, 1.0f);
                    BackgroundCard = new(0.16f, 0.24f, 0.32f, 0.95f);
                    AccentPurple = new(0.2f, 0.6f, 0.9f, 1.0f);
                    AccentPurpleHover = new(0.3f, 0.7f, 1.0f, 1.0f);
                    AccentPurpleActive = new(0.1f, 0.5f, 0.8f, 1.0f);
                    break;
                case ThemeType.ForestGreen:
                    BackgroundPrimary = new(0.08f, 0.12f, 0.08f, 1.0f);
                    BackgroundSecondary = new(0.12f, 0.18f, 0.12f, 1.0f);
                    BackgroundCard = new(0.16f, 0.24f, 0.16f, 0.95f);
                    AccentPurple = new(0.3f, 0.8f, 0.4f, 1.0f);
                    AccentPurpleHover = new(0.4f, 0.9f, 0.5f, 1.0f);
                    AccentPurpleActive = new(0.2f, 0.7f, 0.3f, 1.0f);
                    break;
                case ThemeType.FoxxiOrange:
                    BackgroundPrimary = new(0.15f, 0.10f, 0.08f, 1.0f);
                    BackgroundSecondary = new(0.20f, 0.14f, 0.12f, 1.0f);
                    BackgroundCard = new(0.25f, 0.18f, 0.16f, 0.95f);
                    AccentPurple = new(1.0f, 0.6f, 0.2f, 1.0f);
                    AccentPurpleHover = new(1.0f, 0.7f, 0.3f, 1.0f);
                    AccentPurpleActive = new(0.9f, 0.5f, 0.1f, 1.0f);
                    break;
                case ThemeType.RoseGold:
                    BackgroundPrimary = new(0.14f, 0.10f, 0.12f, 1.0f);
                    BackgroundSecondary = new(0.18f, 0.14f, 0.16f, 1.0f);
                    BackgroundCard = new(0.22f, 0.18f, 0.20f, 0.95f);
                    AccentPurple = new(0.9f, 0.6f, 0.7f, 1.0f);
                    AccentPurpleHover = new(1.0f, 0.7f, 0.8f, 1.0f);
                    AccentPurpleActive = new(0.8f, 0.5f, 0.6f, 1.0f);
                    break;
                case ThemeType.MidnightBlue:
                    BackgroundPrimary = new(0.05f, 0.08f, 0.15f, 1.0f);
                    BackgroundSecondary = new(0.08f, 0.12f, 0.20f, 1.0f);
                    BackgroundCard = new(0.12f, 0.16f, 0.25f, 0.95f);
                    AccentPurple = new(0.4f, 0.6f, 1.0f, 1.0f);
                    AccentPurpleHover = new(0.5f, 0.7f, 1.0f, 1.0f);
                    AccentPurpleActive = new(0.3f, 0.5f, 0.9f, 1.0f);
                    break;
                case ThemeType.KayaNoir:
                    BackgroundPrimary = new(0.06f, 0.06f, 0.06f, 1.0f);
                    BackgroundSecondary = new(0.10f, 0.10f, 0.10f, 1.0f);
                    BackgroundCard = new(0.14f, 0.14f, 0.14f, 0.95f);
                    AccentPurple = new(0.55f, 0.55f, 0.55f, 1.0f);
                    AccentPurpleHover = new(0.65f, 0.65f, 0.65f, 1.0f);
                    AccentPurpleActive = new(0.45f, 0.45f, 0.45f, 1.0f);
                    break;
            }
            
            // Update other colors based on theme
            SuccessGreen = new(0.2f, 0.8f, 0.3f, 1.0f);
            SuccessGreenHover = new(0.3f, 0.9f, 0.4f, 1.0f);
            DangerRed = new(0.8f, 0.2f, 0.3f, 1.0f);
            DangerRedHover = new(0.9f, 0.3f, 0.4f, 1.0f);
            WarningYellow = new(1.0f, 0.8f, 0.2f, 1.0f);
            WarningYellowHover = new(1.0f, 0.9f, 0.3f, 1.0f);
            TextPrimary = new(0.95f, 0.95f, 0.98f, 1.0f);
            TextSecondary = new(0.7f, 0.7f, 0.8f, 1.0f);
        }

        public static void ApplyCardStyle()
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, BackgroundCard);
            ImGui.PushStyleColor(ImGuiCol.Border, AccentPurple);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 12.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 16));
        }

        public static void PopCardStyle()
        {
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(3);
        }

        public static void ApplyModernButtonStyle(Vector4 baseColor, Vector4 hoverColor, Vector4? activeColor = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, baseColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor ?? new Vector4(baseColor.X * 0.8f, baseColor.Y * 0.8f, baseColor.Z * 0.8f, baseColor.W));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(16, 8));
        }

        public static void PopModernButtonStyle()
        {
            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar(2);
        }

        public static void ApplyGlobalStyle()
        {
            var style = ImGui.GetStyle();
            style.WindowRounding = 16.0f;
            style.FrameRounding = 8.0f;
            style.PopupRounding = 8.0f;
            style.ScrollbarRounding = 8.0f;
            style.GrabRounding = 8.0f;
            style.TabRounding = 8.0f;
            style.WindowBorderSize = 1.0f;
            style.FrameBorderSize = 0.0f;

            // Modern spacing
            style.WindowPadding = new Vector2(12, 12);
            style.FramePadding = new Vector2(8, 6);
            style.ItemSpacing = new Vector2(8, 6);
            style.ItemInnerSpacing = new Vector2(6, 6);
        }
    }
    #endregion

    private readonly Plugin plugin;
    private readonly Configuration configuration;
    
    private bool showWinnerSelectionPopup = false;
    private string selectedWinnerToPass = null;
    
    private string newProfileName = "";
    private string selectedProfile = "";
    private bool showImportDialog = false;
    private string importProfileName = "";
    private string importData = "";
    private string importFeedback = "";
    private bool importSuccess = false;
    private int newBonusNumber = 0;

    public MainWindow(Plugin plugin, Configuration configuration)
        : base("Truth or Dare##MainWindow")
    {
        this.plugin = plugin;
        this.configuration = configuration;

        // Set window size properties - increased height for Settings tab
        this.Size = new Vector2(600, 800);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        // Apply saved theme
        if (configuration.SelectedTheme >= 0 && configuration.SelectedTheme < Enum.GetValues<ThemeType>().Length)
        {
            ModernStyle.ApplyTheme((ThemeType)configuration.SelectedTheme);
        }

        // Add title bar buttons for quick actions
        SetupTitleBarButtons();
    }

    private void SetupTitleBarButtons()
    {
        // Copy results button
        this.TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Copy,
            Click = (msg) =>
            {
                CopyCurrentResults();
            },
            IconOffset = new Vector2(2, 1),
            ShowTooltip = () =>
            {
                ImGui.SetTooltip("Copy Results to Clipboard");
            }
        });
    }

    private void CopyCurrentResults()
    {
        var gameRolls = plugin.GetCurrentRolls();
        if (gameRolls.Count == 0) return;

        // Generate winner and stripper info for copying
        string winnerForCopy = "";
        int winnerRollForCopy = 0;

        var currentRoundWinner = plugin.GetCurrentRoundWinner();
        if (!string.IsNullOrEmpty(currentRoundWinner))
        {
            winnerForCopy = currentRoundWinner;
            gameRolls.TryGetValue(currentRoundWinner, out winnerRollForCopy);
        }
        else if (plugin.IsRollingPhase && gameRolls.Count > 0)
        {
            // Show tentative winner during rolling phase
            var sortedRolls = gameRolls.OrderByDescending(kvp => kvp.Value).ToList();
            foreach (var candidate in sortedRolls)
            {
                if (candidate.Key != configuration.LastWinner)
                {
                    winnerForCopy = candidate.Key;
                    winnerRollForCopy = candidate.Value;
                    break;
                }
            }

            if (string.IsNullOrEmpty(winnerForCopy) && sortedRolls.Count > 0)
            {
                winnerForCopy = sortedRolls[0].Key;
                winnerRollForCopy = sortedRolls[0].Value;
            }
        }

        if (!string.IsNullOrEmpty(winnerForCopy))
        {
            // Generate stripper list
            var stripperList = gameRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
            string stripListDisplay = stripperList.Count > 0 ? string.Join(", ", stripperList) : "None";

            var copyText = $"/yell Winner: {winnerForCopy} ({winnerRollForCopy}) | Strippers: {stripListDisplay}";
            ImGui.SetClipboardText(copyText);
        }
    }


    public void Dispose() { }
    
    private void DrawSupportButtons()
    {
    }
    
    private Vector2 GetIconTextButtonSize(FontAwesomeIcon icon, string text)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var iconSize = ImGui.CalcTextSize(icon.ToIconString());
        ImGui.PopFont();
        var textSize = ImGui.CalcTextSize(text);
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        return new Vector2(iconSize.X + textSize.X + spacing + ImGui.GetStyle().FramePadding.X * 2, Math.Max(iconSize.Y, textSize.Y) + ImGui.GetStyle().FramePadding.Y * 2);
    }
    
    private bool IconTextButton(FontAwesomeIcon icon, string text)
    {
        var buttonSize = GetIconTextButtonSize(icon, text);
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var clicked = ImGui.Button($"##{text}", buttonSize);
        
        // Calculate icon dimensions with icon font
        ImGui.PushFont(UiBuilder.IconFont);
        var iconString = icon.ToIconString();
        var iconSize = ImGui.CalcTextSize(iconString);
        var iconPos = new Vector2(pos.X + ImGui.GetStyle().FramePadding.X, pos.Y + (buttonSize.Y - iconSize.Y) / 2);
        drawList.AddText(iconPos, ImGui.GetColorU32(ImGuiCol.Text), iconString);
        ImGui.PopFont();
        
        // Draw text
        var textPos = new Vector2(pos.X + ImGui.GetStyle().FramePadding.X + iconSize.X + ImGui.GetStyle().ItemSpacing.X, pos.Y + (buttonSize.Y - ImGui.CalcTextSize(text).Y) / 2);
        drawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);
        
        return clicked;
    }

    private void DrawWinnerSelectionPopup()
    {
        if (showWinnerSelectionPopup)
        {
            ImGui.OpenPopup("Select Winner to Pass");
        }
        
        // Center the popup
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        
        if (ImGui.BeginPopupModal("Select Winner to Pass", ref showWinnerSelectionPopup, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Select which winner is passing:");
            ImGui.Separator();
            
            var winners = plugin.GetCurrentRoundWinners();
            var rolls = plugin.GetCurrentRolls();
            
            foreach (var winner in winners)
            {
                int roll = rolls.TryGetValue(winner, out int r) ? r : 0;
                if (ImGui.Selectable($"{winner} ({roll})", selectedWinnerToPass == winner))
                {
                    selectedWinnerToPass = winner;
                }
            }
            
            ImGui.Separator();
            
            // Confirm button
            if (selectedWinnerToPass != null)
            {
                ModernStyle.ApplyModernButtonStyle(ModernStyle.SuccessGreen, ModernStyle.SuccessGreenHover);
                if (ImGui.Button("Confirm Pass", new Vector2(120, 30)))
                {
                    plugin.PassWinnerToNext(selectedWinnerToPass);
                    showWinnerSelectionPopup = false;
                    selectedWinnerToPass = null;
                }
                ModernStyle.PopModernButtonStyle();
                
                ImGui.SameLine();
            }
            
            // Cancel button
            if (ImGui.Button("Cancel", new Vector2(120, 30)))
            {
                showWinnerSelectionPopup = false;
                selectedWinnerToPass = null;
            }
            
            ImGui.EndPopup();
        }
    }
    
    public override void Draw()
    {
        // Apply custom styling for this window only
        var style = ImGui.GetStyle();
        var originalWindowRounding = style.WindowRounding;
        var originalFrameRounding = style.FrameRounding;
        var originalPopupRounding = style.PopupRounding;
        var originalScrollbarRounding = style.ScrollbarRounding;
        var originalGrabRounding = style.GrabRounding;
        var originalTabRounding = style.TabRounding;

        // Apply rounded corners temporarily
        style.WindowRounding = 16.0f;
        style.FrameRounding = 8.0f;
        style.PopupRounding = 8.0f;
        style.ScrollbarRounding = 8.0f;
        style.GrabRounding = 8.0f;
        style.TabRounding = 8.0f;

        try
        {

            var gameRolls = plugin.GetCurrentRolls();

        
        ImGui.Separator();

        // Tab bar  
        if (ImGui.BeginTabBar("MainTabs"))
        {
            if (ImGui.BeginTabItem("[Game]"))
            {
                DrawGameTab(gameRolls);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("[Settings]"))
            {
                DrawSettingsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("[About]"))
            {
                DrawAboutTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
        }
        finally
        {
            // Restore original ImGui styling
            style.WindowRounding = originalWindowRounding;
            style.FrameRounding = originalFrameRounding;
            style.PopupRounding = originalPopupRounding;
            style.ScrollbarRounding = originalScrollbarRounding;
            style.GrabRounding = originalGrabRounding;
            style.TabRounding = originalTabRounding;
        }
    }

    private void DrawGameTab(IReadOnlyDictionary<string, int> gameRolls)
    {
        // Game Status section with card styling
        ModernStyle.ApplyCardStyle();
        if (ImGui.BeginChild("GameStatusCard", new Vector2(0, 120), true, ImGuiWindowFlags.NoScrollbar))
        {
            // Section header
            ImGui.TextColored(ModernStyle.TextPrimary, "Game Status");
            ImGui.Separator();
            ImGui.Spacing();

            // Status display with color coding and icons
            ImGui.SetWindowFontScale(1.2f);
            if (plugin.IsGameActive)
            {
                if (plugin.IsRollingPhase)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ModernStyle.WarningYellow, FontAwesomeIcon.Dice.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine();
                    ImGui.TextColored(ModernStyle.WarningYellow, "ROLLING");
                    ImGui.SameLine();
                    ImGui.TextColored(ModernStyle.TextSecondary, " - Collecting rolls...");
                }
                else
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ModernStyle.SuccessGreen, FontAwesomeIcon.CheckCircle.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine();
                    ImGui.TextColored(ModernStyle.SuccessGreen, "ACTIVE");
                    ImGui.SameLine();
                    ImGui.TextColored(ModernStyle.TextSecondary, " - Ready for action!");
                }
            }
            else
            {
                ImGui.SetWindowFontScale(1.0f); // Use normal size for NO GAME
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ModernStyle.DangerRed, FontAwesomeIcon.TimesCircle.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColored(ModernStyle.DangerRed, "NO GAME");
                ImGui.SameLine();
                ImGui.TextColored(ModernStyle.TextSecondary, " - Click Start to begin");
            }
            ImGui.SetWindowFontScale(1.0f);

            ImGui.Spacing();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Users.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.TextPrimary, $"Players: {gameRolls.Count}");

            if (!string.IsNullOrEmpty(configuration.LastWinner))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Crown.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColored(ModernStyle.AccentPurple, $"Last Winner: {configuration.LastWinner}");
            }
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();

        ImGui.Spacing();
        ModernStyle.ApplyCardStyle();
        if (ImGui.BeginChild("StatisticsCard", new Vector2(0, 110), true, ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.ChartBar.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.TextPrimary, "Statistics");
            ImGui.Separator();
            ImGui.Spacing();

            var availableWidth = ImGui.GetContentRegionAvail().X;
            var labelWidth = availableWidth * 0.6f;
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Trophy.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.Text("All Time Rounds:");
            ImGui.SameLine(labelWidth);
            ImGui.TextColored(ModernStyle.SuccessGreen, configuration.Statistics.TotalRounds.ToString());
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Clock.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.Text("This Session:");
            ImGui.SameLine(labelWidth);
            ImGui.TextColored(ModernStyle.WarningYellow, configuration.Statistics.SessionRounds.ToString());
            ImGui.SameLine();
            ImGui.SetCursorPosX(availableWidth - 65 + ImGui.GetWindowContentRegionMin().X);
            
            bool canReset = configuration.Statistics.SessionRounds > 0;
            if (!canReset) ImGui.BeginDisabled();
            
            ModernStyle.ApplyModernButtonStyle(ModernStyle.DangerRed, ModernStyle.DangerRedHover);
            
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 7));
            
            if (ImGui.Button("Reset", new Vector2(60, 30)))
            {
                plugin.ResetSessionStatistics();
            }
            ImGui.PopStyleVar();
            ModernStyle.PopModernButtonStyle();
            
            if (!canReset) ImGui.EndDisabled();
            
            if (ImGui.IsItemHovered())
            {
                if (canReset)
                    ImGui.SetTooltip("Reset session round count to 0");
                else
                    ImGui.SetTooltip("No session rounds to reset");
            }
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();

        ImGui.Spacing();

        string winnerForCopy = "";
        int winnerRollForCopy = 0;
        bool hasResults = false;

        if (gameRolls.Count > 0)
        {
            var currentRoundWinner = plugin.GetCurrentRoundWinner();
            if (!string.IsNullOrEmpty(currentRoundWinner))
            {
                winnerForCopy = currentRoundWinner;
                gameRolls.TryGetValue(currentRoundWinner, out winnerRollForCopy);
                hasResults = true;
            }
            else if (plugin.IsRollingPhase && gameRolls.Count > 0)
            {
                // Show tentative winner during rolling phase
                var sortedRolls = gameRolls.OrderByDescending(kvp => kvp.Value).ToList();
                foreach (var candidate in sortedRolls)
                {
                    if (candidate.Key != configuration.LastWinner)
                    {
                        winnerForCopy = candidate.Key;
                        winnerRollForCopy = candidate.Value;
                        hasResults = true;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(winnerForCopy) && sortedRolls.Count > 0)
                {
                    winnerForCopy = sortedRolls[0].Key;
                    winnerRollForCopy = sortedRolls[0].Value;
                    hasResults = true;
                }
            }
        }

        // Copy Results button with styling
        if (!hasResults) ImGui.BeginDisabled();

        ModernStyle.ApplyModernButtonStyle(ModernStyle.AccentPurple, ModernStyle.AccentPurpleHover, ModernStyle.AccentPurpleActive);

        ImGui.PushFont(UiBuilder.IconFont);
        var iconSize = ImGui.CalcTextSize(FontAwesomeIcon.Copy.ToIconString());
        ImGui.PopFont();

        var buttonSize = new Vector2(200, 35);
        if (ImGui.Button($"   Copy Results", buttonSize))
        {
            if (hasResults && !string.IsNullOrEmpty(winnerForCopy))
            {
                var stripperList = gameRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
                string stripListDisplay = stripperList.Count > 0 ? string.Join(", ", stripperList) : "None";
                var copyText = $"/yell Winner: {winnerForCopy} ({winnerRollForCopy}) | Strippers: {stripListDisplay}";
                ImGui.SetClipboardText(copyText);
            }
        }

        // Draw icon on button
        var drawList = ImGui.GetWindowDrawList();
        var buttonMin = ImGui.GetItemRectMin();
        var iconPos = new Vector2(buttonMin.X + 12, buttonMin.Y + (35 - iconSize.Y) / 2);
        ImGui.PushFont(UiBuilder.IconFont);
        drawList.AddText(iconPos, ImGui.GetColorU32(ModernStyle.TextPrimary), FontAwesomeIcon.Copy.ToIconString());
        ImGui.PopFont();

        ModernStyle.PopModernButtonStyle();
        if (!hasResults) ImGui.EndDisabled();

        if (ImGui.IsItemHovered())
        {
            if (hasResults)
                ImGui.SetTooltip("Copy the results to clipboard for pasting in chat");
            else
                ImGui.SetTooltip("No results available to copy");
        }


        // Controls section with card styling
        ImGui.Spacing();
        ModernStyle.ApplyCardStyle();
        if (ImGui.BeginChild("ControlsCard", new Vector2(0, 140), true, ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.TextColored(ModernStyle.TextPrimary, "Controls");
            ImGui.Separator();
            ImGui.Spacing();

            // Calculate button dimensions to use full available width
            var availableWidth = ImGui.GetContentRegionAvail().X;
            var buttonSpacing = ImGui.GetStyle().ItemSpacing.X;
            var controlButtonSize = new Vector2((availableWidth - buttonSpacing) / 2, 35);
            var spacing = ImGui.GetStyle().ItemSpacing.X;

            // First row - Start/Stop Game and Clear Last Winner
            if (plugin.IsGameActive)
            {
                // Stop Game button with icon
                ModernStyle.ApplyModernButtonStyle(ModernStyle.DangerRed, ModernStyle.DangerRedHover);

                if (ImGui.Button($"   Stop Game", controlButtonSize))
                {
                    plugin.StopGame();
                }

                // Draw stop icon
                var stopDrawList = ImGui.GetWindowDrawList();
                var stopButtonMin = ImGui.GetItemRectMin();
                var stopIconPos = new Vector2(stopButtonMin.X + 12, stopButtonMin.Y + 8);
                ImGui.PushFont(UiBuilder.IconFont);
                stopDrawList.AddText(stopIconPos, ImGui.GetColorU32(ModernStyle.TextPrimary), FontAwesomeIcon.Stop.ToIconString());
                ImGui.PopFont();

                ModernStyle.PopModernButtonStyle();
            }
            else
            {
                // Start Game button with icon
                ModernStyle.ApplyModernButtonStyle(ModernStyle.SuccessGreen, ModernStyle.SuccessGreenHover);

                if (ImGui.Button($"   Start Game", controlButtonSize))
                {
                    plugin.StartGame();
                }

                // Draw play icon
                var startDrawList = ImGui.GetWindowDrawList();
                var startButtonMin = ImGui.GetItemRectMin();
                var startIconPos = new Vector2(startButtonMin.X + 12, startButtonMin.Y + 8);
                ImGui.PushFont(UiBuilder.IconFont);
                startDrawList.AddText(startIconPos, ImGui.GetColorU32(ModernStyle.TextPrimary), FontAwesomeIcon.Play.ToIconString());
                ImGui.PopFont();

                ModernStyle.PopModernButtonStyle();
            }

            ImGui.SameLine();

            // Clear Last Winner button
            ModernStyle.ApplyModernButtonStyle(ModernStyle.BackgroundSecondary, ModernStyle.AccentPurple);
            if (ImGui.Button($"   Clear Winner", controlButtonSize))
            {
                plugin.ClearLastWinner();
            }

            // Draw clear icon
            var clearDrawList = ImGui.GetWindowDrawList();
            var clearButtonMin = ImGui.GetItemRectMin();
            var clearIconPos = new Vector2(clearButtonMin.X + 12, clearButtonMin.Y + 8);
            ImGui.PushFont(UiBuilder.IconFont);
            clearDrawList.AddText(clearIconPos, ImGui.GetColorU32(ModernStyle.TextPrimary), FontAwesomeIcon.Eraser.ToIconString());
            ImGui.PopFont();
            ModernStyle.PopModernButtonStyle();

            ImGui.Spacing();

            // Second row - Pass to Next Winner (centered)
            ImGui.SetCursorPosX((availableWidth - controlButtonSize.X) / 2 + ImGui.GetWindowContentRegionMin().X);

            bool canPass = plugin.CanPass();
            if (!canPass) ImGui.BeginDisabled();

            ModernStyle.ApplyModernButtonStyle(ModernStyle.WarningYellow, new Vector4(1.0f, 0.9f, 0.3f, 1.0f));
            if (ImGui.Button($"   Pass Winner", controlButtonSize))
            {
                var winners = plugin.GetCurrentRoundWinners();
                if (winners.Count > 1)
                {
                    // Show selection popup for multiple winners
                    showWinnerSelectionPopup = true;
                    selectedWinnerToPass = null;
                }
                else if (winners.Count == 1)
                {
                    // Single winner, pass directly
                    plugin.PassWinnerToNext(winners[0]);
                }
            }

            // Draw pass icon
            var passDrawList = ImGui.GetWindowDrawList();
            var passButtonMin = ImGui.GetItemRectMin();
            var passIconPos = new Vector2(passButtonMin.X + 12, passButtonMin.Y + 8);
            ImGui.PushFont(UiBuilder.IconFont);
            var textColor = canPass ? ModernStyle.TextPrimary : ModernStyle.TextSecondary;
            passDrawList.AddText(passIconPos, ImGui.GetColorU32(textColor), FontAwesomeIcon.ArrowRight.ToIconString());
            ImGui.PopFont();
            ModernStyle.PopModernButtonStyle();

            if (!canPass) ImGui.EndDisabled();
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();

        // Always show the Roll Results section
        ShowCurrentResults(gameRolls);
        
        // Draw winner selection popup if needed
        DrawWinnerSelectionPopup();

        // Collapsible commands section - matches your screenshot
        if (ImGui.CollapsingHeader(">> Available Commands"))
        {
            ImGui.TextWrapped("Commands available:");
            ImGui.BulletText("/tod - Open this window");
            ImGui.BulletText("/tod config - Open configuration");
            ImGui.BulletText("/todstart - Start a game");
            ImGui.BulletText("/todstop - Stop current game");
            ImGui.BulletText("/todstatus - Print status to chat");
        }

        // Footer - matches your screenshot
        ImGui.Separator();
        var footerText = "Made with <3 by kirin-xiv";
        var footerWidth = ImGui.CalcTextSize(footerText).X;
        var windowWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
        ImGui.SetCursorPosX((windowWidth - footerWidth) / 2 + ImGui.GetWindowContentRegionMin().X);
        ImGui.TextColored(new Vector4(1.0f, 0.75f, 0.8f, 1f), footerText);
    }

    private void ShowCurrentResults(IReadOnlyDictionary<string, int> gameRolls)
    {
        // Sort rolls by value descending for display
        var sortedRolls = gameRolls.OrderByDescending(kvp => kvp.Value).ToList();

        // Results section with card styling
        ImGui.Spacing();
        ModernStyle.ApplyCardStyle();

        // Use remaining available height for roll results, leaving space for commands/footer
        float availableHeight = ImGui.GetContentRegionAvail().Y - 120; // Leave room for commands and footer

        if (ImGui.BeginChild("ResultsCard", new Vector2(0, availableHeight), true, ImGuiWindowFlags.NoScrollbar))
        {
            // Header with icon
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.List.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.TextPrimary, "Roll Results");
            ImGui.Separator();
            ImGui.Spacing();

            if (gameRolls.Count == 0)
            {
                // No rolls yet - show placeholder message
                ImGui.TextColored(ModernStyle.TextSecondary, "No rolls yet... waiting for players to /random");
            }
            else
            {
                // Table display with scrolling for long player lists
                if (ImGui.BeginTable("RollsTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
                {
                    // Setup columns with better sizing
                    ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 80);

                    // Table headers
                    ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ModernStyle.AccentPurple, "Player");
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ModernStyle.AccentPurple, "Roll");

                    foreach (var roll in sortedRolls)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    // Show bonus prize icon if player hit a bonus number
                    bool isBonusHit = configuration.EnableBonusPrizes && configuration.BonusPrizes.Any(bp => bp.Number == roll.Value);

                    // Color code players based on game status
                    var currentWinner = plugin.GetCurrentRoundWinner();
                    if (!string.IsNullOrEmpty(currentWinner) && roll.Key == currentWinner)
                    {
                        // Winner in gold
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ModernStyle.WarningYellow, FontAwesomeIcon.Crown.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                        ImGui.TextColored(ModernStyle.WarningYellow, roll.Key);
                    }
                    else if (roll.Key == configuration.LastWinner)
                    {
                        // Excluded player in muted color
                        ImGui.TextColored(ModernStyle.TextSecondary, roll.Key + " (excluded)");
                    }
                    else
                    {
                        // Normal player
                        ImGui.TextColored(ModernStyle.TextPrimary, roll.Key);
                    }
                    
                    // Bonus prize indicator
                    if (isBonusHit)
                    {
                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(new Vector4(1.0f, 0.85f, 0.0f, 1.0f), FontAwesomeIcon.MoneyBillWave.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                        {
                            var bp = configuration.BonusPrizes.FirstOrDefault(b => b.Number == roll.Value);
                            var label = bp != null && !string.IsNullOrWhiteSpace(bp.Prize) ? $": {bp.Prize}" : "";
                            ImGui.SetTooltip($"Bonus prize! Rolled {roll.Value}{label}");
                        }
                    }

                    ImGui.TableNextColumn();

                    // Color code roll values
                    Vector4 rollColor = roll.Value <= 100 ?
                        ModernStyle.DangerRed : // Red for strippers
                        ModernStyle.TextPrimary; // White for normal
                    
                    // Bonus prize hits get gold color too
                    if (isBonusHit)
                        rollColor = new Vector4(1.0f, 0.85f, 0.0f, 1.0f);

                    // Add special formatting for high rolls
                    if (roll.Value >= 900)
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ModernStyle.SuccessGreen, FontAwesomeIcon.Star.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                    }

                    ImGui.TextColored(rollColor, roll.Value.ToString());
                    }

                    ImGui.EndTable();
                }
            }
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();

        // Winner announcement and copy functionality - OUTSIDE the child window to prevent clipping
        ShowWinnerInfo(gameRolls);
    }

    private void ShowWinnerInfo(IReadOnlyDictionary<string, int> gameRolls)
    {
        // Determine winners and strippers for display
        var currentRoundWinners = plugin.GetCurrentRoundWinners();
        
        if (currentRoundWinners.Count > 0)
        {
            // Generate stripper list
            var stripperList = gameRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
            string stripListDisplay = stripperList.Count > 0 ? string.Join(", ", stripperList) : "None";

            // Winner announcement card - adjust height based on winner count
            int cardHeight = currentRoundWinners.Count > 1 ? 100 : 80;
            
            ImGui.Spacing();
            ModernStyle.ApplyCardStyle();
            if (ImGui.BeginChild("WinnerCard", new Vector2(0, cardHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                string status = plugin.IsRollingPhase ? " (Tentative)" : "";

                // Add some vertical spacing for centering
                ImGui.Spacing();

                // Winner announcement with icons
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ModernStyle.WarningYellow, FontAwesomeIcon.Trophy.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                
                if (currentRoundWinners.Count == 1)
                {
                    int roll = gameRolls.TryGetValue(currentRoundWinners[0], out int r) ? r : 0;
                    ImGui.TextColored(ModernStyle.WarningYellow, $"Winner: {currentRoundWinners[0]} ({roll}){status}");
                }
                else
                {
                    var winnerDetails = currentRoundWinners.Select(w => 
                        $"{w} ({(gameRolls.TryGetValue(w, out int r) ? r : 0)})"
                    );
                    ImGui.TextColored(ModernStyle.WarningYellow, $"Winners: {string.Join(", ", winnerDetails)}{status}");
                }

                if (stripperList.Count > 0)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ModernStyle.DangerRed, FontAwesomeIcon.Heart.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine();
                    ImGui.TextColored(ModernStyle.DangerRed, $"Strippers: {stripListDisplay}");
                }
                else
                {
                    ImGui.TextColored(ModernStyle.TextSecondary, "No strippers this round!");
                }
            }
            ImGui.EndChild();
            ModernStyle.PopCardStyle();
        }
        else if (plugin.IsRollingPhase && gameRolls.Count > 0)
        {
            // Show tentative winner during rolling phase
            var sortedRolls = gameRolls.OrderByDescending(kvp => kvp.Value).ToList();
            
            // Determine tentative winners based on NumberOfWinners setting
            var tentativeWinners = new List<KeyValuePair<string, int>>();
            int winnersNeeded = Math.Min(configuration.NumberOfWinners, sortedRolls.Count);
            
            for (int i = 0; i < winnersNeeded && i < sortedRolls.Count; i++)
            {
                tentativeWinners.Add(sortedRolls[i]);
            }
            
            if (tentativeWinners.Count > 0)
            {
                // Generate stripper list
                var stripperList = gameRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
                string stripListDisplay = stripperList.Count > 0 ? string.Join(", ", stripperList) : "None";

                // Winner announcement card
                int cardHeight = tentativeWinners.Count > 1 ? 100 : 80;
                
                ImGui.Spacing();
                ModernStyle.ApplyCardStyle();
                if (ImGui.BeginChild("WinnerCard", new Vector2(0, cardHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    ImGui.Spacing();

                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ModernStyle.WarningYellow, FontAwesomeIcon.Trophy.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine();
                    
                    if (tentativeWinners.Count == 1)
                    {
                        ImGui.TextColored(ModernStyle.WarningYellow, $"Winner: {tentativeWinners[0].Key} ({tentativeWinners[0].Value}) (Tentative)");
                    }
                    else
                    {
                        var winnerText = string.Join(", ", tentativeWinners.Select(w => $"{w.Key} ({w.Value})"));
                        ImGui.TextColored(ModernStyle.WarningYellow, $"Winners: {winnerText} (Tentative)");
                    }

                    if (stripperList.Count > 0)
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ModernStyle.DangerRed, FontAwesomeIcon.Heart.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                        ImGui.TextColored(ModernStyle.DangerRed, $"Strippers: {stripListDisplay}");
                    }
                    else
                    {
                        ImGui.TextColored(ModernStyle.TextSecondary, "No strippers this round!");
                    }
                }
                ImGui.EndChild();
                ModernStyle.PopCardStyle();
            }
        }
    }

    private void DrawSettingsTab()
    {
        // Theme Selection at the top
        ModernStyle.ApplyCardStyle();
        if (ImGui.BeginChild("ThemeCard", new Vector2(0, 90), true, ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Palette.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.AccentPurple, "Theme Selection");
            ImGui.Separator();
            ImGui.Spacing();
            
            var currentTheme = (ThemeType)configuration.SelectedTheme;
            if (ImGui.BeginCombo("Color Theme", GetThemeDisplayName(currentTheme)))
            {
                foreach (var theme in Enum.GetValues<ThemeType>())
                {
                    bool isSelected = currentTheme == theme;
                    if (ImGui.Selectable(GetThemeDisplayName(theme), isSelected))
                    {
                        configuration.SelectedTheme = (int)theme;
                        configuration.Save();
                        ModernStyle.ApplyTheme(theme); // Apply immediately
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Choose your favorite color theme for the interface!");
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Game Settings
        ModernStyle.ApplyCardStyle();
        if (ImGui.BeginChild("GameSettingsCard", new Vector2(0, 140), true, ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Cog.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.AccentPurple, "Game Settings");
            ImGui.Separator();
            ImGui.Spacing();
            
            var rollTimeout = configuration.RollTimeout;
            if (ImGui.SliderInt("Roll Timeout (seconds)", ref rollTimeout, 10, 60))
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
            
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Winner Selection Mode:");
            ImGui.SameLine(ImGui.GetWindowWidth() - 200);
            var currentMode = configuration.WinnerMode;
            ImGui.SetNextItemWidth(180);
            if (ImGui.BeginCombo("##WinnerMode", GetWinnerModeName(currentMode)))
            {
                foreach (var mode in Enum.GetValues<WinnerSelectionMode>())
                {
                    if (ImGui.Selectable(GetWinnerModeName(mode), currentMode == mode))
                    {
                        configuration.WinnerMode = mode;
                        configuration.Save();
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(GetWinnerModeDescription(mode));
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(GetWinnerModeDescription(currentMode));
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Auto-Skip Players
        ModernStyle.ApplyCardStyle();
        float autoSkipHeight = configuration.AutoSkipPlayers.Count > 0 ? Math.Min(200f, 55f + configuration.AutoSkipPlayers.Count * 25f) : 70f;
        if (ImGui.BeginChild("AutoSkipCard", new Vector2(0, autoSkipHeight), true, ImGuiWindowFlags.None))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Forward.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.AccentPurple, "Auto-Skip Players");
            ImGui.Separator();
            ImGui.Spacing();
            
            ImGui.TextColored(ModernStyle.TextSecondary, "These players will be automatically skipped if they win:");
            ImGui.Spacing();
            
            int skipToRemove = -1;
            string newSkipName = "";
            
            if (ImGui.BeginTable("AutoSkipTable", 2, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Player Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 35);
                
                for (int i = 0; i < configuration.AutoSkipPlayers.Count; i++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var name = configuration.AutoSkipPlayers[i] ?? "";
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputText($"##AutoSkip{i}", ref name, 60))
                    {
                        configuration.AutoSkipPlayers[i] = name;
                        configuration.Save();
                    }
                    ImGui.TableNextColumn();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##DelSkip{i}", new Vector2(25, 20)))
                    {
                        skipToRemove = i;
                    }
                    ImGui.PopFont();
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Remove");
                }
                
                // Add row
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##AutoSkipNew", "Enter player name...", ref newSkipName, 60);
                if (ImGui.IsItemDeactivatedAfterEdit() && !string.IsNullOrWhiteSpace(newSkipName))
                {
                    if (!configuration.AutoSkipPlayers.Any(s => s.Equals(newSkipName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        configuration.AutoSkipPlayers.Add(newSkipName.Trim());
                        configuration.Save();
                    }
                }
                ImGui.TableNextColumn();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##AddSkip", new Vector2(25, 20)) && !string.IsNullOrWhiteSpace(newSkipName))
                {
                    if (!configuration.AutoSkipPlayers.Any(s => s.Equals(newSkipName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        configuration.AutoSkipPlayers.Add(newSkipName.Trim());
                        configuration.Save();
                    }
                }
                ImGui.PopFont();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Add");
                
                ImGui.EndTable();
            }
            
            if (skipToRemove >= 0 && skipToRemove < configuration.AutoSkipPlayers.Count)
            {
                configuration.AutoSkipPlayers.RemoveAt(skipToRemove);
                configuration.Save();
            }
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Chat Channel Settings (scrollable for longer content)
        ModernStyle.ApplyCardStyle();
        if (ImGui.BeginChild("ChatChannelCard", new Vector2(0, 145), true, ImGuiWindowFlags.None))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Comments.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.AccentPurple, "Chat Channel Settings");
            ImGui.Separator();
            ImGui.Spacing();
            
            // Basic channel settings in a compact layout
            if (ImGui.BeginTable("ChannelSettings", 2, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Channel", ImGuiTableColumnFlags.WidthStretch);
                
                // Rules Channel
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Rules Channel:");
                ImGui.TableNextColumn();
                var rulesChannel = configuration.ChatChannels.RulesChannel;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginCombo("##Rules", rulesChannel.ToString()))
                {
                    foreach (var channel in Enum.GetValues<ChatChannelType>())
                    {
                        if (ImGui.Selectable(channel.ToString(), rulesChannel == channel))
                        {
                            configuration.ChatChannels.RulesChannel = channel;
                            configuration.Save();
                        }
                    }
                    ImGui.EndCombo();
                }
                
                // Results Channel
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Results Channel:");
                ImGui.TableNextColumn();
                var resultsChannel = configuration.ChatChannels.ResultsChannel;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginCombo("##Results", resultsChannel.ToString()))
                {
                    foreach (var channel in Enum.GetValues<ChatChannelType>())
                    {
                        if (ImGui.Selectable(channel.ToString(), resultsChannel == channel))
                        {
                            configuration.ChatChannels.ResultsChannel = channel;
                            configuration.Save();
                        }
                    }
                    ImGui.EndCombo();
                }
                
                ImGui.EndTable();
            }
            
            ImGui.Spacing();
            
            // Winner-specific channels
            var useWinnerChannels = configuration.ChatChannels.UseWinnerSpecificChannels;
            if (ImGui.Checkbox("Use Winner-Specific Channels", ref useWinnerChannels))
            {
                configuration.ChatChannels.UseWinnerSpecificChannels = useWinnerChannels;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Each winner outputs to their own designated channel");
                
            if (useWinnerChannels && ImGui.BeginTable("WinnerChannels", 2, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Channel", ImGuiTableColumnFlags.WidthStretch);
                
                for (int i = 0; i < Math.Min(configuration.NumberOfWinners, 2); i++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"Winner #{i + 1}:");
                    ImGui.TableNextColumn();
                    
                    var winnerChannel = i switch
                    {
                        0 => configuration.ChatChannels.Winner1Channel,
                        1 => configuration.ChatChannels.Winner2Channel,
                        _ => configuration.ChatChannels.ResultsChannel
                    };
                    
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.BeginCombo($"##Winner{i + 1}", winnerChannel.ToString()))
                    {
                        foreach (var channel in Enum.GetValues<ChatChannelType>())
                        {
                            if (ImGui.Selectable(channel.ToString(), winnerChannel == channel))
                            {
                                switch (i)
                                {
                                    case 0: configuration.ChatChannels.Winner1Channel = channel; break;
                                    case 1: configuration.ChatChannels.Winner2Channel = channel; break;
                                }
                                configuration.Save();
                            }
                        }
                        ImGui.EndCombo();
                    }
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Jackpot Settings
        ModernStyle.ApplyCardStyle();
        float jackpotCardHeight = configuration.EnableJackpot ? 140f : 85f;
        if (ImGui.BeginChild("JackpotCard", new Vector2(0, jackpotCardHeight), true, ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Star.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.AccentPurple, "Jackpot Settings");
            ImGui.Separator();
            ImGui.Spacing();
            
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
                        if (ImGui.Selectable(channel.ToString(), jackpotChannel == channel))
                        {
                            configuration.ChatChannels.JackpotChannel = channel;
                            configuration.Save();
                        }
                    }
                    ImGui.EndCombo();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Channel where jackpot wins are announced");
            }
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Bonus Prizes Settings
        ModernStyle.ApplyCardStyle();
        float bonusCardHeight = configuration.EnableBonusPrizes ? 260f : 85f;
        if (ImGui.BeginChild("BonusPrizesCard", new Vector2(0, bonusCardHeight), true, ImGuiWindowFlags.None))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.MoneyBillWave.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.AccentPurple, "Bonus Prizes");
            ImGui.Separator();
            ImGui.Spacing();
            
            var enableBonusPrizes = configuration.EnableBonusPrizes;
            if (ImGui.Checkbox("Enable Bonus Prizes", ref enableBonusPrizes))
            {
                configuration.EnableBonusPrizes = enableBonusPrizes;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("When enabled, rolling a bonus prize number awards gil without affecting the round outcome");

            if (enableBonusPrizes)
            {
                ImGui.Spacing();
                ImGui.TextColored(ModernStyle.TextSecondary, "Players who roll any of these numbers win a bonus prize:");
                ImGui.Spacing();
                
                // Bonus numbers list
                int numberToRemove = -1;
                var numberToAdd = new BonusPrize();
                
                if (ImGui.BeginTable("BonusNumbersTable", 3, ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Number", ImGuiTableColumnFlags.WidthFixed, 70);
                    ImGui.TableSetupColumn("Prize", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 35);
                    
                    for (int i = 0; i < configuration.BonusPrizes.Count; i++)
                    {
                        var bp = configuration.BonusPrizes[i];
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var num = bp.Number;
                        ImGui.SetNextItemWidth(55);
                        if (ImGui.InputInt($"##BonusNum{i}", ref num, 0, 0))
                        {
                            num = Math.Max(0, num);
                            bp.Number = num;
                            configuration.Save();
                        }
                        ImGui.TableNextColumn();
                        var prize = bp.Prize ?? "";
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputTextWithHint($"##BonusPrize{i}", "e.g. 100k gil, Fat Cat minion...", ref prize, 60))
                        {
                            bp.Prize = prize;
                            configuration.Save();
                        }
                        ImGui.TableNextColumn();
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##DelBonus{i}", new Vector2(25, 20)))
                        {
                            numberToRemove = i;
                        }
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Remove");
                    }
                    
                    // Add new number row
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(55);
                    ImGui.InputInt("##BonusNumNew", ref newBonusNumber, 0, 0);
                    if (ImGui.IsItemDeactivatedAfterEdit() && newBonusNumber > 0)
                    {
                        numberToAdd.Number = newBonusNumber;
                    }
                    ImGui.TableNextColumn();
                    var newPrize = "";
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint("##BonusPrizeNew", "e.g. 100k gil, Fat Cat minion...", ref newPrize, 60);
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        numberToAdd.Prize = newPrize;
                    }
                    ImGui.TableNextColumn();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##AddBonus", new Vector2(25, 20)) && newBonusNumber > 0)
                    {
                        numberToAdd.Number = newBonusNumber;
                        numberToAdd.Prize = newPrize;
                    }
                    ImGui.PopFont();
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Add");
                    
                    ImGui.EndTable();
                }
                
                // Process add/remove outside the table loop
                if (numberToRemove >= 0 && numberToRemove < configuration.BonusPrizes.Count)
                {
                    configuration.BonusPrizes.RemoveAt(numberToRemove);
                    configuration.Save();
                }
                if (numberToAdd.Number > 0)
                {
                    if (!configuration.BonusPrizes.Any(bp => bp.Number == numberToAdd.Number))
                    {
                        configuration.BonusPrizes.Add(new BonusPrize { Number = numberToAdd.Number, Prize = numberToAdd.Prize });
                        configuration.BonusPrizes = configuration.BonusPrizes.OrderBy(bp => bp.Number).ToList();
                        newBonusNumber = 0;
                        configuration.Save();
                    }
                }
                
                ImGui.Spacing();
                
                // Channel selection
                var bonusChannel = configuration.ChatChannels.BonusPrizesChannel;
                ImGui.SetNextItemWidth(150);
                if (ImGui.BeginCombo("Announcement Channel", bonusChannel.ToString()))
                {
                    foreach (var channel in Enum.GetValues<ChatChannelType>())
                    {
                        if (ImGui.Selectable(channel.ToString(), bonusChannel == channel))
                        {
                            configuration.ChatChannels.BonusPrizesChannel = channel;
                            configuration.Save();
                        }
                    }
                    ImGui.EndCombo();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Channel where bonus prize wins are announced");
            }
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Roll Detection and other settings
        ModernStyle.ApplyCardStyle();
        if (ImGui.BeginChild("DetectionCard", new Vector2(0, 135), true, ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Dice.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.AccentPurple, "Roll Detection");
            ImGui.Separator();
            ImGui.Spacing();
            
            var enableRandom = configuration.EnableRandomDetection;
            if (ImGui.Checkbox("Detect /random rolls", ref enableRandom))
            {
                configuration.EnableRandomDetection = enableRandom;
                configuration.Save();
            }

            var enableDice = configuration.EnableDiceDetection;
            if (ImGui.Checkbox("Detect /dice rolls", ref enableDice))
            {
                configuration.EnableDiceDetection = enableDice;
                configuration.Save();
            }
            
            var debugMode = configuration.DebugMode;
            if (ImGui.Checkbox("Debug Mode", ref debugMode))
            {
                configuration.DebugMode = debugMode;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Enables special debug roll patterns for testing");
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Announcement Templates (expandable section)
        ModernStyle.ApplyCardStyle();
        if (ImGui.BeginChild("AnnouncementCard", new Vector2(0, 300), true, ImGuiWindowFlags.None))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Bullhorn.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(ModernStyle.AccentPurple, "Announcement Templates");
            ImGui.Separator();
            ImGui.Spacing();
            
            ImGui.TextColored(ModernStyle.TextSecondary, "Customize all announcement messages. Use placeholders for dynamic content:");
            
            if (ImGui.CollapsingHeader("Game Rules Messages"))
            {
                configuration.Announcements.RulesLine1 = DrawAnnouncementInput("Rules Line 1", configuration.Announcements.RulesLine1, 
                    "Main rules line with roll instructions");
                configuration.Announcements.RulesLine2 = DrawAnnouncementInput("Rules Line 2", configuration.Announcements.RulesLine2, 
                    "Instructions about where to post T/D");
                configuration.Announcements.RulesLine3 = DrawAnnouncementInput("Rules Line 3", configuration.Announcements.RulesLine3, 
                    "Dare rules and stripper conditions");
                configuration.Announcements.RulesLine4 = DrawAnnouncementInput("Rules Line 4", configuration.Announcements.RulesLine4, 
                    "Wi-Fi/Discord message line");
                configuration.Announcements.RulesLine5 = DrawAnnouncementInput("Rules Line 5", configuration.Announcements.RulesLine5, 
                    "Pre-countdown announcement");
            }
            
            if (ImGui.CollapsingHeader("Countdown Messages"))
            {
                configuration.Announcements.CountdownStart = DrawAnnouncementInput("Countdown Start", configuration.Announcements.CountdownStart, "3...");
                configuration.Announcements.CountdownMiddle = DrawAnnouncementInput("Countdown Middle", configuration.Announcements.CountdownMiddle, "2...");
                configuration.Announcements.CountdownEnd = DrawAnnouncementInput("Countdown End", configuration.Announcements.CountdownEnd, "1...");
                configuration.Announcements.CountdownGo = DrawAnnouncementInput("Go Signal", configuration.Announcements.CountdownGo, "Go!");
            }
            
            if (ImGui.CollapsingHeader("Result Messages"))
            {
                configuration.Announcements.SingleWinnerResult = DrawAnnouncementInput("Single Winner", configuration.Announcements.SingleWinnerResult, 
                    "Format for single winner results");
                configuration.Announcements.MultipleWinnersResult = DrawAnnouncementInput("Multiple Winners", configuration.Announcements.MultipleWinnersResult, 
                    "Format for multiple winners (traditional mode)");
                configuration.Announcements.WinnerSpecificResult = DrawAnnouncementInput("Winner-Specific", configuration.Announcements.WinnerSpecificResult, 
                    "Format for individual winner announcements");
                configuration.Announcements.PassedWinnerResult = DrawAnnouncementInput("Passed Winner", configuration.Announcements.PassedWinnerResult, 
                    "Format when a winner passes to the next player");
                configuration.Announcements.JackpotWinnerResult = DrawAnnouncementInput("Jackpot Winner", configuration.Announcements.JackpotWinnerResult, 
                    "Format for jackpot winner announcements (non-passable)");
                configuration.Announcements.HighestAsksLowestResult = DrawAnnouncementInput("Highest Asks Lowest", configuration.Announcements.HighestAsksLowestResult, 
                    "Format when highest roller asks lowest (uses {WINNER_NAME}, {WINNER_ROLL}, {OTHER_WINNER}, {OTHER_ROLL})");
                configuration.Announcements.BonusPrizeResult = DrawAnnouncementInput("Bonus Prize", configuration.Announcements.BonusPrizeResult, 
                    "Format for bonus prize summary (uses {BONUS_PRIZE_WINNERS})");
            }
            
            if (ImGui.CollapsingHeader("Available Placeholders"))
            {
                ImGui.TextColored(ModernStyle.TextSecondary, "Click any placeholder to copy it:");
                ImGui.Spacing();
                
                foreach (var placeholder in AnnouncementTemplates.PlaceholderDescriptions)
                {
                    if (ImGui.SmallButton(placeholder.Key))
                    {
                        ImGui.SetClipboardText(placeholder.Key);
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"{placeholder.Value}\nClick to copy to clipboard");
                    
                    ImGui.SameLine();
                    ImGui.TextColored(ModernStyle.TextSecondary, $"- {placeholder.Value}");
                }
            }
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Profile Management
        DrawProfileManagementSection();
    }
    
    private string DrawAnnouncementInput(string label, string value, string tooltip)
    {
        ImGui.Text($"{label}:");
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText($"##{label}", ref value, 500))
        {
            configuration.Save();
        }
        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(tooltip))
            ImGui.SetTooltip(tooltip);
        ImGui.Spacing();
        return value;
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
            ImGui.TextColored(ModernStyle.AccentPurple, "Profile Management");
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
                    importFeedback = $"Loaded profile '{selectedProfile}'";
                    importSuccess = true;
                    selectedProfile = "";
                }
                else
                {
                    importFeedback = "Failed to load profile";
                    importSuccess = false;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Delete", new Vector2(60, 0)) && !string.IsNullOrEmpty(selectedProfile))
            {
                if (configuration.DeleteProfile(selectedProfile))
                {
                    importFeedback = $"Deleted profile '{selectedProfile}'";
                    importSuccess = true;
                    selectedProfile = "";
                }
                else
                {
                    importFeedback = "Failed to delete profile";
                    importSuccess = false;
                }
            }

            // Save New Profile
            ImGui.SetNextItemWidth(200);
            ImGui.InputTextWithHint("##NewProfileName", "Enter profile name...", ref newProfileName, 50);
            
            ImGui.SameLine();
            if (ImGui.Button("Save Current", new Vector2(90, 0)) && !string.IsNullOrWhiteSpace(newProfileName))
            {
                if (configuration.SaveCurrentAsProfile(newProfileName.Trim()))
                {
                    importFeedback = $"Saved profile '{newProfileName.Trim()}'";
                    importSuccess = true;
                    newProfileName = "";
                }
                else
                {
                    importFeedback = "Failed to save profile";
                    importSuccess = false;
                }
            }

            // Export/Import Section
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            if (ImGui.Button("Export Profile", new Vector2(110, 0)))
            {
                if (!string.IsNullOrEmpty(selectedProfile))
                {
                    try
                    {
                        var profileData = configuration.ExportProfileToString(selectedProfile);
                        ImGui.SetClipboardText(profileData);
                        importFeedback = $"Profile '{selectedProfile}' copied to clipboard!";
                        importSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        importFeedback = $"Export failed: {ex.Message}";
                        importSuccess = false;
                    }
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Copy selected profile to clipboard as JSON");
            
            ImGui.SameLine();
            if (ImGui.Button("Export All", new Vector2(85, 0)))
            {
                try
                {
                    var allProfilesData = configuration.ExportAllProfilesToString();
                    ImGui.SetClipboardText(allProfilesData);
                    importFeedback = "All profiles copied to clipboard!";
                    importSuccess = true;
                }
                catch (Exception ex)
                {
                    importFeedback = $"Export failed: {ex.Message}";
                    importSuccess = false;
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Copy all profiles to clipboard as JSON");
            
            ImGui.SameLine();
            if (ImGui.Button("Import", new Vector2(65, 0)))
            {
                var clipboardData = ImGui.GetClipboardText();
                if (!string.IsNullOrWhiteSpace(clipboardData))
                {
                    importData = clipboardData;
                    importProfileName = "";
                    importFeedback = "";
                    showImportDialog = true;
                }
                else
                {
                    importFeedback = "Clipboard is empty or contains no text";
                    importSuccess = false;
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Import profile(s) from clipboard JSON data");
        }
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        // Show import feedback if there's any
        if (!string.IsNullOrEmpty(importFeedback))
        {
            ImGui.Spacing();
            var feedbackColor = importSuccess ? ModernStyle.SuccessGreen : ModernStyle.DangerRed;
            ImGui.TextColored(feedbackColor, importFeedback);
        }
        
        DrawImportDialog();
    }

    private void DrawImportDialog()
    {
        if (!showImportDialog) return;

        ImGui.SetNextWindowSize(new Vector2(500, 300), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Import Profile", ref showImportDialog))
        {
            ImGui.TextColored(ModernStyle.AccentPurple, "Import Profile from Clipboard");
            ImGui.Separator();
            ImGui.Spacing();
            
            // Analyze the clipboard data
            bool isMultipleProfiles = false;
            bool isValidJson = false;
            string profilePreview = "";
            
            try
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                
                if (importData.TrimStart().StartsWith("["))
                {
                    // Try to parse as multiple profiles
                    var profiles = System.Text.Json.JsonSerializer.Deserialize<List<GameProfile>>(importData, jsonOptions);
                    if (profiles != null && profiles.Count > 0)
                    {
                        isMultipleProfiles = true;
                        isValidJson = true;
                        profilePreview = $"Found {profiles.Count} profiles: {string.Join(", ", profiles.Select(p => p.Name))}";
                    }
                }
                else
                {
                    // Try to parse as single profile
                    var profile = System.Text.Json.JsonSerializer.Deserialize<GameProfile>(importData, jsonOptions);
                    if (profile != null && !string.IsNullOrEmpty(profile.Name))
                    {
                        isValidJson = true;
                        profilePreview = $"Profile: {profile.Name}";
                        importProfileName = profile.Name; // Default to original name
                    }
                }
            }
            catch (Exception ex)
            {
                isValidJson = false;
                profilePreview = $"JSON parsing error: {ex.Message}";
            }
            
            if (isValidJson)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ModernStyle.SuccessGreen, FontAwesomeIcon.Check.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColored(ModernStyle.SuccessGreen, "Valid profile data detected");
                ImGui.Text(profilePreview);
                ImGui.Spacing();
                
                if (isMultipleProfiles)
                {
                    ImGui.TextColored(ModernStyle.TextSecondary, "Multiple profiles will be imported. Existing profiles with the same names will be overwritten.");
                    ImGui.Spacing();
                    
                    if (ImGui.Button("Import All Profiles", new Vector2(150, 30)))
                    {
                        try
                        {
                            if (configuration.ImportAllProfilesFromString(importData))
                            {
                                importFeedback = "Successfully imported all profiles!";
                                importSuccess = true;
                                showImportDialog = false;
                            }
                            else
                            {
                                importFeedback = "Failed to import profiles";
                                importSuccess = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            importFeedback = $"Import error: {ex.Message}";
                            importSuccess = false;
                        }
                    }
                }
                else
                {
                    ImGui.Text("Profile Name:");
                    ImGui.SetNextItemWidth(300);
                    ImGui.InputTextWithHint("##ImportProfileName", "Enter name for this profile...", ref importProfileName, 50);
                    
                    if (string.IsNullOrWhiteSpace(importProfileName))
                    {
                        ImGui.TextColored(ModernStyle.WarningYellow, "Please enter a profile name");
                    }
                    else
                    {
                        // Check if profile already exists
                        var existingProfiles = configuration.GetProfileNames();
                        bool profileExists = existingProfiles.Any(p => p.Equals(importProfileName.Trim(), StringComparison.OrdinalIgnoreCase));
                        
                        if (profileExists)
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.TextColored(ModernStyle.WarningYellow, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                            ImGui.PopFont();
                            ImGui.SameLine();
                            ImGui.TextColored(ModernStyle.WarningYellow, "This will overwrite the existing profile with the same name");
                        }
                        
                        ImGui.Spacing();
                        if (ImGui.Button("Import Profile", new Vector2(120, 30)))
                        {
                            try
                            {
                                if (configuration.ImportProfileFromString(importData, importProfileName.Trim()))
                                {
                                    importFeedback = $"Successfully imported profile '{importProfileName.Trim()}'!";
                                    importSuccess = true;
                                    showImportDialog = false;
                                }
                                else
                                {
                                    importFeedback = "Failed to import profile";
                                    importSuccess = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                importFeedback = $"Import error: {ex.Message}";
                                importSuccess = false;
                            }
                        }
                    }
                }
            }
            else
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ModernStyle.DangerRed, FontAwesomeIcon.Times.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColored(ModernStyle.DangerRed, "Invalid profile data");
                ImGui.TextColored(ModernStyle.TextSecondary, profilePreview);
                ImGui.Spacing();
                ImGui.TextWrapped("The clipboard does not contain valid profile JSON data. Make sure you copied the complete profile data from an export operation.");
            }
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            if (ImGui.Button("Cancel", new Vector2(80, 25)))
            {
                showImportDialog = false;
            }
        }
        ImGui.End();
    }

    private void DrawAboutTab()
    {
        // Hero section
        ModernStyle.ApplyCardStyle();
        ImGui.BeginChild("AboutHero", new Vector2(-1, 120), true);
        
        var titleSize = ImGui.CalcTextSize("Truth or Dare for FFXIV");
        var windowWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX((windowWidth - titleSize.X) / 2);
        ImGui.TextColored(ModernStyle.AccentPurple, "Truth or Dare for FFXIV");
        
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize($"Version 2.3.6.0 by Kirin Avenleigh").X) / 2);
        ImGui.TextColored(ModernStyle.TextSecondary, "Version 2.3.6.0 by Kirin Avenleigh");
        
        ImGui.Spacing();
        ImGui.TextWrapped("Automated Truth or Dare game management with roll tracking, winner determination, and customizable announcements.");
        
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Features section
        ModernStyle.ApplyCardStyle();
        ImGui.BeginChild("AboutFeatures", new Vector2(-1, 235), true);
        
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Star.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.Text("Key Features");
        ImGui.Spacing();
        
        var features = new[]
        {
            "Automatic roll detection (/random and /dice)",
            "Multi-winner support (1-2 winners) with high/low mode",
            "Smart winner selection with exclusion rules",
            "Multi-channel chat output support",
            "Customizable announcement templates",
            "Winner-specific channel assignments",
            "Pass-to-next-winner functionality",
            "Theme system with 6 built-in themes",
            "Stripper identification (rolls ≤ 100)"
        };
        
        foreach (var feature in features)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Check.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.Text(feature);
        }
        
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
        
        ImGui.Spacing();
        
        // Usage section
        ModernStyle.ApplyCardStyle();
        ImGui.BeginChild("AboutUsage", new Vector2(-1, 180), true);
        
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(ModernStyle.AccentPurple, FontAwesomeIcon.Play.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.Text("How to Use");
        ImGui.Spacing();
        
        ImGui.Text("1."); ImGui.SameLine(); ImGui.Text("Click 'Start Game' to begin roll collection");
        ImGui.Text("2."); ImGui.SameLine(); ImGui.Text("Players roll using /random or /dice commands");
        ImGui.Text("3."); ImGui.SameLine(); ImGui.Text("Results are automatically posted after timeout");
        ImGui.Text("4."); ImGui.SameLine(); ImGui.Text("Use 'Pass Winner' if someone declines");
        ImGui.Text("5."); ImGui.SameLine(); ImGui.Text("Customize all settings in the Settings tab");
        
        ImGui.Spacing();
        ImGui.TextColored(ModernStyle.TextSecondary, "No macros required - everything is automated!");
        
        ImGui.EndChild();
        ModernStyle.PopCardStyle();
    }
    
    private string GetWinnerModeName(WinnerSelectionMode mode)
    {
        return mode switch
        {
            WinnerSelectionMode.TopHighest => "Top Highest",
            WinnerSelectionMode.HighestAndLowest => "Highest & Lowest",
            WinnerSelectionMode.BottomLowest => "Bottom Lowest",
            WinnerSelectionMode.Random => "Random Selection",
            WinnerSelectionMode.Middle => "Middle Rolls",
            WinnerSelectionMode.HighestAsksLowest => "Highest Asks Lowest",
            _ => mode.ToString()
        };
    }
    
    private string GetWinnerModeDescription(WinnerSelectionMode mode)
    {
        return mode switch
        {
            WinnerSelectionMode.TopHighest => "Select the N highest rolls (e.g., 98, 95 for 2 winners)",
            WinnerSelectionMode.HighestAndLowest => "Select highest and lowest rolls (e.g., 98 and 12)",
            WinnerSelectionMode.BottomLowest => "Select the N lowest rolls (e.g., 12, 15 for 2 winners)",
            WinnerSelectionMode.Random => "Randomly select N winners from all participants",
            WinnerSelectionMode.Middle => "Select the middle roll value(s) from all participants",
            WinnerSelectionMode.HighestAsksLowest => "Highest roller asks the lowest roller — Truth or Dare?",
            _ => "Unknown selection mode"
        };
    }
    

    
}
