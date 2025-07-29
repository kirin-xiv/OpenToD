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
            HelpMessage = "Starts a Truth or Dare round"
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
        _ = StartGameAsync();
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

        // Debug logging for all chat types containing "Random!"
        if (configuration.LogAllChatTypes && messageText.Contains("Random!"))
        {
            pluginLog.Debug($"Chat message with 'Random!' - Type: {type} ({(int)type}), Message: {messageText}");
        }

        if (!isGameActive || !isRollingPhase)
            return;

        // Check if this is a roll message by pattern
        // Random messages appear as "Random! <player> rolls a <number>." for others
        // or "Random! You roll a <number>." for yourself
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
                // Try to get the player name from client state
                normalizedName = clientState.LocalPlayer.Name.TextValue;
            }
        }

        // Only accept the first roll from each player
        if (!currentRolls.ContainsKey(normalizedName))
        {
            currentRolls[normalizedName] = rollValue;

            if (configuration.EnableDebugLogging)
                pluginLog.Debug($"First roll recorded: {normalizedName} = {rollValue} (ChatType: {type} [{(int)type}])");
        }
        else
        {
            if (configuration.EnableDebugLogging)
                pluginLog.Debug($"Ignored duplicate roll from {normalizedName}");
        }


        if (configuration.EnableDebugLogging)
            pluginLog.Debug($"Roll captured: {normalizedName} = {rollValue} (ChatType: {type} [{(int)type}])");
    }

    private string NormalizePlayerName(string name)
    {
        var parts = name.Split(' ');
        if (parts.Length >= 2)
        {
            var lastPart = parts[^1];
            if (serverNames.Contains(lastPart))
            {
                return string.Join(' ', parts.Take(parts.Length - 1));
            }
        }
        return name;
    }

    public async Task StartGameAsync()
    {
        if (isGameActive)
        {
            chatGui.PrintError("[ToD] A game is already in progress!");
            return;
        }

        gameCancellation?.Cancel();
        gameCancellation = new CancellationTokenSource();
        var token = gameCancellation.Token;

        isGameActive = true;
        currentRolls.Clear();

        try
        {
            // Send announcement messages
            await SendYellAsync("Truth or Dare: High roll (/random) chooses someone this round. You cannot win two rounds in a row.");
            await Task.Delay(1000, token);

            await SendYellAsync("Reminder: Keep T/D in Yell chat.");
            await Task.Delay(1000, token);

            await SendYellAsync("Max 3 rounds per dare. If you roll 100 or under, remove one item of clothing of your choice.");
            await Task.Delay(1000, token);

            if (!string.IsNullOrEmpty(configuration.CustomAnnouncement))
            {
                await SendYellAsync(configuration.CustomAnnouncement);
                await Task.Delay(1000, token);
            }

            await SendYellAsync("Rolls begin after a short countdown.");
            await Task.Delay(2000, token);

            // Countdown
            await SendYellAsync("3...");
            await Task.Delay(1000, token);
            await SendYellAsync("2...");
            await Task.Delay(1000, token);
            await SendYellAsync("1...");
            await Task.Delay(1000, token);
            await SendYellAsync("Go!");

            isRollingPhase = true;

            // Wait for roll timeout
            await Task.Delay(configuration.RollTimeout * 1000, token);

            // Closing countdown
            await SendYellAsync("Rolls closing in 3...2...1... Rolls are now closed.");
            isRollingPhase = false;

            await Task.Delay(1000, token);

            // Process results
            ProcessResults();
        }
        catch (OperationCanceledException)
        {
            chatGui.Print("[ToD] Game cancelled.");
        }
        finally
        {
            isGameActive = false;
            isRollingPhase = false;
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
    }

    private void ProcessResults()
    {
        if (currentRolls.Count < configuration.MinimumRolls)
        {
            chatGui.PrintError($"[ToD] Not enough rolls received ({currentRolls.Count}/{configuration.MinimumRolls}). Game cancelled.");
            return;
        }

        // Find winner
        var sortedRolls = currentRolls.OrderByDescending(kvp => kvp.Value).ToList();
        string winner = "";
        int winnerRoll = 0;
        bool forcedRepeatWinner = false;

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
            forcedRepeatWinner = true;
        }

        // Find strip list
        var stripList = currentRolls.Where(kvp => kvp.Value <= 100).Select(kvp => kvp.Key).ToList();

        // Format messages
        var stripMessage = stripList.Count > 0 ? string.Join(", ", stripList) : "None";
        var resultMessage = $"[T/D] {winner} wins ({winnerRoll}) | Strip: {stripMessage} | Keep T/D in /yell! Dares can be 3 rounds max.";

        _ = SendYellAsync(resultMessage);

        // 🔽 Print summary locally for manual copy/paste
        var summaryMessage = $"Winner: {winner} ({winnerRoll}) | Strippers: {stripMessage}";
        chatGui.Print(summaryMessage);

        // ✅ Update last winner only if not forced repeat
        if (!forcedRepeatWinner)
        {
            configuration.LastWinner = winner;
            configuration.Save();
        }

        if (configuration.EnableDebugLogging)
            pluginLog.Debug($"Game complete. Winner: {winner} ({winnerRoll}). Strip list: {stripMessage}");
    }


    private async Task SendYellAsync(string message)
    {
        var command = $"/{configuration.ChatType.ToString().ToLower()} {message}";
        commandManager.ProcessCommand(command);
        await Task.Yield();
    }

    public void ClearLastWinner()
    {
        configuration.LastWinner = "";
        configuration.Save();
    }

    public IReadOnlyDictionary<string, int> GetCurrentRolls() => currentRolls;
    public bool IsGameActive => isGameActive;
    public bool IsRollingPhase => isRollingPhase;

    public void OpenConfigWindow()
    {
        configWindow.IsOpen = true;
        mainWindow.IsOpen = false;
    }

    private void DrawUI() => WindowSystem.Draw();
    private void OpenConfigUI() => configWindow.IsOpen = true;
    private void OpenMainUI() => mainWindow.IsOpen = true;
}
