# Truth or Dare Plugin

**Run ToD, Brain off!**

Automates Truth or Dare games in FFXIV by tracking rolls and determining winners automatically. No more manual counting or forgetting who won last round!

## 🎯 What It Does

- **Automatically detects /random rolls** during your Truth or Dare games
- **Smart tiebreaker system** - when multiple people roll the same number, whoever rolled first wins
- **Prevents repeat winners** - skips to the next highest roll if the same person won last round
- **Pass to next winner** - easily move the win to the next highest roller if someone declines
- **Identifies strippers** - anyone who rolls 100 or under
- **Outputs results directly** to your chat for easy copy/paste
- **Debug mode** for easy testing with /random 2

## 📥 Installation

1. Add this repository URL to your Dalamud plugin sources:
   ```
   https://raw.githubusercontent.com/kirin-xiv/FFToD-Release/main/repo.json
   ```

2. Install "Truth or Dare" from the plugin installer

3. Type `/tod` to open the plugin window

## ⚙️ Setup

### Configuration
1. Open the plugin with `/tod`
2. Click "Configuration"
3. Set your **Character Name** (important for detecting your own rolls)
4. Set **Roll Timeout** to 17 seconds (matches the macro timing)

### Required Macro
Create this macro in-game and add `/todstart` where indicated:

```
/sh Truth or Dare: High roll (/random) chooses someone this round. You cannot win two rounds in a row. <wait.2>
/sh Keep T/D in /yell. <wait.2>
/sh Max 3 rounds per dare. If you roll 100 or under, remove one item of clothing of your choice. <wait.2>
/sh High numbers cannot win twice in a row. If a repeat occurs, we move to second highest. <wait.2>
/sh Wi-Fi: MSS-8Y5FR8MZSQD8 || SplashBathhouse <wait.2>
/sh --- Rolls begin on 'Go!' after a short countdown --- <wait.2>
/sh 3... <wait.2>
/sh 2... <wait.2>
/sh 1... <wait.2>
/todstart
/sh Go! <wait.10>
/sh Rolls closing in 3... <wait.2>
/sh 2... <wait.2>
/sh 1... <wait.2>
/sh --- Rolls are now closed ---
```

## 🎮 How to Use

1. **Run your macro** - This handles all the announcements and timing
2. **Watch the plugin window** - You'll see rolls appear as people use `/random`
3. **Get results automatically** - After the timeout, results appear in your chat as grey text
4. **Copy and paste** - Copy the `/yell Winner: [name] | Strippers: [names]` line to yell chat
5. **Optional**: Use "Pass to Next Winner" if the winner declines

### Example Output
The plugin will output something like this to your chat:
```
/yell Winner: PlayerName (947) | Strippers: LowRoller, AnotherPlayer
```

## 🔧 Commands

- `/tod` - Open the main plugin window
- `/tod config` - Open configuration directly
- `/todstart` - Start collecting rolls (use in your macro)
- `/todstop` - Stop the current game manually
- `/todstatus` - Check current game status

## 📋 Features

### Smart Winner Selection
- **Automatic tiebreaker**: When multiple players roll the same highest number, whoever rolled first wins
- **No repeat winners**: If the highest roller won the previous round, automatically selects the next highest
- **Fallback protection**: If only the previous winner rolled, they win anyway (prevents deadlock)
- **Pass functionality**: Easily pass the win to the next highest roller with one button

### Automatic Stripper Detection
- Anyone rolling 100 or under is automatically identified
- Clean output: "None" if nobody needs to strip

### Real-time Tracking
- See rolls appear in the plugin window as they happen
- **Color-coded**: Low rolls (≤100) in red, high rolls (≥900) in green
- Roll timeout prevents games from running too long

### Debug Mode
- Enable in configuration for easier testing
- Use `/random 2` instead of `/random` for 50% chance of ties
- Perfect for testing the tiebreaker system

## 🛠️ Troubleshooting

**Plugin not detecting my rolls?**
- Make sure your Character Name is set correctly in configuration
- The plugin looks for "Random! You roll a [number]." vs "Random! [Name] rolls a [number]."

**Results not appearing?**
- Check that you're using `/todstart` in your macro at the right time
- Make sure the Roll Timeout (17 seconds) matches your macro timing

**Want to clear the last winner?**
- Use the "Clear Last Winner" button in the main window
- Or use `/tod` and click the clear button next to the last winner

**Testing tiebreakers?**
- Enable Debug Mode in configuration
- Use `/random 2` to easily create ties for testing

## 💡 Tips

- Keep the plugin window open during games to monitor rolls in real-time
- The 17-second timeout works perfectly with the provided macro
- Grey text in chat is your copy/paste line - look for it after "rolls are closed"
- Plugin remembers the last winner between sessions
- Use "Pass to Next Winner" if someone declines their dare
- Debug mode makes testing much easier with `/random 2`

## 🎭 Perfect for RP Venues!

This plugin was designed for FFXIV RP venues running Truth or Dare events. It eliminates manual work and ensures fair, consistent games every time. The automatic tiebreaker system means you never have to manually decide between tied players - the first roller always wins!

---

Made with ❤️ for the FFXIV RP community