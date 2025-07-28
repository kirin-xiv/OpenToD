Truth or Dare Plugin for FFXIV
A Dalamud plugin that automates Truth or Dare game management with automatic roll tracking and winner calculation.
Features

🎲 Automatic Roll Detection - Monitors party chat for /random rolls
🏆 Smart Winner Selection - Highest roll wins, but skips repeat winners
👕 Strip Detection - Automatically identifies players who rolled under 100
⏱️ Manual Processing - Click to process rolls when ready
💾 Persistent State - Remembers the last winner between sessions
🖥️ Real-time UI - Live display of current rolls with color coding

Installation
Prerequisites

XIVLauncher with Dalamud enabled
FFXIV with party chat access

Installation Steps

Download the latest release from the Releases page
Extract the plugin files to your Dalamud devPlugins folder:

Windows: %appdata%\XIVLauncher\devPlugins\TruthOrDarePlugin\


Restart FFXIV or reload Dalamud plugins
Enable the plugin in the Dalamud plugin installer

Usage
Starting a Game

Open the plugin window: Type /tod in chat
Start the game: Click the "Start Game" button
Announce to party: Tell everyone to type /random in party chat
Watch the rolls: Rolls will appear automatically in the plugin window

Processing Results

Wait for rolls: Let everyone roll their /random
Process when ready: Click "Process Rolls" button
See the results: Winner and strip list will be announced in chat

Commands

/tod - Opens the Truth or Dare control window

How It Works
Roll Detection
The plugin automatically monitors party chat for messages matching the pattern:
Random! [Player Name] rolls a [number]
Winner Logic

Highest roll wins - Player with the highest number is selected
Skip repeat winners - If the highest roller won the last round, the next highest wins
Strip detection - Players who rolled under 100 are added to the strip list

Example Output
[T/D] PlayerName wins (756) | Strip: LowRoller1, LowRoller2
Interface
Main Window

Game Status - Shows if a game is currently active
Current Rolls - Live display of all detected rolls

Red text for rolls under 100 (strip)
Normal text for safe rolls


Control Buttons:

Start Game
Stop Game
Process Rolls
Clear Last Winner



Roll Display
Rolls are displayed in descending order with color coding:

🔴 Red - Rolls under 100 (strip)
⚪ White - Normal rolls

Configuration
The plugin automatically saves:

Last Winner - Prevents the same person from winning consecutive rounds
Window Preferences - Remembers window size and position

To reset the last winner, click the "Clear Last Winner" button.
Troubleshooting
Rolls Not Being Detected

Ensure you're in a party
Check that players are using /random (not /roll or other commands)
Verify the plugin window shows "Game Active: Yes"

Plugin Not Loading

Check that the plugin files are in the correct directory
Restart FFXIV or reload Dalamud plugins
Check the Dalamud log (/xllog) for error messages

Window Not Appearing

Try typing /tod again
Check if the window is minimized or off-screen
Restart the plugin through the Dalamud plugin manager