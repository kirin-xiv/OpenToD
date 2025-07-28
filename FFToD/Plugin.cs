// ========================================
// Plugin.cs - REPLACE ENTIRE CONTENT
// ========================================
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TruthOrDarePlugin.Windows;

namespace TruthOrDarePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

        private const string CommandName = "/tod";
        private const string StartCommand = "/todstart";
        private const string StopCommand = "/todstop";
        private const string StatusCommand = "/todstatus";

        public Configuration Configuration { get; init; }
        public readonly WindowSystem WindowSystem = new("SamplePlugin");
        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        private bool _isGameActive = false;
        private bool _isRollingPhase = false;
        private DateTime _rollPhaseStart;
        private Dictionary<string, int> _currentRolls = new();
        private string? _lastWinner = null;

        private readonly Regex _rollRegex = new(@"Random!\s+(.+?)\s+rolls?\s+a?\s*(\d+)", RegexOptions.IgnoreCase);

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            RegisterCommands();

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

            ChatGui.ChatMessage += OnChatMessage;

            _lastWinner = Configuration.LastWinner;

            Log.Information("Truth or Dare Plugin initialized successfully!");
        }

        private void RegisterCommands()
        {
            CommandManager.AddHandler(CommandName, new CommandInfo(OnMainCommand)
            {
                HelpMessage = "Opens Truth or Dare plugin window"
            });

            CommandManager.AddHandler(StartCommand, new CommandInfo(OnStartCommand)
            {
                HelpMessage = "Starts a Truth or Dare round"
            });

            CommandManager.AddHandler(StopCommand, new CommandInfo(OnStopCommand)
            {
                HelpMessage = "Stops the current Truth or Dare round"
            });

            CommandManager.AddHandler(StatusCommand, new CommandInfo(OnStatusCommand)
            {
                HelpMessage = "Shows current Truth or Dare status"
            });
        }

        public void Dispose()
        {
            ChatGui.ChatMessage -= OnChatMessage;
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            MainWindow.Dispose();

            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(StartCommand);
            CommandManager.RemoveHandler(StopCommand);
            CommandManager.RemoveHandler(StatusCommand);

            Log.Information("Truth or Dare Plugin disposed successfully!");
        }

        private void OnMainCommand(string command, string args) => ToggleMainUI();
        private void OnStartCommand(string command, string args) => StartTruthOrDareRound();
        private void OnStopCommand(string command, string args) => StopTruthOrDareRound();
        private void OnStatusCommand(string command, string args) => ShowStatus();

        public void StartTruthOrDareRound()
        {
            if (_isGameActive)
            {
                ChatGui.Print("Truth or Dare round is already active!");
                return;
            }

            _isGameActive = true;
            _isRollingPhase = true;
            _rollPhaseStart = DateTime.Now;
            _currentRolls.Clear();

            var announcement = string.IsNullOrEmpty(Configuration.CustomAnnouncement)
                ? "🎲 TRUTH OR DARE TIME! 🎲 Rules: High roll wins (unless you won last round)! Rolls under 100 = strip! 3... 2... 1... GO! Type /random now!"
                : Configuration.CustomAnnouncement;

            SendToChat(announcement, Configuration.AnnouncementChatType);

            Task.Delay(TimeSpan.FromSeconds(Configuration.RollTimeout))
                .ContinueWith(_ => ProcessRollTimeout());

            Log.Information("Truth or Dare round started");
        }

        public void StopTruthOrDareRound()
        {
            if (!_isGameActive)
            {
                ChatGui.Print("No Truth or Dare round is currently active.");
                return;
            }

            _isGameActive = false;
            _isRollingPhase = false;
            _currentRolls.Clear();

            SendToChat("Truth or Dare round stopped.", XivChatType.Echo);
            Log.Information("Truth or Dare round stopped");
        }

        private void ShowStatus()
        {
            if (!_isGameActive)
            {
                ChatGui.Print("No Truth or Dare round is currently active.");
                return;
            }

            var status = $"Truth or Dare Status: Game Active: {_isGameActive}, Rolling Phase: {_isRollingPhase}, Current Rolls: {_currentRolls.Count}, Last Winner: {_lastWinner ?? "None"}";

            if (_currentRolls.Any())
            {
                status += " Current Rolls: ";
                foreach (var roll in _currentRolls.OrderByDescending(r => r.Value))
                {
                    status += $"{roll.Key}: {roll.Value}, ";
                }
            }

            ChatGui.Print(status);
        }

        private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (!_isGameActive || !_isRollingPhase)
                return;

            if (!IsRelevantChatType(type))
                return;

            var messageText = message.TextValue;
            var match = _rollRegex.Match(messageText);
            if (match.Success)
            {
                var playerName = match.Groups[1].Value.Trim();
                if (int.TryParse(match.Groups[2].Value, out var rollValue))
                {
                    _currentRolls[playerName] = rollValue;
                    Log.Information($"Recorded roll: {playerName} = {rollValue}");

                    if (ShouldAutoProcess())
                    {
                        Task.Delay(2000).ContinueWith(_ => ProcessRolls());
                    }
                }
            }
        }

        private bool IsRelevantChatType(XivChatType type)
        {
            return type == XivChatType.Party ||
                   type == XivChatType.Alliance ||
                   type == XivChatType.CrossLinkShell1 ||
                   type == XivChatType.FreeCompany;
        }

        private bool ShouldAutoProcess()
        {
            return _currentRolls.Count >= Configuration.MinimumRolls &&
                   DateTime.Now.Subtract(_rollPhaseStart).TotalSeconds >= Configuration.MinimumWaitTime;
        }

        private void ProcessRollTimeout()
        {
            if (!_isGameActive || !_isRollingPhase)
                return;

            if (_currentRolls.Count == 0)
            {
                SendToChat("No rolls detected. Truth or Dare round cancelled.", XivChatType.Echo);
                StopTruthOrDareRound();
                return;
            }

            ProcessRolls();
        }

        private void ProcessRolls()
        {
            if (!_isGameActive || !_isRollingPhase || _currentRolls.Count == 0)
                return;

            _isRollingPhase = false;

            var (winner, winningRoll) = DetermineWinner();
            var stripList = _currentRolls.Where(r => r.Value < 100).Select(r => r.Key).ToList();
            var result = FormatResult(winner, winningRoll, stripList);

            SendToChat(result, Configuration.ResultChatType);

            _lastWinner = winner;
            Configuration.LastWinner = _lastWinner;
            Configuration.Save();

            _isGameActive = false;
            _currentRolls.Clear();

            Log.Information($"Processed rolls - Winner: {winner} ({winningRoll}), Strip: {string.Join(", ", stripList)}");
        }

        private (string winner, int roll) DetermineWinner()
        {
            var sortedRolls = _currentRolls.OrderByDescending(r => r.Value).ToList();
            var topRoll = sortedRolls[0];

            if (_lastWinner != null && topRoll.Key.Equals(_lastWinner, StringComparison.OrdinalIgnoreCase))
            {
                var nextRoll = sortedRolls.Skip(1).FirstOrDefault();
                return nextRoll.Key != null ? (nextRoll.Key, nextRoll.Value) : (topRoll.Key, topRoll.Value);
            }

            return (topRoll.Key, topRoll.Value);
        }

        private string FormatResult(string winner, int winningRoll, List<string> stripList)
        {
            var result = $"[T/D] {winner} wins ({winningRoll})";

            if (stripList.Any())
            {
                result += $" | Strip: {string.Join(", ", stripList)}";
            }

            result += " | Keep T/D in /yell! Dares can be 3 rounds max.";
            return result;
        }

        private void SendToChat(string message, XivChatType chatType)
        {
            try
            {
                var formattedMessage = chatType switch
                {
                    XivChatType.Yell => $"[YELL] {message}",
                    XivChatType.Say => $"[SAY] {message}",
                    XivChatType.Party => $"[PARTY] {message}",
                    _ => message
                };

                ChatGui.Print(formattedMessage);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to send chat message: {ex.Message}");
                ChatGui.Print($"Error sending message: {ex.Message}");
            }
        }

        public void ClearLastWinner()
        {
            _lastWinner = null;
            Configuration.LastWinner = null;
            Configuration.Save();
            ChatGui.Print("Last winner cleared.");
            Log.Information("Last winner cleared");
        }

        private void DrawUI() => WindowSystem.Draw();
        public void ToggleConfigUI() => ConfigWindow.Toggle();
        public void ToggleMainUI() => MainWindow.Toggle();

        public bool IsGameActive => _isGameActive;
        public bool IsRollingPhase => _isRollingPhase;
        public Dictionary<string, int> CurrentRolls => _currentRolls;
        public string? LastWinner => _lastWinner;
    }
}
