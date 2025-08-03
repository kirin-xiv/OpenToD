using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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
    private CancellationTokenSource? gameCancellation;

    // Store the current round winner separately from the "last winner" used for exclusion
    private string currentRoundWinner = "";

    // Server list for name normalization
    private readonly HashSet<string> serverNames = new()
    {
        "Adamantoise", "Aegis", "Alexander", "Alpha", "Anima", "Asura", "Atomos", "Bahamut",
        "Balmung", "Behemoth", "Belias", "Brynhildr", "Cactuar", "Carbuncle", "Cerberus",
        "Chocobo", "Coeurl", "Cuchulainn", "Diabolos", "Durandal", "Dynamis", "Excalibur",
        "Exodus", "Faerie", "Famfrit", "Fenrir", "Garuda", "Gilgamesh", "Goblin", "Gungnir",
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

        // Check if this is a roll message
        var rollMatch = Regex.Match(messageText, @"Random! (.+) rolls? a (\d+)\.");
        if (!rollMatch.Success)
            return;

        var playerName = rollMatch.Groups[1].Value;
        var rollValue = int.Parse(rollMatch.Groups[2].Value);

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
            pluginLog.Debug($"Roll recorded: {normalizedName} = {rollValue}");
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

    public void StartGame()
    {
        if (isGameActive)
        {
            chatGui.PrintError("[ToD] A game is already in progress!");
            return;
        }

        // Update last winner from previous round at the START of new round
        if (!string.IsNullOrEmpty(currentRoundWinner))
        {
            configuration.LastWinner = currentRoundWinner;
            configuration.Save();
        }

        gameCancellation?.Cancel();
        gameCancellation = new CancellationTokenSource();

        isGameActive = true;
        isRollingPhase = true;
        currentRolls.Clear();
        currentRoundWinner = ""; // Clear current round winner

        chatGui.Print("[ToD] Game started! Collecting rolls...");

        // Auto-close after timeout
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
        currentRoundWinner = ""; // Clear current round winner when stopped

        chatGui.Print("[ToD] Game stopped.");
    }

    private void ProcessResults()
    {
        if (currentRolls.Count < 2) // Hardcoded minimum
        {
            chatGui.PrintError($"[ToD] Not enough rolls received ({currentRolls.Count}/2). Game cancelled.");
            isGameActive = false;
            currentRoundWinner = "";
            return;
        }

        // Find winner (skip last winner if possible)
        var sortedRolls = currentRolls.OrderByDescending(kvp => kvp.Value).ToList();
        string winner = "";
        int winnerRoll = 0;

        foreach (var roll in sortedRolls)
        {
            if (roll.Key != configuration.LastWinner)
            {
                winner = roll.Key;
                winnerRoll = roll.Value;
                break;
            }
        }

        // Fallback if only last winner rolled
        if (string.IsNullOrEmpty(winner) && sortedRolls.Count > 0)
        {
            winner = sortedRolls[0].Key;
            winnerRoll = sortedRolls[0].Value;
        }

        // Store current round winner (but don't update LastWinner yet)
        currentRoundWinner = winner;

        // Find strippers (100 or under)
        var stripList = currentRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
        var stripMessage = stripList.Count > 0 ? string.Join(", ", stripList) : "None";

        // Print copy/paste result
        var summaryMessage = $"/yell Winner: {winner} ({winnerRoll}) | Strippers: {stripMessage}";
        chatGui.Print($"{summaryMessage}");

        isGameActive = false;
        pluginLog.Debug($"Game complete. Winner: {winner} ({winnerRoll}). Strip list: {stripMessage}");
    }

    public void ClearLastWinner()
    {
        configuration.LastWinner = "";
        configuration.Save();
    }

    public void PassToNextWinner()
    {
        if (string.IsNullOrEmpty(currentRoundWinner) || currentRolls.Count < 2)
            return;

        // Find next eligible winner (exclude current winner and last winner)
        var sortedRolls = currentRolls.OrderByDescending(kvp => kvp.Value).ToList();
        string newWinner = "";
        int newWinnerRoll = 0;

        foreach (var roll in sortedRolls)
        {
            if (roll.Key != currentRoundWinner && roll.Key != configuration.LastWinner)
            {
                newWinner = roll.Key;
                newWinnerRoll = roll.Value;
                break;
            }
        }

        // Fallback: if no one else available, just exclude current winner
        if (string.IsNullOrEmpty(newWinner))
        {
            foreach (var roll in sortedRolls)
            {
                if (roll.Key != currentRoundWinner)
                {
                    newWinner = roll.Key;
                    newWinnerRoll = roll.Value;
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(newWinner))
        {
            // Update last winner to the previous winner (for next round exclusion)
            configuration.LastWinner = currentRoundWinner;
            configuration.Save();

            // Update current round winner
            currentRoundWinner = newWinner;

            // Find strippers for new announcement
            var stripList = currentRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();
            var stripMessage = stripList.Count > 0 ? string.Join(", ", stripList) : "None";

            // Announce new winner
            var summaryMessage = $"/yell Winner: {newWinner} ({newWinnerRoll}) | Strippers: {stripMessage}";
            chatGui.Print($"{summaryMessage}");
            
            pluginLog.Debug($"Passed to next winner: {newWinner} ({newWinnerRoll})");
        }
    }

    public bool CanPass()
    {
        if (string.IsNullOrEmpty(currentRoundWinner) || isGameActive || currentRolls.Count < 2)
            return false;

        // Check if there's at least one other player besides current winner
        var eligibleCount = currentRolls.Count(kvp => kvp.Key != currentRoundWinner);
        return eligibleCount > 0;
    }

    public IReadOnlyDictionary<string, int> GetCurrentRolls() => currentRolls;
    public bool IsGameActive => isGameActive;
    public bool IsRollingPhase => isRollingPhase;
    public string GetCurrentRoundWinner() => currentRoundWinner; // New method to get current winner

    public void OpenConfigWindow()
    {
        configWindow.IsOpen = true;
        mainWindow.IsOpen = false;
    }

    private void DrawUI() => WindowSystem.Draw();
    private void OpenConfigUI() => configWindow.IsOpen = true;
    private void OpenMainUI() => mainWindow.IsOpen = true;
}
