# PicoNvidiaUpdater

This project is a lightweight tool to check for Nvidia GPU drivers update on Windows. You might now a similar project to this one, the OG `TinyNvidiaUpdateChecker`. The purpose are the same, but this repo aims to simplify the update process and not requiring GUI interaction at all, everything is on the terminal.

Features:

- Interactive and quiet mode
- Complete and minimal installation
- Driver selection (Game Ready/Studio Ready)
- Notebook/desktop driver download override
- Download-only mode without installation
- Check mode, exit code 0 means no update, otherwise an update is available

## How to Run

- Internet connection
- Windows 10 or higher
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)*

You'll need .NET 8 to run the executable on the release page. If you don't have .NET 8, you can recompile the project down to any .NET version you have, as long as it is .NET not Framework version. Also, this app has a built in 7z archiver downloader, so you don't need to install 7z separately for the minimal installation feature.

## Command Line Switches

| Argument | Description |
|----------|-------------|
| `-i` `--interactive` | Run in interactive mode, you will be asked several questions about the installation |
| `-q` `--quiet` | Install the driver without confirmation and without popup windows |
| `-c` `--check` | Perform update checks only, without downloading and installation |
| `-m` `--minimal` | Install the graphics driver only, excluding all other features bundled in the setup |
| `-t` `--driver-type` | Driver type, `GameReady` or `StudioReady` |
| `-o` `--output-path` | Output path to save the installer and temporary files, usually you can ignore this |
| `-d` `--download` | Download the drivers but don't install or delete the installation files. You must specify `-o` to use this |
| `--override-desktop` | Force the driver detection to find for desktop drivers |
| `--override-notebook` | Force the driver detection to find for notebook/e-GPU drivers |

## License

This project is licensed under MIT License.

Huge thanks to `TinyNvidiaUpdateChecker` for the inspiration and original code which I refactored here.
