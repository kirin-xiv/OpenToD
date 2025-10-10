using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FFToD;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Truth or Dare";

    private const string CommandName = "/tod";
    private const string CommandStartName = "/todstart";
    private const string CommandStopName = "/todstop";
    private const string CommandStatusName = "/todstatus";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IChatGui chatGui;
    private readonly IClientState clientState;
    private readonly IPluginLog pluginLog;

    public readonly WindowSystem WindowSystem = new("FFToD");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private Configuration configuration;

    // Game state
    private bool isGameActive = false;
    private bool isRollingPhase = false;
    private readonly Dictionary<string, int> currentRolls = new();
    private readonly Dictionary<string, int> rollOrder = new(); // Track order of rolls for tiebreaking
    private int rollCounter = 0; // Counter for roll ordering
    private CancellationTokenSource? gameCancellation;

    // Store the current round winners separately from the "last winners" used for exclusion
    private List<string> currentRoundWinners = new List<string>();

    // Message queue for timed chat messages
    private readonly Queue<string> messageQueue = new();
    private DateTime lastMessageSent = DateTime.MinValue;
    private bool waitingForGoMessage = false;

    // Server list for name normalization
    private readonly HashSet<string> serverNames = new()
    {
        "Adamantoise", "Aegis", "Alexander", "Alpha", "Anima", "Asura", "Atomos", "Bahamut",
        "Balmung", "Behemoth", "Belias", "Brynhildr", "Cactuar", "Carbuncle", "Cerberus",
        "Chocobo", "Coeurl", "Cuchulainn", "Diabolos", "Durandal", "Dynamis", "Excalibur",
        "Exodus", "Faerie", "Famfrit", "Fenrir", "Garuda", "Gilgamesh", "Goblin", "Golem", "Gungnir",
        "Hades", "Halicarnassus", "Hifumi", "Hyperion", "Ifrit", "Ixion", "Jenova", "Kujata",
        "Lamia", "Leviathan", "Lich", "Louisoix", "Maduin", "Maelia", "Malboro", "Mandragora",
        "Masamune", "Mateus", "Midgardsormr", "Moogle", "Odin", "Omega", "Pandaemonium",
        "Phantom", "Phoenix", "Rafflesia", "Ragnarok", "Ramuh", "Ravana", "Raiden", "Ridill",
        "Sagittarius", "Sargatanas", "Seraph", "Shinryu", "Shiva", "Siren", "Sophia", "Spriggan",
        "Sphene", "Tiamat", "Titan", "Tonberry", "Tulimshar", "Typhon", "Ultima", "Ultros",
        "Unicorn", "Valefor", "Varis", "Yojimbo", "Zalera", "Zeromus", "Zodiark", "Zurvan"
    };

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui,
        IClientState clientState,
        IPluginLog pluginLog)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.chatGui = chatGui;
        this.clientState = clientState;
        this.pluginLog = pluginLog;

        configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        configuration.Initialize(pluginInterface);

        // Initialize ECommons
        ECommonsMain.Init(pluginInterface, this);

        mainWindow = new MainWindow(this, configuration);
        configWindow = new ConfigWindow(configuration);

        WindowSystem.AddWindow(mainWindow);
        WindowSystem.AddWindow(configWindow);

        commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Truth or Dare main window"
        });

        commandManager.AddHandler(CommandStartName, new CommandInfo(OnStartCommand)
        {
            HelpMessage = "Starts collecting rolls for Truth or Dare"
        });

        commandManager.AddHandler(CommandStopName, new CommandInfo(OnStopCommand)
        {
            HelpMessage = "Stops the current Truth or Dare round"
        });

        commandManager.AddHandler(CommandStatusName, new CommandInfo(OnStatusCommand)
        {
            HelpMessage = "Shows the current Truth or Dare status"
        });

        chatGui.ChatMessage += OnChatMessage;
        pluginInterface.UiBuilder.Draw += DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUI;
        pluginInterface.UiBuilder.OpenMainUi += OpenMainUI;

        // Subscribe to framework update for timed message sending
        if (pluginInterface.UiBuilder is { } uiBuilder)
        {
            uiBuilder.Draw += ProcessMessageQueue;
        }
    }

    public void Dispose()
    {
        gameCancellation?.Cancel();
        gameCancellation?.Dispose();

        WindowSystem.RemoveAllWindows();

        commandManager.RemoveHandler(CommandName);
        commandManager.RemoveHandler(CommandStartName);
        commandManager.RemoveHandler(CommandStopName);
        commandManager.RemoveHandler(CommandStatusName);

        chatGui.ChatMessage -= OnChatMessage;
        pluginInterface.UiBuilder.Draw -= DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUI;
        pluginInterface.UiBuilder.OpenMainUi -= OpenMainUI;

        // Unsubscribe from framework update
        if (pluginInterface.UiBuilder is { } uiBuilder)
        {
            uiBuilder.Draw -= ProcessMessageQueue;
        }

        // Dispose ECommons
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (args == "config")
            configWindow.IsOpen = true;
        else
            mainWindow.IsOpen = true;
    }

    private void OnStartCommand(string command, string args)
    {
        StartGame();
    }

    private void OnStopCommand(string command, string args)
    {
        StopGame();
    }

    private void OnStatusCommand(string command, string args)
    {
        var status = isGameActive ?
            $"Game is active. Rolling phase: {isRollingPhase}. Current rolls: {currentRolls.Count}" :
            "No game is currently active.";

        chatGui.Print($"[ToD] {status}");

        if (!string.IsNullOrEmpty(configuration.LastWinner))
            chatGui.Print($"[ToD] Last winner: {configuration.LastWinner}");
    }

    private void OnChatMessage(Dalamud.Game.Text.XivChatType type, int timestamp, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        var messageText = message.TextValue;

        if (!isGameActive || !isRollingPhase)
            return;

        // Check if this is a roll message - support both /random and /dice patterns
        Match rollMatch = null;
        string playerName = "";
        int rollValue = 0;
        bool isValidRoll = false;

        // Check for /random patterns if enabled
        if (configuration.EnableRandomDetection)
        {
            if (configuration.DebugMode)
            {
                // Debug pattern: "Random! PlayerName roll a 2 (out of 2)." or "Random! You roll a 1 (out of 2)."
                rollMatch = Regex.Match(messageText, @"Random! (.+) rolls? a (\d+) \(out of \d+\)\.");
            }
            else
            {
                // Normal pattern: "Random! PlayerName rolls a 123."
                rollMatch = Regex.Match(messageText, @"Random! (.+) rolls? a (\d+)\.");
            }
            
            if (rollMatch != null && rollMatch.Success)
            {
                playerName = rollMatch.Groups[1].Value;
                rollValue = int.Parse(rollMatch.Groups[2].Value);
                isValidRoll = true;
            }
        }

        // Check for /dice patterns if enabled
        if (!isValidRoll && configuration.EnableDiceDetection)
        {
            // Dice pattern examples:
            // "(Kirin Avenleigh) Random! 923"
            // "(Leonie Avenleigh) Random! 724"
            var diceMatch = Regex.Match(messageText, @"\((.+?)\) Random! (\d+)");
            if (diceMatch.Success)
            {
                playerName = diceMatch.Groups[1].Value;
                rollValue = int.Parse(diceMatch.Groups[2].Value);
                isValidRoll = true;
            }
        }

        if (!isValidRoll)
            return;

        // Normalize player name
        var normalizedName = NormalizePlayerName(playerName);

        // Handle "You" case
        if (normalizedName == "You")
        {
            if (!string.IsNullOrEmpty(configuration.LocalPlayerName))
            {
                normalizedName = configuration.LocalPlayerName;
            }
            else if (clientState.LocalPlayer != null)
            {
                normalizedName = clientState.LocalPlayer.Name.TextValue;
            }
        }

        // Only accept the first roll from each player
        if (!currentRolls.ContainsKey(normalizedName))
        {
            currentRolls[normalizedName] = rollValue;
            rollOrder[normalizedName] = rollCounter++; // Track roll order for tiebreaking
            pluginLog.Debug($"Roll recorded: {normalizedName} = {rollValue} (order: {rollOrder[normalizedName]})");
        }
    }

    private string NormalizePlayerName(string name)
    {
        // Remove world name if present
        foreach (var server in serverNames)
        {
            if (name.EndsWith($" {server}", StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - server.Length - 1);

            if (name.EndsWith(server, StringComparison.OrdinalIgnoreCase))
            {
                var index = name.LastIndexOf(server, StringComparison.OrdinalIgnoreCase);
                return name.Substring(0, index).Trim();
            }
        }

        return name;
    }

    private string ResolveTiebreaker(List<KeyValuePair<string, int>> tiedPlayers)
    {
        // Among tied players, return the one who rolled first (lowest roll order)
        var firstRoller = tiedPlayers.OrderBy(player => rollOrder.TryGetValue(player.Key, out int order) ? order : int.MaxValue).First();
        pluginLog.Debug($"Tiebreaker resolved: {tiedPlayers.Count} players tied at {firstRoller.Value}, winner: {firstRoller.Key} (rolled first)");
        return firstRoller.Key;
    }

    private string FindWinnerWithTiebreaker(List<KeyValuePair<string, int>> sortedRolls, string excludePlayer = "")
    {
        if (sortedRolls == null || sortedRolls.Count == 0)
            return "";
            
        // Group players by roll value (highest first due to sorting)
        var rollGroups = sortedRolls.GroupBy(kvp => kvp.Value).OrderByDescending(g => g.Key);
        
        foreach (var group in rollGroups)
        {
            var eligiblePlayers = group.Where(player => !string.IsNullOrEmpty(player.Key) && player.Key != excludePlayer).ToList();
            if (eligiblePlayers.Count == 0) continue;
            
            if (eligiblePlayers.Count == 1)
            {
                // Single player at this roll value, they win
                return eligiblePlayers[0].Key;
            }
            else
            {
                // Multiple players tied at this roll value, use tiebreaker
                return ResolveTiebreaker(eligiblePlayers);
            }
        }
        
        return ""; // No eligible winner found
    }

    private string FindWinnerWithTiebreaker(List<KeyValuePair<string, int>> sortedRolls, string excludePlayer1, string excludePlayer2)
    {
        if (sortedRolls == null || sortedRolls.Count == 0)
            return "";
            
        // Group players by roll value (highest first due to sorting)
        var rollGroups = sortedRolls.GroupBy(kvp => kvp.Value).OrderByDescending(g => g.Key);
        
        foreach (var group in rollGroups)
        {
            var eligiblePlayers = group.Where(player => !string.IsNullOrEmpty(player.Key) && player.Key != excludePlayer1 && player.Key != excludePlayer2).ToList();
            if (eligiblePlayers.Count == 0) continue;
            
            if (eligiblePlayers.Count == 1)
            {
                // Single player at this roll value, they win
                return eligiblePlayers[0].Key;
            }
            else
            {
                // Multiple players tied at this roll value, use tiebreaker
                return ResolveTiebreaker(eligiblePlayers);
            }
        }
        
        return ""; // No eligible winner found
    }
    
    private List<string> FindMultipleWinnersWithTiebreaker(List<KeyValuePair<string, int>> sortedRolls, List<string> excludePlayers, int count)
    {
        var winners = new List<string>();
        
        if (sortedRolls == null || sortedRolls.Count == 0 || count <= 0)
            return winners;
            
        // Group players by roll value (highest first due to sorting)
        var rollGroups = sortedRolls.GroupBy(kvp => kvp.Value).OrderByDescending(g => g.Key);
        
        foreach (var group in rollGroups)
        {
            if (winners.Count >= count) break;
            
            var eligiblePlayers = group.Where(player => 
                !string.IsNullOrEmpty(player.Key) && 
                !excludePlayers.Contains(player.Key) &&
                !winners.Contains(player.Key)
            ).ToList();
            
            if (eligiblePlayers.Count == 0) continue;
            
            // Sort by tiebreaker (who rolled first)
            var sortedByTiebreaker = eligiblePlayers.OrderBy(player => 
                rollOrder.TryGetValue(player.Key, out int order) ? order : int.MaxValue
            ).ToList();
            
            // Add as many winners as we need from this roll value
            int toAdd = Math.Min(count - winners.Count, sortedByTiebreaker.Count);
            for (int i = 0; i < toAdd; i++)
            {
                winners.Add(sortedByTiebreaker[i].Key);
            }
        }
        
        return winners;
    }

    public void StartGame()
    {
        if (isGameActive)
        {
            chatGui.PrintError("[ToD] A game is already in progress!");
            return;
        }

        // Update last winners from previous round at the START of new round
        if (currentRoundWinners.Count > 0)
        {
            configuration.LastWinners = new List<string>(currentRoundWinners);
            // Keep LastWinner for backwards compatibility
            configuration.LastWinner = currentRoundWinners.FirstOrDefault() ?? "";
            configuration.Save();
        }

        gameCancellation?.Cancel();
        gameCancellation = new CancellationTokenSource();

        isGameActive = true;
        isRollingPhase = false; // Start with rolling disabled until "Go!" is posted
        currentRolls.Clear();
        rollOrder.Clear(); // Clear roll order tracking
        rollCounter = 0; // Reset roll counter
        currentRoundWinners.Clear(); // Clear current round winners

        chatGui.Print("[ToD] Game started! Posting rules...");

        // Auto-post rules to shout if enabled
        if (configuration.AutoPostRules)
        {
            PostGameRules();
        }
        else
        {
            // If not auto-posting, start collecting rolls immediately
            isRollingPhase = true;
            chatGui.Print("[ToD] Collecting rolls...");
        }

        // Auto-close after timeout (only for manual mode without auto-posting)
        if (!configuration.AutoPostRules)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(configuration.RollTimeout * 1000, gameCancellation.Token);
                    
                    // Close rolling phase and process results
                    isRollingPhase = false;
                    ProcessResults();
                }
                catch (OperationCanceledException)
                {
                    // Game was cancelled
                }
            });
        }
    }

    public void StopGame()
    {
        if (!isGameActive)
        {
            chatGui.PrintError("[ToD] No game is currently active.");
            return;
        }

        gameCancellation?.Cancel();
        isGameActive = false;
        isRollingPhase = false;
        currentRoundWinners.Clear(); // Clear current round winners when stopped

        chatGui.Print("[ToD] Game stopped.");
    }

    private void ProcessResults()
    {
        if (currentRolls.Count < 2) // Hardcoded minimum
        {
            chatGui.PrintError($"[ToD] Not enough rolls received ({currentRolls.Count}/2). Game cancelled.");
            isGameActive = false;
            currentRoundWinners.Clear();
            return;
        }

        // Find winners using tiebreaker logic (skip last winners if possible)
        var sortedRolls = currentRolls.OrderByDescending(kvp => kvp.Value).ToList();
        currentRoundWinners.Clear();

        // Determine how many winners we need
        int winnersNeeded = Math.Min(configuration.NumberOfWinners, sortedRolls.Count);
        
        // Find winners, trying to exclude previous winners if possible
        var availableRolls = sortedRolls.ToList();
        for (int i = 0; i < winnersNeeded; i++)
        {
            // Try to find winner excluding last winners and already selected winners
            var excludeList = new List<string>();
            excludeList.AddRange(configuration.LastWinners);
            excludeList.AddRange(currentRoundWinners);
            
            string winner = FindMultipleWinnersWithTiebreaker(availableRolls, excludeList, 1).FirstOrDefault();
            
            // If no eligible winner found, try without excluding last winners
            if (string.IsNullOrEmpty(winner))
            {
                excludeList = new List<string>(currentRoundWinners);
                winner = FindMultipleWinnersWithTiebreaker(availableRolls, excludeList, 1).FirstOrDefault();
            }
            
            // If still no winner, just pick from remaining rolls
            if (string.IsNullOrEmpty(winner) && availableRolls.Count > currentRoundWinners.Count)
            {
                winner = availableRolls.Where(r => !currentRoundWinners.Contains(r.Key)).FirstOrDefault().Key;
            }
            
            if (!string.IsNullOrEmpty(winner))
            {
                currentRoundWinners.Add(winner);
            }
        }

        // Find strippers (100 or under)
        var stripList = currentRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
        var stripMessage = stripList.Count > 0 ? string.Join(", ", stripList) : "None";

        // Generate result message
        var statusChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.StatusChannel);
        
        // Auto-post results if enabled
        if (configuration.AutoPostResults)
        {
            QueueChatMessage($"{statusChannel} {configuration.Announcements.RollsClosed}");
            
            if (configuration.ChatChannels.UseWinnerSpecificChannels && currentRoundWinners.Count > 0)
            {
                // Output each winner to their specific channel
                for (int i = 0; i < currentRoundWinners.Count && i < 3; i++)
                {
                    var winner = currentRoundWinners[i];
                    int winnerRoll = currentRolls.TryGetValue(winner, out int roll) ? roll : 0;
                    
                    // Get the appropriate channel for this winner position
                    ChatChannelType winnerChannel = i switch
                    {
                        0 => configuration.ChatChannels.Winner1Channel,
                        1 => configuration.ChatChannels.Winner2Channel,
                        2 => configuration.ChatChannels.Winner3Channel,
                        _ => configuration.ChatChannels.ResultsChannel
                    };
                    
                    var channelCommand = configuration.ChatChannels.GetChannelCommand(winnerChannel);
                    
                    var winnerMessage = $"{channelCommand} {ProcessAnnouncementTemplate(configuration.Announcements.WinnerSpecificResult, winner, winnerRoll, i + 1, stripMessage)}";
                    QueueChatMessage(winnerMessage);
                }
            }
            else
            {
                // Use traditional single-channel output
                var resultsChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.ResultsChannel);
                
                if (currentRoundWinners.Count == 1)
                {
                    int winnerRoll = currentRolls.TryGetValue(currentRoundWinners[0], out int roll) ? roll : 0;
                    
                    var summaryMessage = $"{resultsChannel} {ProcessAnnouncementTemplate(configuration.Announcements.SingleWinnerResult, currentRoundWinners[0], winnerRoll, 1, stripMessage)}";
                    QueueChatMessage(summaryMessage);
                }
                else
                {
                    var winnerDetails = currentRoundWinners.Select(w => 
                        $"{w} ({(currentRolls.TryGetValue(w, out int r) ? r : 0)})"
                    );
                    
                    var summaryMessage = $"{resultsChannel} {ProcessAnnouncementTemplate(configuration.Announcements.MultipleWinnersResult, "", 0, 0, stripMessage, "", string.Join(", ", winnerDetails))}";
                    QueueChatMessage(summaryMessage);
                }
            }
        }

        isGameActive = false;
        pluginLog.Debug($"Game complete. Winners: {string.Join(", ", currentRoundWinners)}. Strip list: {stripMessage}");
    }

    public void ClearLastWinner()
    {
        configuration.LastWinner = "";
        configuration.LastWinners.Clear();
        configuration.Save();
    }

    public void PassWinnerToNext(string? winnerToPas = null)
    {
        if (currentRoundWinners.Count == 0 || currentRolls.Count < 2)
            return;

        // If no specific winner specified, use the first one (for single winner scenario)
        if (string.IsNullOrEmpty(winnerToPas))
        {
            if (currentRoundWinners.Count == 1)
            {
                winnerToPas = currentRoundWinners[0];
            }
            else
            {
                // Multiple winners exist but none specified - this should be handled by UI
                pluginLog.Warning("PassWinnerToNext called with multiple winners but no specific winner specified");
                return;
            }
        }

        // Make sure the winner to pass is actually a current winner
        if (!currentRoundWinners.Contains(winnerToPas))
            return;

        // Find next eligible winner using tiebreaker logic
        var sortedRolls = currentRolls.OrderByDescending(kvp => kvp.Value).ToList();
        
        // Build exclude list: all current winners + last round's winners
        var excludeList = new List<string>();
        excludeList.AddRange(currentRoundWinners);
        excludeList.AddRange(configuration.LastWinners);
        
        // Try to find a replacement winner
        var replacement = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, 1).FirstOrDefault();
        
        // If no eligible winner found, try without excluding last winners
        if (string.IsNullOrEmpty(replacement))
        {
            excludeList = new List<string>(currentRoundWinners);
            replacement = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, 1).FirstOrDefault();
        }
        
        if (!string.IsNullOrEmpty(replacement))
        {
            // Update last winners to include the passed winner
            if (!configuration.LastWinners.Contains(winnerToPas))
            {
                configuration.LastWinners.Add(winnerToPas);
            }
            configuration.LastWinner = winnerToPas;
            configuration.Save();

            // Replace the passed winner with the new winner
            int indexToReplace = currentRoundWinners.IndexOf(winnerToPas);
            currentRoundWinners[indexToReplace] = replacement;

            // Find strippers for new announcement
            var stripList = currentRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
            var stripMessage = stripList.Count > 0 ? string.Join(", ", stripList) : "None";

            // Generate new winner message
            
            // Auto-post new winner if enabled
            if (configuration.AutoPostResults)
            {
                if (configuration.ChatChannels.UseWinnerSpecificChannels && currentRoundWinners.Count > 0)
                {
                    // Output each winner to their specific channel
                    for (int i = 0; i < currentRoundWinners.Count && i < 3; i++)
                    {
                        var winner = currentRoundWinners[i];
                        int winnerRoll = currentRolls.TryGetValue(winner, out int roll) ? roll : 0;
                        
                        // Get the appropriate channel for this winner position
                        ChatChannelType winnerChannel = i switch
                        {
                            0 => configuration.ChatChannels.Winner1Channel,
                            1 => configuration.ChatChannels.Winner2Channel,
                            2 => configuration.ChatChannels.Winner3Channel,
                            _ => configuration.ChatChannels.ResultsChannel
                        };
                        
                        var channelCommand = configuration.ChatChannels.GetChannelCommand(winnerChannel);
                        
                        // Use appropriate template based on whether this winner was passed
                        if (winner == replacement)
                        {
                            // This is the new winner who received the pass
                            var winnerMessage = $"{channelCommand} {ProcessAnnouncementTemplate(configuration.Announcements.PassedWinnerResult, winner, winnerRoll, i + 1, stripMessage, winnerToPas)}";
                            QueueChatMessage(winnerMessage);
                        }
                        else
                        {
                            // Regular winner, use normal template
                            var winnerMessage = $"{channelCommand} {ProcessAnnouncementTemplate(configuration.Announcements.WinnerSpecificResult, winner, winnerRoll, i + 1, stripMessage)}";
                            QueueChatMessage(winnerMessage);
                        }
                    }
                }
                else
                {
                    // Use traditional single-channel output
                    var resultsChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.ResultsChannel);
                    
                    if (currentRoundWinners.Count == 1)
                    {
                        int winnerRoll = currentRolls.TryGetValue(replacement, out int roll) ? roll : 0;
                        
                        var summaryMessage = $"{resultsChannel} {ProcessAnnouncementTemplate(configuration.Announcements.PassedWinnerResult, replacement, winnerRoll, 1, stripMessage, winnerToPas)}";
                        QueueChatMessage(summaryMessage);
                    }
                    else
                    {
                        var winnerDetails = currentRoundWinners.Select(w => 
                            $"{w} ({(currentRolls.TryGetValue(w, out int r) ? r : 0)})"
                        );
                        
                        var summaryMessage = $"{resultsChannel} {ProcessAnnouncementTemplate(configuration.Announcements.MultipleWinnersResult, "", 0, 0, stripMessage, "", string.Join(", ", winnerDetails))}";
                        QueueChatMessage(summaryMessage);
                    }
                }
            }
            
            pluginLog.Debug($"Passed winner {winnerToPas} to {replacement}");
        }
    }
    
    public List<string> GetCurrentRoundWinners() => new List<string>(currentRoundWinners);

    public bool CanPass()
    {
        if (currentRoundWinners.Count == 0 || isGameActive || currentRolls.Count < 2)
            return false;

        // Check if there's at least one other player besides current winners
        var eligibleCount = currentRolls.Count(kvp => !currentRoundWinners.Contains(kvp.Key));
        return eligibleCount > 0;
    }

    public IReadOnlyDictionary<string, int> GetCurrentRolls() => currentRolls;
    public bool IsGameActive => isGameActive;
    public bool IsRollingPhase => isRollingPhase;
    public string GetCurrentRoundWinner() => currentRoundWinners.FirstOrDefault() ?? ""; // Returns first winner for backwards compatibility

    private string ProcessAnnouncementTemplate(string template, string winnerName = "", int winnerRoll = 0, int winnerNumber = 0, string stripMessage = "", string passedFrom = "", string winnersList = "")
    {
        var rollInstructions = "High roll ";
        if (configuration.EnableRandomDetection && configuration.EnableDiceDetection)
            rollInstructions += "(/random or /dice)";
        else if (configuration.EnableRandomDetection)
            rollInstructions += "(/random)";
        else if (configuration.EnableDiceDetection)
            rollInstructions += "(/dice)";
        rollInstructions += " chooses someone this round.";
        
        string multiWinnerText = configuration.NumberOfWinners > 1 ? $" Top {configuration.NumberOfWinners} players win!" : "";
        string noRepeatText = configuration.NumberOfWinners > 1 ? "Winners cannot win two rounds in a row." : "You cannot win two rounds in a row.";
        var resultsChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.ResultsChannel);
        
        return template
            .Replace("{ROLL_INSTRUCTIONS}", rollInstructions + multiWinnerText)
            .Replace("{NO_REPEAT_TEXT}", noRepeatText)
            .Replace("{RESULTS_CHANNEL}", resultsChannel)
            .Replace("{CUSTOM_WIFI_MESSAGE}", configuration.CustomWiFiMessage)
            .Replace("{WINNER_NAME}", winnerName)
            .Replace("{WINNER_ROLL}", winnerRoll.ToString())
            .Replace("{WINNER_NUMBER}", winnerNumber.ToString())
            .Replace("{WINNERS_LIST}", winnersList)
            .Replace("{STRIPPERS}", stripMessage)
            .Replace("{PASSED_FROM}", passedFrom);
    }

    private void ProcessMessageQueue()
    {
        // Process message queue with 2-second delays
        if (messageQueue.Count > 0)
        {
            var timeSinceLastMessage = DateTime.Now - lastMessageSent;
            if (timeSinceLastMessage.TotalMilliseconds >= 2000)
            {
                ProcessNextQueuedMessage();
            }
        }
    }

    private void ProcessNextQueuedMessage()
    {
        if (messageQueue.Count == 0) return;
        
        var message = messageQueue.Dequeue();
        PostToChat(message);
        lastMessageSent = DateTime.Now;
        
        // Check if this was the "Go!" message to start collecting rolls
        var countdownChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.CountdownChannel);
        if (message == $"{countdownChannel} Go!" && waitingForGoMessage)
        {
            isRollingPhase = true;
            waitingForGoMessage = false;
            chatGui.Print("[ToD] Go! Now collecting rolls...");
            
            // NOW start the 17-second timer for roll collection
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(configuration.RollTimeout * 1000, gameCancellation.Token);

                    // Start closing countdown if auto-posting is enabled
                    if (configuration.AutoPostRules)
                    {
                        var countdownChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.CountdownChannel);
                        QueueChatMessage($"{countdownChannel} {configuration.Announcements.ClosingCountdown1}");
                        QueueChatMessage($"{countdownChannel} {configuration.Announcements.ClosingCountdown2}");
                        QueueChatMessage($"{countdownChannel} {configuration.Announcements.ClosingCountdown3}");

                        // Wait for countdown messages to be sent (3 messages * 2 seconds each = 6 seconds)
                        await Task.Delay(6000, gameCancellation.Token);
                    }

                    // Close rolling phase and process results
                    isRollingPhase = false;
                    ProcessResults();
                }
                catch (OperationCanceledException)
                {
                    // Game was cancelled
                }
            });
        }
    }

    private void QueueChatMessage(string message)
    {
        messageQueue.Enqueue(message);
    }

    private void PostGameRules()
    {
        // Set flag to enable rolling when "Go!" is posted
        waitingForGoMessage = true;
        
        var rulesChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.RulesChannel);
        var countdownChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.CountdownChannel);
        var resultsChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.ResultsChannel);
        
        // Build roll instructions based on enabled detection
        var rollInstructions = "High roll ";
        if (configuration.EnableRandomDetection && configuration.EnableDiceDetection)
            rollInstructions += "(/random or /dice)";
        else if (configuration.EnableRandomDetection)
            rollInstructions += "(/random)";
        else if (configuration.EnableDiceDetection)
            rollInstructions += "(/dice)";
        rollInstructions += " chooses someone this round.";
        
        // Add multi-winner text if needed
        if (configuration.NumberOfWinners > 1)
            rollInstructions += $" Top {configuration.NumberOfWinners} players win!";
        
        // Queue the complete macro sequence using templates
        QueueChatMessage($"{rulesChannel} {ProcessAnnouncementTemplate(configuration.Announcements.RulesLine1)}");
        QueueChatMessage($"{rulesChannel} {ProcessAnnouncementTemplate(configuration.Announcements.RulesLine2)}");
        QueueChatMessage($"{rulesChannel} {ProcessAnnouncementTemplate(configuration.Announcements.RulesLine3)}");
        QueueChatMessage($"{rulesChannel} {ProcessAnnouncementTemplate(configuration.Announcements.RulesLine4)}");
        QueueChatMessage($"{rulesChannel} {ProcessAnnouncementTemplate(configuration.Announcements.RulesLine5)}");
        QueueChatMessage($"{countdownChannel} {configuration.Announcements.CountdownStart}");
        QueueChatMessage($"{countdownChannel} {configuration.Announcements.CountdownMiddle}");
        QueueChatMessage($"{countdownChannel} {configuration.Announcements.CountdownEnd}");
        QueueChatMessage($"{countdownChannel} {configuration.Announcements.CountdownGo}");
    }

    public void PostToChat(string message)
    {
        try
        {
            Chat.SendMessage(message);
        }
        catch (Exception ex)
        {
            pluginLog.Error($"Failed to post to chat: {ex.Message}");
            chatGui.PrintError($"[ToD] Failed to post to chat: {ex.Message}");
        }
    }

    public void OpenConfigWindow()
    {
        // Settings are now integrated into the main window as a tab
        mainWindow.IsOpen = true;
    }

    private void DrawUI() => WindowSystem.Draw();
    private void OpenConfigUI() => configWindow.IsOpen = true;
    private void OpenMainUI() => mainWindow.IsOpen = true;
}
