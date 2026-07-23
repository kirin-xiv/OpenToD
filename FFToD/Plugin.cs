using Dalamud.Game.Chat;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
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
    public IChatGui ChatGui => chatGui;
    private readonly IClientState clientState;
    private readonly IPluginLog pluginLog;

    public readonly WindowSystem WindowSystem = new("FFToD");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private Configuration configuration;

    private bool isGameActive = false;
    private bool isRollingPhase = false;
    private readonly Dictionary<string, int> currentRolls = new();
    private readonly Dictionary<string, int> rollOrder = new();
    private readonly Dictionary<string, (int roll, BonusPrize prize)> bonusPrizeHits = new();
    private readonly List<string> autoSkippedThisRound = new();
    private int rollCounter = 0;
    private CancellationTokenSource? gameCancellation;

    private List<string> currentRoundWinners = new List<string>();
    private bool isJackpotRound = false;
    

    private readonly Queue<string> messageQueue = new();
    private DateTime lastMessageSent = DateTime.MinValue;
    private bool waitingForGoMessage = false;
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

        if (pluginInterface.UiBuilder is { } uiBuilder)
        {
            uiBuilder.Draw -= ProcessMessageQueue;
        }


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
        if (!IsUserAuthorized())
        {
            chatGui.PrintError("[ToD] You must be logged in to start a game. Open the plugin window and click 'Log In'.");
            return;
        }
        StartGame();
    }

    private void OnStopCommand(string command, string args)
    {
        if (!IsUserAuthorized())
        {
            chatGui.PrintError("[ToD] You must be logged in to stop a game. Open the plugin window and click 'Log In'.");
            return;
        }
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

    private void OnChatMessage(IHandleableChatMessage message)
    {
        var messageText = message.Message.TextValue;

        if (!isGameActive || !isRollingPhase)
            return;

        // Check if this is a roll message - support both /random and /dice patterns
        Match rollMatch = null;
        string playerName = "";
        int rollValue = 0;
        bool isValidRoll = false;

        if (configuration.EnableRandomDetection)
        {
            if (configuration.DebugMode)
            {
                rollMatch = Regex.Match(messageText, @"Random! (.+) rolls? a (\d+) \(out of \d+\)\.");
            }
            else
            {
                rollMatch = Regex.Match(messageText, @"Random! (.+) rolls? a (\d+)\.");
            }
            
            if (rollMatch != null && rollMatch.Success)
            {
                playerName = rollMatch.Groups[1].Value;
                rollValue = int.Parse(rollMatch.Groups[2].Value);
                isValidRoll = true;
            }
        }

        if (!isValidRoll && configuration.EnableDiceDetection)
        {
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
            else if (Svc.Objects?.LocalPlayer != null)
            {
                normalizedName = Svc.Objects.LocalPlayer.Name.TextValue;
            }
        }

        if (!currentRolls.ContainsKey(normalizedName))
        {
            currentRolls[normalizedName] = rollValue;
            rollOrder[normalizedName] = rollCounter++;
            pluginLog.Debug($"Roll recorded: {normalizedName} = {rollValue} (order: {rollOrder[normalizedName]})");
            
            // Check for bonus prize hits
            if (configuration.EnableBonusPrizes)
            {
                var matchedPrize = configuration.BonusPrizes.FirstOrDefault(bp => bp.Number == rollValue);
                if (matchedPrize != null)
                {
                    bonusPrizeHits[normalizedName] = (rollValue, matchedPrize);
                    pluginLog.Debug($"Bonus prize hit: {normalizedName} rolled {rollValue}");
                }
            }
        }
    }

    private string NormalizePlayerName(string name)
    {

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

        var firstRoller = tiedPlayers.OrderBy(player => rollOrder.TryGetValue(player.Key, out int order) ? order : int.MaxValue).First();
        pluginLog.Debug($"Tiebreaker resolved: {tiedPlayers.Count} players tied at {firstRoller.Value}, winner: {firstRoller.Key} (rolled first)");
        return firstRoller.Key;
    }

    private string CheckForJackpotWinner()
    {
        if (!configuration.EnableJackpot)
            return "";
            

        var jackpotHits = currentRolls.Where(kvp => kvp.Value == configuration.JackpotValue).ToList();
        
        if (jackpotHits.Count == 0)
            return "";
            
        if (jackpotHits.Count == 1)
            return jackpotHits[0].Key;
            

        return ResolveTiebreaker(jackpotHits);
    }

    private string FindWinnerWithTiebreaker(List<KeyValuePair<string, int>> sortedRolls, string excludePlayer = "")
    {
        if (sortedRolls == null || sortedRolls.Count == 0)
            return "";
            

        var rollGroups = sortedRolls.GroupBy(kvp => kvp.Value).OrderByDescending(g => g.Key);
        
        foreach (var group in rollGroups)
        {
            var eligiblePlayers = group.Where(player => !string.IsNullOrEmpty(player.Key) && player.Key != excludePlayer).ToList();
            if (eligiblePlayers.Count == 0) continue;
            
            if (eligiblePlayers.Count == 1)
            {

                return eligiblePlayers[0].Key;
            }
            else
            {

                return ResolveTiebreaker(eligiblePlayers);
            }
        }
        
        return "";
    }

    private string FindWinnerWithTiebreaker(List<KeyValuePair<string, int>> sortedRolls, string excludePlayer1, string excludePlayer2)
    {
        if (sortedRolls == null || sortedRolls.Count == 0)
            return "";
            

        var rollGroups = sortedRolls.GroupBy(kvp => kvp.Value).OrderByDescending(g => g.Key);
        
        foreach (var group in rollGroups)
        {
            var eligiblePlayers = group.Where(player => !string.IsNullOrEmpty(player.Key) && player.Key != excludePlayer1 && player.Key != excludePlayer2).ToList();
            if (eligiblePlayers.Count == 0) continue;
            
            if (eligiblePlayers.Count == 1)
            {

                return eligiblePlayers[0].Key;
            }
            else
            {

                return ResolveTiebreaker(eligiblePlayers);
            }
        }
        
        return "";
    }
    
    private List<string> FindMultipleWinnersWithTiebreaker(List<KeyValuePair<string, int>> sortedRolls, List<string> excludePlayers, int count, bool ascending = false)
    {
        var winners = new List<string>();
        
        if (sortedRolls == null || sortedRolls.Count == 0 || count <= 0)
            return winners;
            
        var rollGroups = sortedRolls.GroupBy(kvp => kvp.Value);
        rollGroups = ascending ? rollGroups.OrderBy(g => g.Key) : rollGroups.OrderByDescending(g => g.Key);
        
        foreach (var group in rollGroups)
        {
            if (winners.Count >= count) break;
            
            var eligiblePlayers = group.Where(player => 
                !string.IsNullOrEmpty(player.Key) && 
                !excludePlayers.Contains(player.Key) &&
                !winners.Contains(player.Key)
            ).ToList();
            
            if (eligiblePlayers.Count == 0) continue;
            

            var sortedByTiebreaker = eligiblePlayers.OrderBy(player => 
                rollOrder.TryGetValue(player.Key, out int order) ? order : int.MaxValue
            ).ToList();
            

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
        bonusPrizeHits.Clear(); // Clear bonus prize hits
        autoSkippedThisRound.Clear(); // Clear auto-skip tracking
        currentRoundWinners.Clear(); // Clear current round winners
        isJackpotRound = false; // Reset jackpot flag

        chatGui.Print("[ToD] Game started! Posting rules...");

        // Auto-post rules to shout if enabled
        if (configuration.AutoPostRules)
        {
            PostGameRules();
        }
        else
        {

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
                    

                    isRollingPhase = false;
                    ProcessResults();
                }
                catch (OperationCanceledException)
                {

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

        // Check for jackpot winner first - this supersedes all normal logic
        string jackpotWinner = CheckForJackpotWinner();
        

        var stripList = currentRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
        var stripMessage = stripList.Count > 0 ? string.Join(", ", stripList) : "None";
        
        var statusChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.StatusChannel);
        
        if (!string.IsNullOrEmpty(jackpotWinner))
        {

            currentRoundWinners.Clear();
            currentRoundWinners.Add(jackpotWinner);
            isJackpotRound = true;
            

            if (configuration.AutoPostResults)
            {
                QueueChatMessage($"{statusChannel} {configuration.Announcements.RollsClosed}");
                
                var jackpotChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.JackpotChannel);
                int jackpotRoll = currentRolls.TryGetValue(jackpotWinner, out int roll) ? roll : 0;
                
                var jackpotMessage = $"{jackpotChannel} {ProcessAnnouncementTemplate(configuration.Announcements.JackpotWinnerResult, jackpotWinner, jackpotRoll, 1, stripMessage)}";
                QueueChatMessage(jackpotMessage);
            }
        }
        else
        {
            isJackpotRound = false;
            var sortedRolls = currentRolls.OrderByDescending(kvp => kvp.Value).ToList();
            currentRoundWinners.Clear();

            int winnersNeeded = Math.Min(configuration.NumberOfWinners, Math.Min(2, sortedRolls.Count));
            
            if (winnersNeeded == 1)
            {
                var excludeList = new List<string>(configuration.LastWinners);
                excludeList.AddRange(configuration.AutoSkipPlayers);

                switch (configuration.WinnerMode)
                {
                    case WinnerSelectionMode.BottomLowest:
                        var sortedAscending = currentRolls.OrderBy(kvp => kvp.Value).ToList();
                        string winner = FindMultipleWinnersWithTiebreaker(sortedAscending, excludeList, 1, true).FirstOrDefault();
                        if (string.IsNullOrEmpty(winner))
                            winner = FindMultipleWinnersWithTiebreaker(sortedAscending, new List<string>(), 1, true).FirstOrDefault();
                        if (!string.IsNullOrEmpty(winner))
                            currentRoundWinners.Add(winner);
                        break;

                    case WinnerSelectionMode.Middle:
                        var rollsByValue = currentRolls.OrderBy(kvp => kvp.Value).ToList();
                        var eligibleRolls = rollsByValue.Where(kvp => !excludeList.Contains(kvp.Key)).ToList();
                        if (eligibleRolls.Count == 0)
                            eligibleRolls = rollsByValue;
                        
                        var middleIndex = eligibleRolls.Count / 2;
                        if (middleIndex < eligibleRolls.Count)
                            currentRoundWinners.Add(eligibleRolls[middleIndex].Key);
                        break;

                    case WinnerSelectionMode.Random:
                        var eligiblePlayers = currentRolls.Keys.Where(k => !excludeList.Contains(k)).ToList();
                        if (eligiblePlayers.Count == 0)
                            eligiblePlayers = currentRolls.Keys.ToList();
                        var random = new Random();
                        var randomWinner = eligiblePlayers[random.Next(eligiblePlayers.Count)];
                        currentRoundWinners.Add(randomWinner);
                        break;

                    case WinnerSelectionMode.HighestAsksLowest:
                        // For single winner count, still pick the pair — highest asks lowest
                        var highestAsk = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, 1).FirstOrDefault();
                        if (string.IsNullOrEmpty(highestAsk))
                            highestAsk = FindMultipleWinnersWithTiebreaker(sortedRolls, new List<string>(), 1).FirstOrDefault();
                        if (!string.IsNullOrEmpty(highestAsk))
                        {
                            currentRoundWinners.Add(highestAsk);
                            var sortedAsc = currentRolls.OrderBy(kvp => kvp.Value).ToList();
                            var lowExclude = new List<string> { highestAsk };
                            var lowestAsk = FindMultipleWinnersWithTiebreaker(sortedAsc, lowExclude, 1, true).FirstOrDefault();
                            if (!string.IsNullOrEmpty(lowestAsk))
                                currentRoundWinners.Add(lowestAsk);
                        }
                        break;

                    case WinnerSelectionMode.HighestAndLowest:
                    default:
                        string defaultWinner = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, 1).FirstOrDefault();
                        if (string.IsNullOrEmpty(defaultWinner))
                            defaultWinner = FindMultipleWinnersWithTiebreaker(sortedRolls, new List<string>(), 1).FirstOrDefault();
                        if (!string.IsNullOrEmpty(defaultWinner))
                            currentRoundWinners.Add(defaultWinner);
                        break;
                }
            }
            else if (winnersNeeded >= 2)
            {
                var excludeList = new List<string>(configuration.LastWinners);
                excludeList.AddRange(configuration.AutoSkipPlayers);

                switch (configuration.WinnerMode)
                {
                    case WinnerSelectionMode.TopHighest:
                        var topWinners = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, winnersNeeded);
                        if (topWinners.Count < winnersNeeded)
                            topWinners = FindMultipleWinnersWithTiebreaker(sortedRolls, new List<string>(), winnersNeeded);
                        currentRoundWinners.AddRange(topWinners);
                        break;

                    case WinnerSelectionMode.HighestAndLowest:
                        string highestWinner = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, 1).FirstOrDefault();
                        if (string.IsNullOrEmpty(highestWinner))
                            highestWinner = FindMultipleWinnersWithTiebreaker(sortedRolls, new List<string>(), 1).FirstOrDefault();

                        if (!string.IsNullOrEmpty(highestWinner))
                        {
                            currentRoundWinners.Add(highestWinner);

                            var sortedAscending = currentRolls.OrderBy(kvp => kvp.Value).ToList();
                            excludeList.Add(highestWinner);
                            string lowestWinner = FindMultipleWinnersWithTiebreaker(sortedAscending, excludeList, 1, true).FirstOrDefault();
                            if (string.IsNullOrEmpty(lowestWinner))
                            {
                                excludeList = new List<string> { highestWinner };
                                lowestWinner = FindMultipleWinnersWithTiebreaker(sortedAscending, excludeList, 1, true).FirstOrDefault();
                            }

                            if (!string.IsNullOrEmpty(lowestWinner))
                                currentRoundWinners.Add(lowestWinner);

                            if (winnersNeeded > 2 && currentRoundWinners.Count < winnersNeeded)
                            {
                                excludeList = new List<string>(configuration.LastWinners);
                                excludeList.AddRange(currentRoundWinners);
                                var nextWinner = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, 1).FirstOrDefault();
                                if (!string.IsNullOrEmpty(nextWinner))
                                    currentRoundWinners.Add(nextWinner);
                            }
                        }
                        break;

                    case WinnerSelectionMode.HighestAsksLowest:
                        string highestAsker = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, 1).FirstOrDefault();
                        if (string.IsNullOrEmpty(highestAsker))
                            highestAsker = FindMultipleWinnersWithTiebreaker(sortedRolls, new List<string>(), 1).FirstOrDefault();

                        if (!string.IsNullOrEmpty(highestAsker))
                        {
                            currentRoundWinners.Add(highestAsker);

                            var sortedAscendingForAsk = currentRolls.OrderBy(kvp => kvp.Value).ToList();
                            var askExcludeList = new List<string> { highestAsker };
                            string lowestDoer = FindMultipleWinnersWithTiebreaker(sortedAscendingForAsk, askExcludeList, 1, true).FirstOrDefault();
                            if (string.IsNullOrEmpty(lowestDoer))
                            {
                                askExcludeList = new List<string> { highestAsker };
                                lowestDoer = FindMultipleWinnersWithTiebreaker(sortedAscendingForAsk, askExcludeList, 1, true).FirstOrDefault();
                            }

                            if (!string.IsNullOrEmpty(lowestDoer))
                                currentRoundWinners.Add(lowestDoer);
                        }
                        break;

                    case WinnerSelectionMode.BottomLowest:
                        var sortedLowest = currentRolls.OrderBy(kvp => kvp.Value).ToList();
                        var bottomWinners = FindMultipleWinnersWithTiebreaker(sortedLowest, excludeList, winnersNeeded, true);
                        if (bottomWinners.Count < winnersNeeded)
                            bottomWinners = FindMultipleWinnersWithTiebreaker(sortedLowest, new List<string>(), winnersNeeded, true);
                        currentRoundWinners.AddRange(bottomWinners);
                        break;

                    case WinnerSelectionMode.Random:
                        var eligiblePlayers = currentRolls.Keys.Where(k => !excludeList.Contains(k)).ToList();
                        if (eligiblePlayers.Count < winnersNeeded)
                            eligiblePlayers = currentRolls.Keys.ToList();

                        var random = new Random();
                        var shuffled = eligiblePlayers.OrderBy(x => random.Next()).Take(winnersNeeded).ToList();
                        currentRoundWinners.AddRange(shuffled);
                        break;

                    case WinnerSelectionMode.Middle:
                        var rollsByValue = currentRolls.OrderBy(kvp => kvp.Value).ToList();
                        
                        var eligibleRolls = rollsByValue.Where(kvp => !excludeList.Contains(kvp.Key)).ToList();
                        if (eligibleRolls.Count == 0)
                            eligibleRolls = rollsByValue;
                            
                        var middleIndex = eligibleRolls.Count / 2;
                        if (winnersNeeded == 1 || eligibleRolls.Count <= 2)
                        {
                            if (middleIndex < eligibleRolls.Count)
                                currentRoundWinners.Add(eligibleRolls[middleIndex].Key);
                        }
                        else
                        {
                            int startIndex = Math.Max(0, middleIndex - (winnersNeeded / 2));
                            for (int i = 0; i < winnersNeeded && (startIndex + i) < eligibleRolls.Count; i++)
                            {
                                currentRoundWinners.Add(eligibleRolls[startIndex + i].Key);
                            }
                        }
                        break;
                }
            }

            if (configuration.AutoPostResults)
            {
                QueueChatMessage($"{statusChannel} {configuration.Announcements.RollsClosed}");
                
                // Auto-skip blast: after "Rolls Closed", before winner results
                if (configuration.AutoSkipBlast && configuration.AutoSkipPlayers.Count > 0)
                {
                    var skippedRollers = currentRolls
                        .Where(kvp => configuration.AutoSkipPlayers.Any(s => s.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)))
                        .Select(kvp => $"{kvp.Key} ({kvp.Value})")
                        .ToList();
                    
                    if (skippedRollers.Count > 0)
                    {
                        var resultsChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.ResultsChannel);
                        var blastMsg = ProcessAnnouncementTemplate(configuration.Announcements.AutoSkipBlastResult, "", 0, 0, "", "", string.Join(", ", skippedRollers));
                        QueueChatMessage($"{resultsChannel} {blastMsg}");
                    }
                }
                
                if (configuration.ChatChannels.UseWinnerSpecificChannels && currentRoundWinners.Count > 0)
                {

                    for (int i = 0; i < currentRoundWinners.Count && i < 2; i++)
                    {
                        var winner = currentRoundWinners[i];
                        int winnerRoll = currentRolls.TryGetValue(winner, out int roll) ? roll : 0;
                        

                        ChatChannelType winnerChannel = i switch
                        {
                            0 => configuration.ChatChannels.Winner1Channel,
                            1 => configuration.ChatChannels.Winner2Channel,
                            _ => configuration.ChatChannels.ResultsChannel
                        };
                        
                        var channelCommand = configuration.ChatChannels.GetChannelCommand(winnerChannel);
                        
                        var winnerMessage = $"{channelCommand} {ProcessAnnouncementTemplate(configuration.Announcements.WinnerSpecificResult, winner, winnerRoll, i + 1, stripMessage)}";
                        QueueChatMessage(winnerMessage);
                    }
                }
                else
                {

                    var resultsChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.ResultsChannel);
                    
                    if (configuration.WinnerMode == WinnerSelectionMode.HighestAsksLowest && currentRoundWinners.Count >= 2)
                    {
                        // Highest asks Lowest: first winner is highest (asker), second is lowest (doer)
                        int askerRoll = currentRolls.TryGetValue(currentRoundWinners[0], out int aRoll) ? aRoll : 0;
                        int doerRoll = currentRolls.TryGetValue(currentRoundWinners[1], out int dRoll) ? dRoll : 0;
                        
                        // Check for auto-skips
                        var askerSkip = autoSkippedThisRound.FirstOrDefault(s => s.EndsWith($" -> {currentRoundWinners[0]}"));
                        var doerSkip = autoSkippedThisRound.FirstOrDefault(s => s.EndsWith($" -> {currentRoundWinners[1]}"));
                        var passedFrom = "";
                        if (!string.IsNullOrEmpty(askerSkip))
                            passedFrom = $" (passed from {askerSkip.Split(" -> ")[0]})";
                        else if (!string.IsNullOrEmpty(doerSkip))
                            passedFrom = $" (passed from {doerSkip.Split(" -> ")[0]})";
                        
                        var summaryMessage = $"{resultsChannel} {ProcessAnnouncementTemplate(configuration.Announcements.HighestAsksLowestResult, currentRoundWinners[0], askerRoll, 1, stripMessage, passedFrom, "", currentRoundWinners[1], doerRoll)}";
                        QueueChatMessage(summaryMessage);
                    }
                    else if (currentRoundWinners.Count == 1)
                    {
                        int winnerRoll = currentRolls.TryGetValue(currentRoundWinners[0], out int roll) ? roll : 0;
                        
                        // Check if this winner was auto-skipped
                        var skipEntry = autoSkippedThisRound.FirstOrDefault(s => s.EndsWith($" -> {currentRoundWinners[0]}"));
                        if (!string.IsNullOrEmpty(skipEntry))
                        {
                            var passedFrom = skipEntry.Split(" -> ")[0];
                            var summaryMessage = $"{resultsChannel} {ProcessAnnouncementTemplate(configuration.Announcements.PassedWinnerResult, currentRoundWinners[0], winnerRoll, 1, stripMessage, passedFrom)}";
                            QueueChatMessage(summaryMessage);
                        }
                        else
                        {
                            var summaryMessage = $"{resultsChannel} {ProcessAnnouncementTemplate(configuration.Announcements.SingleWinnerResult, currentRoundWinners[0], winnerRoll, 1, stripMessage)}";
                            QueueChatMessage(summaryMessage);
                        }
                    }
                    else
                    {
                        // Check for auto-skips in multiple winners
                        var winnerDetails = currentRoundWinners.Select(w =>
                        {
                            var skipEntry = autoSkippedThisRound.FirstOrDefault(s => s.EndsWith($" -> {w}"));
                            if (!string.IsNullOrEmpty(skipEntry))
                            {
                                var passedFrom = skipEntry.Split(" -> ")[0];
                                return $"{w} ({(currentRolls.TryGetValue(w, out int r) ? r : 0)}, passed from {passedFrom})";
                            }
                            return $"{w} ({(currentRolls.TryGetValue(w, out int r2) ? r2 : 0)})";
                        });
                        
                        var summaryMessage = $"{resultsChannel} {ProcessAnnouncementTemplate(configuration.Announcements.MultipleWinnersResult, "", 0, 0, stripMessage, "", string.Join(", ", winnerDetails))}";
                        QueueChatMessage(summaryMessage);
                    }
                }
            }
        }

        // Post bonus prize summary announcement (does not affect round winners)
        if (configuration.EnableBonusPrizes && bonusPrizeHits.Count > 0 && configuration.AutoPostResults)
        {
            var bonusChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.BonusPrizesChannel);
            
            // Build winners list: "PlayerA (420): 100k gil, PlayerB (911): Fat Cat"
            var winnerEntries = bonusPrizeHits.Select(hit =>
            {
                var (roll, prize) = hit.Value;
                var label = !string.IsNullOrWhiteSpace(prize.Prize) ? $": {prize.Prize}" : "";
                return $"{hit.Key} ({roll}){label}";
            });
            var winnersList = string.Join(", ", winnerEntries);
            
            var bonusMessage = $"{bonusChannel} {ProcessAnnouncementTemplate(configuration.Announcements.BonusPrizeResult, "", 0, 0, "", "", winnersList)}";
            QueueChatMessage(bonusMessage);
        }

        isGameActive = false;
        
        configuration.Statistics.IncrementRounds();
        configuration.Save();
        
        pluginLog.Debug($"Game complete. Winners: {string.Join(", ", currentRoundWinners)}. Strip list: {stripMessage}. Bonus hits: {bonusPrizeHits.Count}");
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
            

        if (isJackpotRound)
        {
            pluginLog.Warning("Attempted to pass a jackpot winner - this is not allowed");
            return;
        }

        // If no specific winner specified, use the first one (for single winner scenario)
        if (string.IsNullOrEmpty(winnerToPas))
        {
            if (currentRoundWinners.Count == 1)
            {
                winnerToPas = currentRoundWinners[0];
            }
            else
            {

                pluginLog.Warning("PassWinnerToNext called with multiple winners but no specific winner specified");
                return;
            }
        }

        // Make sure the winner to pass is actually a current winner
        if (!currentRoundWinners.Contains(winnerToPas))
            return;

        // Find next eligible winner using tiebreaker logic
        var sortedRolls = currentRolls.OrderByDescending(kvp => kvp.Value).ToList();
        

        var excludeList = new List<string>();
        excludeList.AddRange(currentRoundWinners);
        excludeList.AddRange(configuration.LastWinners);
        

        var replacement = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, 1).FirstOrDefault();
        

        if (string.IsNullOrEmpty(replacement))
        {
            excludeList = new List<string>(currentRoundWinners);
            replacement = FindMultipleWinnersWithTiebreaker(sortedRolls, excludeList, 1).FirstOrDefault();
        }
        
        if (!string.IsNullOrEmpty(replacement))
        {

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
            

            if (configuration.AutoPostResults)
            {
                if (configuration.ChatChannels.UseWinnerSpecificChannels && currentRoundWinners.Count > 0)
                {

                    for (int i = 0; i < currentRoundWinners.Count && i < 2; i++)
                    {
                        var winner = currentRoundWinners[i];
                        int winnerRoll = currentRolls.TryGetValue(winner, out int roll) ? roll : 0;
                        

                        ChatChannelType winnerChannel = i switch
                        {
                            0 => configuration.ChatChannels.Winner1Channel,
                            1 => configuration.ChatChannels.Winner2Channel,
                            _ => configuration.ChatChannels.ResultsChannel
                        };
                        
                        var channelCommand = configuration.ChatChannels.GetChannelCommand(winnerChannel);
                        

                        if (winner == replacement)
                        {

                            var winnerMessage = $"{channelCommand} {ProcessAnnouncementTemplate(configuration.Announcements.PassedWinnerResult, winner, winnerRoll, i + 1, stripMessage, winnerToPas)}";
                            QueueChatMessage(winnerMessage);
                        }
                        else
                        {

                            var winnerMessage = $"{channelCommand} {ProcessAnnouncementTemplate(configuration.Announcements.WinnerSpecificResult, winner, winnerRoll, i + 1, stripMessage)}";
                            QueueChatMessage(winnerMessage);
                        }
                    }
                }
                else
                {

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
            

        if (isJackpotRound)
            return false;

        // Check if there's at least one other player besides current winners
        var eligibleCount = currentRolls.Count(kvp => !currentRoundWinners.Contains(kvp.Key));
        return eligibleCount > 0;
    }

    public IReadOnlyDictionary<string, (int roll, BonusPrize prize)> GetBonusPrizeHits() => bonusPrizeHits;
    public IReadOnlyDictionary<string, int> GetCurrentRolls() => currentRolls;
    public bool IsGameActive => isGameActive;
    public bool IsRollingPhase => isRollingPhase;
    public string GetCurrentRoundWinner() => currentRoundWinners.FirstOrDefault() ?? ""; // Returns first winner for backwards compatibility

    private string ProcessAnnouncementTemplate(string template, string winnerName = "", int winnerRoll = 0, int winnerNumber = 0, string stripMessage = "", string passedFrom = "", string winnersList = "", string otherWinner = "", int otherRoll = 0)
    {
        var rollInstructions = "High roll ";
        if (configuration.EnableRandomDetection && configuration.EnableDiceDetection)
            rollInstructions += "(/random or /dice)";
        else if (configuration.EnableRandomDetection)
            rollInstructions += "(/random)";
        else if (configuration.EnableDiceDetection)
            rollInstructions += "(/dice)";
        rollInstructions += " chooses someone this round.";
        
        string multiWinnerText = configuration.NumberOfWinners > 1 ? $" {GetWinnerSelectionText()}!" : "";
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
            .Replace("{PASSED_FROM}", passedFrom)
            .Replace("{JACKPOT_VALUE}", configuration.JackpotValue.ToString())
            .Replace("{BONUS_PRIZE_WINNERS}", winnersList)
            .Replace("{OTHER_WINNER}", otherWinner)
            .Replace("{OTHER_ROLL}", otherRoll.ToString())
            .Replace("{AUTO_SKIPPED_LIST}", winnersList);
    }

    private void ProcessMessageQueue()
    {

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
        

        var countdownChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.CountdownChannel);
        if (message == $"{countdownChannel} {configuration.Announcements.CountdownGo}" && waitingForGoMessage)
        {
            isRollingPhase = true;
            waitingForGoMessage = false;
            chatGui.Print("[ToD] Go! Now collecting rolls...");
            

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

        waitingForGoMessage = true;
        
        var rulesChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.RulesChannel);
        var countdownChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.CountdownChannel);
        var resultsChannel = configuration.ChatChannels.GetChannelCommand(configuration.ChatChannels.ResultsChannel);
        

        var rollInstructions = "High roll ";
        if (configuration.EnableRandomDetection && configuration.EnableDiceDetection)
            rollInstructions += "(/random or /dice)";
        else if (configuration.EnableRandomDetection)
            rollInstructions += "(/random)";
        else if (configuration.EnableDiceDetection)
            rollInstructions += "(/dice)";
        rollInstructions += " chooses someone this round.";
        
        if (configuration.NumberOfWinners > 1)
            rollInstructions += $" {GetWinnerSelectionText()}!";
        

        QueueChatMessage($"{rulesChannel} {ProcessAnnouncementTemplate(configuration.Announcements.RulesLine1)}");
        QueueChatMessage($"{rulesChannel} {ProcessAnnouncementTemplate(configuration.Announcements.RulesLine2)}");
        QueueChatMessage($"{rulesChannel} {ProcessAnnouncementTemplate(configuration.Announcements.RulesLine3)}");
        QueueChatMessage($"{rulesChannel} {ProcessAnnouncementTemplate(configuration.Announcements.RulesLine4)}");
        
        // Extra rules lines for enabled features (before the countdown preamble)
        if (configuration.EnableJackpot && !string.IsNullOrWhiteSpace(configuration.JackpotRulesLine))
            QueueChatMessage($"{rulesChannel} {configuration.JackpotRulesLine}");
        if (configuration.EnableBonusPrizes && !string.IsNullOrWhiteSpace(configuration.BonusPrizeRulesLine))
            QueueChatMessage($"{rulesChannel} {configuration.BonusPrizeRulesLine}");
        
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

        mainWindow.IsOpen = true;
    }

    private void DrawUI() => WindowSystem.Draw();
    private void OpenConfigUI() => configWindow.IsOpen = true;
    private void OpenMainUI() => mainWindow.IsOpen = true;

    private string GetWinnerSelectionText()
    {
        return (configuration.NumberOfWinners, configuration.WinnerMode) switch
        {
            (1, WinnerSelectionMode.TopHighest) => "Highest roller wins",
            (1, WinnerSelectionMode.BottomLowest) => "Lowest roller wins", 
            (1, WinnerSelectionMode.Random) => "Random player wins",
            (1, WinnerSelectionMode.Middle) => "Middle roller wins",
            (1, WinnerSelectionMode.HighestAndLowest) => "Highest roller wins", // Fallback to highest for 1 winner
            
            (2, WinnerSelectionMode.TopHighest) => "Top 2 players win",
            (2, WinnerSelectionMode.HighestAndLowest) => "Highest and lowest players win",
            (2, WinnerSelectionMode.BottomLowest) => "Bottom 2 players win",
            (2, WinnerSelectionMode.Random) => "2 random players win", 
            (2, WinnerSelectionMode.Middle) => "Middle 2 players win",
            (_, WinnerSelectionMode.HighestAsksLowest) => "Highest asks lowest",
            
            _ => $"Top {configuration.NumberOfWinners} players win"
        };
    }

    // Authentication methods
    public bool TryAuthenticate()
    {
        return true;
    }

    public void Logout()
    {
    }

    public string GetCurrentUserIdentifier()
    {
        if (Svc.Objects?.LocalPlayer == null) return "";
        
        var characterName = Svc.Objects.LocalPlayer.Name.TextValue;
        var worldName = Svc.Objects.LocalPlayer.HomeWorld.Value.Name.ToString() ?? "";
        
        return string.IsNullOrEmpty(worldName) ? "" : $"{characterName}@{worldName}";
    }

    public bool IsUserAuthorized()
    {
        return true;
    }

    public void ResetSessionStatistics()
    {
        configuration.Statistics.ResetSession();
        configuration.Save();
    }
}
