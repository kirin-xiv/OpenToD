using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Dalamud.Bindings.ImGui;

namespace FFToD;

public enum WinnerSelectionMode
{
    TopHighest,
    HighestAndLowest,
    BottomLowest,
    Random,
    Middle,
    HighestAsksLowest,
}

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
    
    public ChatChannelType Winner1Channel { get; set; } = ChatChannelType.Yell;
    public ChatChannelType Winner2Channel { get; set; } = ChatChannelType.Shout;
    public bool UseWinnerSpecificChannels { get; set; } = false;
    
    public ChatChannelType JackpotChannel { get; set; } = ChatChannelType.Yell;
    public ChatChannelType BonusPrizesChannel { get; set; } = ChatChannelType.Yell;
    
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
    public string RulesLine1 { get; set; } = "Truth or Dare: {ROLL_INSTRUCTIONS} {NO_REPEAT_TEXT}";
    public string RulesLine2 { get; set; } = "Keep T/D in {RESULTS_CHANNEL}.";
    public string RulesLine3 { get; set; } = "Max 3 rounds per dare. If you roll 100 or under, remove one item of clothing of your choice.";
    public string RulesLine4 { get; set; } = "{CUSTOM_WIFI_MESSAGE}";
    public string RulesLine5 { get; set; } = "--- Rolls begin after a short countdown ---";
    
    public string CountdownStart { get; set; } = "3...";
    public string CountdownMiddle { get; set; } = "2...";
    public string CountdownEnd { get; set; } = "1...";
    public string CountdownGo { get; set; } = "Go!";
    
    public string ClosingCountdown1 { get; set; } = "Rolls closing in 3...";
    public string ClosingCountdown2 { get; set; } = "2...";
    public string ClosingCountdown3 { get; set; } = "1...";
    public string RollsClosed { get; set; } = "--- Rolls are now closed ---";
    
    public string SingleWinnerResult { get; set; } = "Winner: {WINNER_NAME} ({WINNER_ROLL}) | Strippers: {STRIPPERS}";
    public string MultipleWinnersResult { get; set; } = "Winners: {WINNERS_LIST} | Strippers: {STRIPPERS}";
    public string WinnerSpecificResult { get; set; } = "Winner #{WINNER_NUMBER}: {WINNER_NAME} ({WINNER_ROLL}) | Strippers: {STRIPPERS}";
    public string PassedWinnerResult { get; set; } = "Winner #{WINNER_NUMBER}: {WINNER_NAME} ({WINNER_ROLL}) (passed from {PASSED_FROM}) | Strippers: {STRIPPERS}";
    public string JackpotWinnerResult { get; set; } = "JACKPOT! {WINNER_NAME} rolled {JACKPOT_VALUE}! Host decides their fate! | Strippers: {STRIPPERS}";
    public string BonusPrizeResult { get; set; } = "Bonus Prizes! {BONUS_PRIZE_WINNERS}";
    public string HighestAsksLowestResult { get; set; } = "{WINNER_NAME} ({WINNER_ROLL}) asks {OTHER_WINNER} ({OTHER_ROLL}) — Truth or Dare? | Strippers: {STRIPPERS}";
    
    public static readonly Dictionary<string, string> PlaceholderDescriptions = new()
    {
        {"{ROLL_INSTRUCTIONS}", "Instructions for rolling (/random or /dice)"},
        {"{NO_REPEAT_TEXT}", "Text about not winning consecutive rounds"},
        {"{RESULTS_CHANNEL}", "The channel where results will be posted"},
        {"{CUSTOM_WIFI_MESSAGE}", "Your custom Wi-Fi/Discord message"},
        {"{WINNER_NAME}", "Name of the winner"},
        {"{WINNER_ROLL}", "The winning roll value"},
        {"{WINNER_NUMBER}", "Winner position (1, 2)"},
        {"{WINNERS_LIST}", "List of all winners with rolls"},
        {"{STRIPPERS}", "List of players who rolled ≤100"},
        {"{PASSED_FROM}", "Name of player who passed their win"},
        {"{JACKPOT_VALUE}", "The configured jackpot roll number"},
        {"{BONUS_PRIZE_WINNERS}", "List of bonus prize winners, e.g. 'PlayerA (420): 100k gil, PlayerB (911): Fat Cat'"},
        {"{OTHER_WINNER}", "The other paired player (e.g. lowest roller in HighestAsksLowest mode)"},
        {"{OTHER_ROLL}", "The other paired player's roll value"}
    };
}

