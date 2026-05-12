# OpenToD - Truth or Dare Plugin

**This project is unmaintained and looking for a new owner. Feel free to fork.**

Automates Truth or Dare games in FFXIV by tracking rolls and determining winners automatically.

## Installation

This plugin is not yet in the main Dalamud repository. For now, build from source or check releases.

## Features

- Automatically detects /random rolls during Truth or Dare games
- Smart tiebreaker system - first roller wins on ties
- Prevents repeat winners
- Pass to next winner functionality
- Identifies players who roll 100 or under
- Debug mode for testing

## Commands

- `/tod` - Open the main plugin window
- `/tod config` - Open configuration
- `/todstart` - Start collecting rolls
- `/todstop` - Stop the current game
- `/todstatus` - Check current game status

## Building

Standard Dalamud plugin build process:
```
dotnet build
```

## License

This project is open source. See LICENSE file for details.

## Contributing

This project is no longer actively maintained. Feel free to fork and continue development.

## For Forkers

Current users receive updates through a private repository. If you fork this and want to distribute updates, you'll need to either:
- Submit to the official Dalamud plugin repository
- Have users add your custom repository URL