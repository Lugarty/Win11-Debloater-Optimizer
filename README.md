# Win11 Debloater & Optimizer

![C#](https://img.shields.io/badge/C%23-12.0-512BD4?logo=csharp)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Windows 11](https://img.shields.io/badge/Windows-11-0078D6?logo=windows)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1+-5391FE?logo=powershell)

> A Windows 11 maintenance utility built with C#/.NET 8, WinForms, and PowerShell.

## Overview

Win11 Debloater & Optimizer provides a desktop interface for selected Windows maintenance and configuration tasks. It coordinates PowerShell scripts and built-in Windows tools, streams output to the interface, and requests confirmation for higher-impact operations.

## Features

- Privacy and telemetry settings
- Selective AppX and provisioned-app removal
- Windows services and scheduled-task controls
- Temporary-file and update-cache cleanup
- DISM, SFC, and power configuration operations
- Asynchronous execution with live output and categorized logs
- Additional confirmation for higher-impact actions

## Tech Stack

- C# 12 and .NET 8
- Windows Forms
- Windows PowerShell 5.1+
- Registry, Services, Scheduled Tasks, AppX, DISM, SFC, and `powercfg`

## Download

Download the Windows executable from the [v1.0 release](https://github.com/Lugarty/Win11-Debloater-Optimizer/releases/download/launcher/Win11Debloater.exe).

## Build from Source

Requirements: Windows 11 and the .NET 8 SDK.

```powershell
git clone https://github.com/Lugarty/Win11-Debloater-Optimizer.git
cd Win11-Debloater-Optimizer
dotnet run
```

To publish a self-contained single-file build:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

## Safety

The application can modify the Registry, services, scheduled tasks, installed applications, and Windows components. Review each operation before confirming it. The app attempts to enable System Restore and create a restore point, but this is best-effort and does not replace a backup. Review the logs and verify the resulting system state after important operations.

Each task reports success, completion with warnings, or failure. Non-zero PowerShell process exit codes and unhandled C# exceptions are reported as failures; PowerShell error output and explicit `[AVISO]` messages are reported as warnings. Review the detailed log after any warning because some commands can complete only part of their work.

## Current Limitations

- Behavior may vary across Windows 11 editions and builds.
- Some commands tolerate missing components, and PowerShell error output may be non-terminating; review warning details in the log.
- No automated test suite or GitHub Actions workflow is currently included.

## License

Released under the MIT License. See [LICENSE](LICENSE).

## Author

[Anísio Oliveira (@Lugarty)](https://github.com/Lugarty)