[Serializable]
public class BonusPrize
{
    public int Number { get; set; }
    public string Prize { get; set; } = "";
}

[Serializable]
public class GameProfile
{
    public string Name { get; set; } = "";
    public int RollTimeout { get; set; } = 17;
    public int NumberOfWinners { get; set; } = 1;
    public WinnerSelectionMode WinnerMode { get; set; } = WinnerSelectionMode.TopHighest;
    public bool DebugMode { get; set; } = false;
    public bool EnableJackpot { get; set; } = false;
    public int JackpotValue { get; set; } = 666;
    public bool AutoPostRules { get; set; } = true;
    public bool AutoPostResults { get; set; } = true;
    public string CustomWiFiMessage { get; set; } = "";
    public ChatChannelSettings ChatChannels { get; set; } = new ChatChannelSettings();
    public bool EnableDiceDetection { get; set; } = true;
    public bool EnableRandomDetection { get; set; } = true;
    public int SelectedTheme { get; set; } = 0;
    public AnnouncementTemplates Announcements { get; set; } = new AnnouncementTemplates();
    public bool EnableBonusPrizes { get; set; } = false;
    public List<BonusPrize> BonusPrizes { get; set; } = new List<BonusPrize>();
    public List<string> AutoSkipPlayers { get; set; } = new List<string>();

    public GameProfile() { }

    public GameProfile(string name, Configuration config)
    {
        Name = name;
        RollTimeout = config.RollTimeout;
        NumberOfWinners = config.NumberOfWinners;
        WinnerMode = config.WinnerMode;
        DebugMode = config.DebugMode;
        EnableJackpot = config.EnableJackpot;
        JackpotValue = config.JackpotValue;
        AutoPostRules = config.AutoPostRules;
        AutoPostResults = config.AutoPostResults;
        CustomWiFiMessage = config.CustomWiFiMessage;
        ChatChannels = new ChatChannelSettings
        {
            RulesChannel = config.ChatChannels.RulesChannel,
            ResultsChannel = config.ChatChannels.ResultsChannel,
            StatusChannel = config.ChatChannels.StatusChannel,
            CountdownChannel = config.ChatChannels.CountdownChannel,
            Winner1Channel = config.ChatChannels.Winner1Channel,
            Winner2Channel = config.ChatChannels.Winner2Channel,
            UseWinnerSpecificChannels = config.ChatChannels.UseWinnerSpecificChannels,
            JackpotChannel = config.ChatChannels.JackpotChannel,
            BonusPrizesChannel = config.ChatChannels.BonusPrizesChannel
        };
        EnableDiceDetection = config.EnableDiceDetection;
        EnableRandomDetection = config.EnableRandomDetection;
        SelectedTheme = config.SelectedTheme;
        Announcements = new AnnouncementTemplates
        {
            RulesLine1 = config.Announcements.RulesLine1,
            RulesLine2 = config.Announcements.RulesLine2,
            RulesLine3 = config.Announcements.RulesLine3,
            RulesLine4 = config.Announcements.RulesLine4,
            RulesLine5 = config.Announcements.RulesLine5,
            CountdownStart = config.Announcements.CountdownStart,
            CountdownMiddle = config.Announcements.CountdownMiddle,
            CountdownEnd = config.Announcements.CountdownEnd,
            CountdownGo = config.Announcements.CountdownGo,
            ClosingCountdown1 = config.Announcements.ClosingCountdown1,
            ClosingCountdown2 = config.Announcements.ClosingCountdown2,
            ClosingCountdown3 = config.Announcements.ClosingCountdown3,
            RollsClosed = config.Announcements.RollsClosed,
            SingleWinnerResult = config.Announcements.SingleWinnerResult,
            MultipleWinnersResult = config.Announcements.MultipleWinnersResult,
            WinnerSpecificResult = config.Announcements.WinnerSpecificResult,
            PassedWinnerResult = config.Announcements.PassedWinnerResult,
            JackpotWinnerResult = config.Announcements.JackpotWinnerResult,
            BonusPrizeResult = config.Announcements.BonusPrizeResult,
            HighestAsksLowestResult = config.Announcements.HighestAsksLowestResult
        };
        EnableBonusPrizes = config.EnableBonusPrizes;
        BonusPrizes = config.BonusPrizes.Select(bp => new BonusPrize { Number = bp.Number, Prize = bp.Prize }).ToList();
        AutoSkipPlayers = new List<string>(config.AutoSkipPlayers);
    }

