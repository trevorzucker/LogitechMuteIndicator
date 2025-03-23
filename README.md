# Logitech Mute Indicator

A lightweight C# system tray application that monitors your selected microphone and changes Logitech RGB lighting based on mute status.

- 🎤 Detects audio input activity (based on volume, not software mute)
- 🔴 Changes Logitech lighting to **red** when input is "muted" (i.e., no sound)
- 🟢 Restores lighting when audio input resumes
- 🖱️ Tray icon with device selection menu
- 💾 Remembers your preferred microphone between sessions
- ⚡ Built with .NET 9.0 and WinForms (tray only, no visible UI)

## ⚙️ Features

- Select your preferred microphone from a dropdown tray menu
- Automatically reconnects when devices are unplugged/replugged
- Displays default device and device names
- Designed with minimal memory/CPU usage in mind

---

## 🧱 Requirements

- .NET 9.0 SDK (or later)
- Logitech G HUB installed and running
- Compatible Logitech RGB hardware
- Windows

---

## 🚀 Getting Started

1. **Clone the repository**

   ```
   bash
   git clone https://github.com/trevorzucker/LogitechMuteIndicator.git
   cd LogitechMuteIndicator
   ```

2. **Build the app**

   ```
   dotnet build
   dotnet run
   ```

3. **Optional: Compile For End Users (No .NET install required)**

   ```
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
   ```
