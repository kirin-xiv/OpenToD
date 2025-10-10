using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace FFToD;

public enum ChatChannelType
{
    Say,
    Yell,
    Shout,
    Party,
    FreeCompany,
    Linkshell1,
    Linkshell2,
    Linkshell3,
    Linkshell4,
    Linkshell5,
    Linkshell6,
    Linkshell7,
    Linkshell8,
    CrossWorldLinkshell1,
    CrossWorldLinkshell2,
    CrossWorldLinkshell3,
    CrossWorldLinkshell4,
    CrossWorldLinkshell5,
    CrossWorldLinkshell6,
    CrossWorldLinkshell7,
    CrossWorldLinkshell8,
    Echo
}

[Serializable]
public class ChatChannelSettings
{
    public ChatChannelType RulesChannel { get; set; } = ChatChannelType.Shout;
    public ChatChannelType ResultsChannel { get; set; } = ChatChannelType.Yell;
    public ChatChannelType StatusChannel { get; set; } = ChatChannelType.Shout;
    public ChatChannelType CountdownChannel { get; set; } = ChatChannelType.Shout;
    
    // Winner-specific output channels
    public ChatChannelType Winner1Channel { get; set; } = ChatChannelType.Yell;
    public ChatChannelType Winner2Channel { get; set; } = ChatChannelType.Shout;
    public ChatChannelType Winner3Channel { get; set; } = ChatChannelType.Say;
    public bool UseWinnerSpecificChannels { get; set; } = false; // Toggle for this feature
    
    public string GetChannelCommand(ChatChannelType channel)
    {
        return channel switch
        {
            ChatChannelType.Say => "/s",
            ChatChannelType.Yell => "/y",
            ChatChannelType.Shout => "/sh",
            ChatChannelType.Party => "/p",
            ChatChannelType.FreeCompany => "/fc",
            ChatChannelType.Linkshell1 => "/l1",
            ChatChannelType.Linkshell2 => "/l2",
            ChatChannelType.Linkshell3 => "/l3",
            ChatChannelType.Linkshell4 => "/l4",
            ChatChannelType.Linkshell5 => "/l5",
            ChatChannelType.Linkshell6 => "/l6",
            ChatChannelType.Linkshell7 => "/l7",
            ChatChannelType.Linkshell8 => "/l8",
            ChatChannelType.CrossWorldLinkshell1 => "/cwl1",
            ChatChannelType.CrossWorldLinkshell2 => "/cwl2",
            ChatChannelType.CrossWorldLinkshell3 => "/cwl3",
            ChatChannelType.CrossWorldLinkshell4 => "/cwl4",
            ChatChannelType.CrossWorldLinkshell5 => "/cwl5",
            ChatChannelType.CrossWorldLinkshell6 => "/cwl6",
            ChatChannelType.CrossWorldLinkshell7 => "/cwl7",
            ChatChannelType.CrossWorldLinkshell8 => "/cwl8",
            ChatChannelType.Echo => "/e",
            _ => "/sh"
        };
    }
}

[Serializable]
public class AnnouncementTemplates
{
    // Game Rules Flow
    public string RulesLine1 { get; set; } = "Truth or Dare: {ROLL_INSTRUCTIONS} {NO_REPEAT_TEXT}";
    public string RulesLine2 { get; set; } = "Keep T/D in {RESULTS_CHANNEL}.";
    public string RulesLine3 { get; set; } = "Max 3 rounds per dare. If you roll 100 or under, remove one item of clothing of your choice.";
    public string RulesLine4 { get; set; } = "{CUSTOM_WIFI_MESSAGE}";
    public string RulesLine5 { get; set; } = "--- Rolls begin after a short countdown ---";
    
    // Countdown Messages
    public string CountdownStart { get; set; } = "3...";
    public string CountdownMiddle { get; set; } = "2...";
    public string CountdownEnd { get; set; } = "1...";
    public string CountdownGo { get; set; } = "Go!";
    
    // Closing Messages
    public string ClosingCountdown1 { get; set; } = "Rolls closing in 3...";
    public string ClosingCountdown2 { get; set; } = "2...";
    public string ClosingCountdown3 { get; set; } = "1...";
    public string RollsClosed { get; set; } = "--- Rolls are now closed ---";
    
    // Result Messages
    public string SingleWinnerResult { get; set; } = "Winner: {WINNER_NAME} ({WINNER_ROLL}) | Strippers: {STRIPPERS}";
    public string MultipleWinnersResult { get; set; } = "Winners: {WINNERS_LIST} | Strippers: {STRIPPERS}";
    public string WinnerSpecificResult { get; set; } = "Winner #{WINNER_NUMBER}: {WINNER_NAME} ({WINNER_ROLL}) | Strippers: {STRIPPERS}";
    public string PassedWinnerResult { get; set; } = "Winner #{WINNER_NUMBER}: {WINNER_NAME} ({WINNER_ROLL}) (passed from {PASSED_FROM}) | Strippers: {STRIPPERS}";
    
    // Available placeholders info for UI
    public static readonly Dictionary<string, string> PlaceholderDescriptions = new()
    {
        {"{ROLL_INSTRUCTIONS}", "Instructions for rolling (/random or /dice)"},
        {"{NO_REPEAT_TEXT}", "Text about not winning consecutive rounds"},
        {"{RESULTS_CHANNEL}", "The channel where results will be posted"},
        {"{CUSTOM_WIFI_MESSAGE}", "Your custom Wi-Fi/Discord message"},
        {"{WINNER_NAME}", "Name of the winner"},
        {"{WINNER_ROLL}", "The winning roll value"},
        {"{WINNER_NUMBER}", "Winner position (1, 2, 3)"},
        {"{WINNERS_LIST}", "List of all winners with rolls"},
        {"{STRIPPERS}", "List of players who rolled ≤100"},
        {"{PASSED_FROM}", "Name of player who passed their win"}
    };
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // Game settings
    public int RollTimeout { get; set; } = 17; // Matches your macro timing
    public string LocalPlayerName { get; set; } = "";
    public string LastWinner { get; set; } = "";
    public int NumberOfWinners { get; set; } = 1; // Number of winners to select (1-3)
    public List<string> LastWinners { get; set; } = new List<string>(); // Track multiple last winners
    
    // Debug settings
    public bool DebugMode { get; set; } = false; // Enable debug roll pattern for testing
    
    // Auto-post settings
    public bool AutoPostRules { get; set; } = true; // Auto-post rules to shout when starting
    public bool AutoPostResults { get; set; } = true; // Auto-post results to yell when rolls close
    public string CustomWiFiMessage { get; set; } = "Wi-Fi: https://discord.gg/ndb6BH5B"; // Customizable Wi-Fi/Discord line
    
    // Chat channel settings
    public ChatChannelSettings ChatChannels { get; set; } = new ChatChannelSettings();
    
    // Roll detection settings
    public bool EnableDiceDetection { get; set; } = true; // Enable /dice command detection
    public bool EnableRandomDetection { get; set; } = true; // Enable /random command detection
    
    // UI Theme settings
    public int SelectedTheme { get; set; } = 0; // 0 = PurpleDream (default)
    
    // Announcement templates
    public AnnouncementTemplates Announcements { get; set; } = new AnnouncementTemplates();

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface?.SavePluginConfig(this);
    }
}