    public void ApplyToConfiguration(Configuration config)
    {
        config.RollTimeout = RollTimeout;
        config.NumberOfWinners = NumberOfWinners;
        config.WinnerMode = WinnerMode;
        config.DebugMode = DebugMode;
        config.EnableJackpot = EnableJackpot;
        config.JackpotValue = JackpotValue;
        config.AutoPostRules = AutoPostRules;
        config.AutoPostResults = AutoPostResults;
        config.CustomWiFiMessage = CustomWiFiMessage;
        config.ChatChannels.RulesChannel = ChatChannels.RulesChannel;
        config.ChatChannels.ResultsChannel = ChatChannels.ResultsChannel;
        config.ChatChannels.StatusChannel = ChatChannels.StatusChannel;
        config.ChatChannels.CountdownChannel = ChatChannels.CountdownChannel;
        config.ChatChannels.Winner1Channel = ChatChannels.Winner1Channel;
        config.ChatChannels.Winner2Channel = ChatChannels.Winner2Channel;
        config.ChatChannels.UseWinnerSpecificChannels = ChatChannels.UseWinnerSpecificChannels;
        config.ChatChannels.JackpotChannel = ChatChannels.JackpotChannel;
        config.ChatChannels.BonusPrizesChannel = ChatChannels.BonusPrizesChannel;
        config.EnableDiceDetection = EnableDiceDetection;
        config.EnableRandomDetection = EnableRandomDetection;
        config.SelectedTheme = SelectedTheme;
        config.Announcements.RulesLine1 = Announcements.RulesLine1;
        config.Announcements.RulesLine2 = Announcements.RulesLine2;
        config.Announcements.RulesLine3 = Announcements.RulesLine3;
        config.Announcements.RulesLine4 = Announcements.RulesLine4;
        config.Announcements.RulesLine5 = Announcements.RulesLine5;
        config.Announcements.CountdownStart = Announcements.CountdownStart;
        config.Announcements.CountdownMiddle = Announcements.CountdownMiddle;
        config.Announcements.CountdownEnd = Announcements.CountdownEnd;
        config.Announcements.CountdownGo = Announcements.CountdownGo;
        config.Announcements.ClosingCountdown1 = Announcements.ClosingCountdown1;
        config.Announcements.ClosingCountdown2 = Announcements.ClosingCountdown2;
        config.Announcements.ClosingCountdown3 = Announcements.ClosingCountdown3;
        config.Announcements.RollsClosed = Announcements.RollsClosed;
        config.Announcements.SingleWinnerResult = Announcements.SingleWinnerResult;
        config.Announcements.MultipleWinnersResult = Announcements.MultipleWinnersResult;
        config.Announcements.WinnerSpecificResult = Announcements.WinnerSpecificResult;
        config.Announcements.PassedWinnerResult = Announcements.PassedWinnerResult;
        config.Announcements.JackpotWinnerResult = Announcements.JackpotWinnerResult;
        config.Announcements.BonusPrizeResult = Announcements.BonusPrizeResult;
        config.Announcements.HighestAsksLowestResult = Announcements.HighestAsksLowestResult;
        config.EnableBonusPrizes = EnableBonusPrizes;
        config.BonusPrizes = BonusPrizes.Select(bp => new BonusPrize { Number = bp.Number, Prize = bp.Prize }).ToList();
        config.AutoSkipPlayers = new List<string>(AutoSkipPlayers);
    }
}

[Serializable]
public class RoundStatistics
{
    public int TotalRounds { get; set; } = 0;
    public int SessionRounds { get; set; } = 0;
    
    public void IncrementRounds()
    {
        TotalRounds++;
        SessionRounds++;
    }
    
