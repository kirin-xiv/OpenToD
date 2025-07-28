// ========================================
// Windows/MainWindow.cs - REPLACE ENTIRE CONTENT
// ========================================
using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Linq;

namespace TruthOrDarePlugin.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin _plugin;

        public MainWindow(Plugin plugin) : base("Truth or Dare Controller###TruthOrDareMain")
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(400, 300),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            _plugin = plugin;
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            ImGui.Text("Game Status:");
            ImGui.Separator();

            ImGui.Text($"Game Active: {(_plugin.IsGameActive ? "Yes" : "No")}");
            ImGui.Text($"Rolling Phase: {(_plugin.IsRollingPhase ? "Yes" : "No")}");
            ImGui.Text($"Current Rolls: {_plugin.CurrentRolls.Count}");
            ImGui.Text($"Last Winner: {_plugin.LastWinner ?? "None"}");

            ImGui.Spacing();

            ImGui.Text("Controls:");
            ImGui.Separator();

            if (ImGui.Button("Start Round"))
            {
                _plugin.StartTruthOrDareRound();
            }

            ImGui.SameLine();

            if (ImGui.Button("Stop Round"))
            {
                _plugin.StopTruthOrDareRound();
            }

            ImGui.SameLine();

            if (ImGui.Button("Clear Last Winner"))
            {
                _plugin.ClearLastWinner();
            }

            ImGui.Spacing();

            if (_plugin.CurrentRolls.Any())
            {
                ImGui.Text("Current Rolls:");
                ImGui.Separator();

                if (ImGui.BeginTable("rolls_table", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Player");
                    ImGui.TableSetupColumn("Roll");
                    ImGui.TableHeadersRow();

                    foreach (var roll in _plugin.CurrentRolls.OrderByDescending(r => r.Value))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(roll.Key);
                        ImGui.TableNextColumn();

                        var color = roll.Value switch
                        {
                            < 100 => new Vector4(1, 0.5f, 0.5f, 1),
                            > 900 => new Vector4(0.5f, 1, 0.5f, 1),
                            _ => Vector4.One
                        };

                        ImGui.TextColored(color, roll.Value.ToString());
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.Spacing();

            ImGui.Text("Quick Commands:");
            ImGui.Separator();
            ImGui.BulletText("/tod - Open this window");
            ImGui.BulletText("/todstart - Start a round");
            ImGui.BulletText("/todstop - Stop current round");
            ImGui.BulletText("/todstatus - Show status in chat");

            ImGui.Spacing();

            if (ImGui.Button("Open Config"))
            {
                _plugin.ToggleConfigUI();
            }
        }
    }
}
