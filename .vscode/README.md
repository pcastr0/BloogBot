# VSCode Debugging Setup for BloogBot

This directory contains the configuration files needed to debug the BloogBot Visual Studio solution in VSCode.

## Prerequisites

1. **Visual Studio 2022** (Community, Professional, or Enterprise) - Required for .NET Framework 4.6.1 projects with WPF and C++ components
2. **VSCode Extensions**:
   - C# Dev Kit (includes C# extension)
   - .NET Runtime Install Tool
   - IntelliCode for C# Dev Kit

## Setup Instructions

1. Install the required VSCode extensions from the Extensions panel
2. Ensure Visual Studio 2022 is installed with the following workloads:
   - .NET desktop development
   - Desktop development with C++

## Building the Solution

### Using VSCode Tasks

- **Debug Build**: `Ctrl+Shift+P` → `Tasks: Run Task` → `build-debug`
- **Release Build**: `Ctrl+Shift+P` → `Tasks: Run Task` → `build-release`
- **Clean**: `Ctrl+Shift+P` → `Tasks: Run Task` → `clean`

### Alternative Build Methods

If the MSBuild path in tasks.json doesn't match your Visual Studio installation, update the paths:

```json
"command": "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe"
```

Replace `Community` with `Professional` or `Enterprise` if you have a different edition.

## Debugging

### Debugging Configurations

1. **Debug BloogBot**: Launches the main BloogBot application
2. **Debug Bootstrapper**: Launches the Bootstrapper application
3. **Attach to Process**: Attach to an already running process

### Starting a Debug Session

1. Set breakpoints in your code
2. Press `F5` or go to the Run and Debug panel
3. Select the desired configuration from the dropdown
4. Click the green play button

## Project Structure

This solution contains:
- **.NET Framework 4.6.1** C# projects (BloogBot, Bootstrapper, various bot implementations)
- **C++ projects** (FastCall, Navigation, Loader) - These require Visual Studio's MSBuild
- **WPF applications** with XAML UI components

## Troubleshooting

### Build Issues

If you encounter build errors:

1. Verify Visual Studio 2022 is properly installed
2. Check that the MSBuild path in tasks.json matches your installation
3. Ensure all required workloads are installed in Visual Studio
4. Try building the solution in Visual Studio first to verify it works

### Debugging Issues

If debugging doesn't work:

1. Ensure the project builds successfully first
2. Check that the output paths in launch.json match the build output
3. Verify the C# Dev Kit extension is properly installed
4. Try the "Attach to Process" configuration as an alternative

### Symbol Loading

The configuration includes symbol server settings to help with debugging. If symbols aren't loading:

1. Check your internet connection
2. Verify the symbol server settings in launch.json
3. Consider disabling "requireExactSource" if you're working with modified code

### "Access is denied" Error in Bootstrapper

The bootstrapper performs privileged operations that may require elevated permissions. If you encounter "Access is denied" errors:

#### Most Common Causes:

1. **Missing Administrator Privileges** - The bootstrapper needs admin rights to:
   - Create processes with specific flags
   - Allocate memory in other processes
   - Write to other process memory
   - Create remote threads

2. **Antivirus/Security Software** - Security software may block:
   - Process injection operations
   - Cross-process memory access
   - Remote thread creation

3. **File Permissions** - Missing access to:
   - WoW executable (PathToWoW in bootstrapperSettings.json)
   - Loader.dll in the Bot directory
   - bootstrapperSettings.json configuration file

#### Diagnostic Steps:

1. **Run the Diagnostic Tool**:
   ```bash
   # Build the solution first
   dotnet build Bootstrapper/Bootstrapper.csproj
   
   # Run diagnostic tool
   Bot/Bootstrapper.exe --diagnose
   ```
   Or compile and run the DiagnosticTool.cs separately

2. **Check Administrator Privileges**:
   - Right-click VSCode and "Run as administrator"
   - Or run the built executable as administrator

3. **Verify File Paths**:
   - Check `Bot/bootstrapperSettings.json` for correct WoW path
   - Ensure `Bot/Loader.dll` exists and is accessible
   - Verify WoW executable exists at the specified path

4. **Temporarily Disable Security Software**:
   - Windows Defender Real-time Protection
   - Third-party antivirus software
   - Add exceptions for the Bot folder

#### Quick Fixes:

1. **Run as Administrator**:
   ```cmd
   # Build and run with admin privileges
   msbuild BloogBot.sln /p:Configuration=Debug /p:Platform=x86
   cd Bot
   Bootstrapper.exe
   ```

2. **Update WoW Path**:
   Edit `Bot/bootstrapperSettings.json`:
   ```json
   {
       "PathToWoW": "C:\\Path\\To\\Your\\WoW.exe"
   }
   ```

3. **Check Windows Security**:
   - Go to Windows Security → Virus & threat protection
   - Manage settings → Add/remove exclusions
   - Add folder exclusion for the Bot directory

## Notes

- This is a legacy .NET Framework project, not a .NET Core/.NET 5+ project
- The C++ components require Visual Studio's toolchain and cannot be built with dotnet CLI alone
- WPF applications may require additional configuration for proper debugging in VSCode