    public void ResetSession()
    {
        SessionRounds = 0;
    }
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public int RollTimeout { get; set; } = 17;
    public string LocalPlayerName { get; set; } = "";
    public string LastWinner { get; set; } = "";
    public int NumberOfWinners { get; set; } = 1;
    public List<string> LastWinners { get; set; } = new List<string>();
    public WinnerSelectionMode WinnerMode { get; set; } = WinnerSelectionMode.TopHighest;
    
    public RoundStatistics Statistics { get; set; } = new RoundStatistics();
    
    public List<GameProfile> SavedProfiles { get; set; } = new List<GameProfile>();
    public string CurrentProfileName { get; set; } = "Default";
    
    public bool DebugMode { get; set; } = false;
    
    public bool EnableJackpot { get; set; } = false;
    public int JackpotValue { get; set; } = 666;
    
    public bool EnableBonusPrizes { get; set; } = false;
    public List<BonusPrize> BonusPrizes { get; set; } = new List<BonusPrize>();
    
    public List<string> AutoSkipPlayers { get; set; } = new List<string>();
    
    public bool AutoPostRules { get; set; } = true;
    public bool AutoPostResults { get; set; } = true;
    public string CustomWiFiMessage { get; set; } = "";
    
    public ChatChannelSettings ChatChannels { get; set; } = new ChatChannelSettings();
    
    public bool EnableDiceDetection { get; set; } = true;
    public bool EnableRandomDetection { get; set; } = true;
    
    public int SelectedTheme { get; set; } = 0;
    
    public AnnouncementTemplates Announcements { get; set; } = new AnnouncementTemplates();
    
    public bool IsAuthenticated { get; set; } = false;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        
        if (Statistics == null)
        {
            Statistics = new RoundStatistics();
        }
        
        // Migrate old bonus prize template to new format
        if (Version < 2)
        {
            if (Announcements.BonusPrizeResult.Contains("{BONUS_PRIZE_NUMBER}"))
            {
                Announcements.BonusPrizeResult = "Bonus Prizes! {BONUS_PRIZE_WINNERS}";
            }
            // Migrate old List<int> BonusPrizeNumbers to List<BonusPrize> BonusPrizes if needed
            Version = 2;
            Save();
        }
    }

    public void Save()
    {
        pluginInterface?.SavePluginConfig(this);
    }

    public bool SaveCurrentAsProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            return false;

        var existingProfile = SavedProfiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
        if (existingProfile != null)
        {
            SavedProfiles.Remove(existingProfile);
        }

        var newProfile = new GameProfile(profileName, this);
        SavedProfiles.Add(newProfile);
        CurrentProfileName = profileName;
        Save();
        return true;
    }

    public bool SaveFromClipboard(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            return false;

        try
        {
            var clipboardText = ImGui.GetClipboardText();
            if (string.IsNullOrWhiteSpace(clipboardText))
                return false;

            var profileData = JsonSerializer.Deserialize<GameProfile>(clipboardText);
            if (profileData == null)
                return false;

            var existingProfile = SavedProfiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
            if (existingProfile != null)
            {
                SavedProfiles.Remove(existingProfile);
            }

            profileData.Name = profileName;
            SavedProfiles.Add(profileData);
            Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool LoadProfile(string profileName)
    {
        var profile = SavedProfiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
        if (profile == null)
            return false;

        profile.ApplyToConfiguration(this);
        CurrentProfileName = profileName;
        Save();
        return true;
    }

    public bool DeleteProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName) || profileName.Equals("Default", StringComparison.OrdinalIgnoreCase))
            return false;

        var profile = SavedProfiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
        if (profile == null)
            return false;

        SavedProfiles.Remove(profile);
        if (CurrentProfileName.Equals(profileName, StringComparison.OrdinalIgnoreCase))
            CurrentProfileName = "Default";
        Save();
        return true;
    }

    public List<string> GetProfileNames()
    {
        var names = new List<string> { "Default" };
        names.AddRange(SavedProfiles.Select(p => p.Name));
        return names.Distinct().ToList();
    }

    public bool ProfileExists(string profileName)
    {
        if (profileName.Equals("Default", StringComparison.OrdinalIgnoreCase))
            return true;
        return SavedProfiles.Any(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
    }

    public bool ExportProfile(string profileName, string filePath)
    {
        try
        {
            GameProfile profileToExport;
            
            if (profileName.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                profileToExport = new GameProfile("Default", this);
            }
            else
            {
                profileToExport = SavedProfiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
                if (profileToExport == null)
                    return false;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(profileToExport, jsonOptions);
            File.WriteAllText(filePath, jsonString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ImportProfile(string filePath, string newProfileName = "")
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var jsonString = File.ReadAllText(filePath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var importedProfile = JsonSerializer.Deserialize<GameProfile>(jsonString, jsonOptions);
            if (importedProfile == null)
                return false;

            if (!string.IsNullOrWhiteSpace(newProfileName))
                importedProfile.Name = newProfileName;
            
            if (string.IsNullOrWhiteSpace(importedProfile.Name))
                importedProfile.Name = "Imported Profile";

            var existingProfile = SavedProfiles.FirstOrDefault(p => p.Name.Equals(importedProfile.Name, StringComparison.OrdinalIgnoreCase));
            if (existingProfile != null)
            {
                SavedProfiles.Remove(existingProfile);
            }

            SavedProfiles.Add(importedProfile);
            Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ExportAllProfiles(string filePath)
    {
        try
        {
            var allProfiles = new List<GameProfile>();
            
            allProfiles.Add(new GameProfile("Default", this));
            allProfiles.AddRange(SavedProfiles);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(allProfiles, jsonOptions);
            File.WriteAllText(filePath, jsonString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ImportAllProfiles(string filePath, bool overwriteExisting = false)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var jsonString = File.ReadAllText(filePath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var importedProfiles = JsonSerializer.Deserialize<List<GameProfile>>(jsonString, jsonOptions);
            if (importedProfiles == null)
                return false;

            foreach (var profile in importedProfiles)
            {
                if (profile.Name.Equals("Default", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(profile.Name))
                    profile.Name = "Imported Profile";

                var existingProfile = SavedProfiles.FirstOrDefault(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
                if (existingProfile != null)
                {
                    if (overwriteExisting)
                    {
                        SavedProfiles.Remove(existingProfile);
                        SavedProfiles.Add(profile);
                    }
                }
                else
                {
                    SavedProfiles.Add(profile);
                }
            }

            Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string ExportProfileToString(string profileName)
    {
        try
        {
            GameProfile profile;
            
            // Handle "Default" profile (current configuration)
            if (profileName.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                profile = new GameProfile("Default", this);
            }
            else
            {
                // Look for saved profile
                profile = SavedProfiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
                if (profile == null)
                    throw new ArgumentException($"Profile '{profileName}' not found");
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(profile, jsonOptions);
        }
        catch
        {
            throw;
        }
    }

    public string ExportAllProfilesToString()
    {
        try
        {
            var allProfiles = new List<GameProfile>();
            
            allProfiles.Add(new GameProfile("Default", this));
            allProfiles.AddRange(SavedProfiles);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(allProfiles, jsonOptions);
        }
        catch
        {
            throw;
        }
    }

    public bool ImportProfileFromString(string jsonData, string newProfileName = "")
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var profile = JsonSerializer.Deserialize<GameProfile>(jsonData, jsonOptions);
            if (profile == null) return false;

            if (!string.IsNullOrEmpty(newProfileName))
                profile.Name = newProfileName;

            var existingProfile = SavedProfiles.FirstOrDefault(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
            if (existingProfile != null)
                SavedProfiles.Remove(existingProfile);

            SavedProfiles.Add(profile);
            Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ImportAllProfilesFromString(string jsonData, bool overwriteExisting = true)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var profiles = JsonSerializer.Deserialize<List<GameProfile>>(jsonData, jsonOptions);
            if (profiles == null || profiles.Count == 0) return false;

            foreach (var profile in profiles)
            {
                if (profile.Name.Equals("Default", StringComparison.OrdinalIgnoreCase))
                    continue;

                var existingProfile = SavedProfiles.FirstOrDefault(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
                if (existingProfile != null)
                {
                    if (overwriteExisting)
                    {
                        SavedProfiles.Remove(existingProfile);
                        SavedProfiles.Add(profile);
                    }
                }
                else
                {
                    SavedProfiles.Add(profile);
                }
            }

            Save();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
