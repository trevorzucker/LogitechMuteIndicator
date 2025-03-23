
using LedCSharp;
using NAudio.CoreAudioApi;

namespace LogitechMuteIndicator;

internal static class Program
{
    public static readonly string DEFAULT_AUDIO_DEVICE_ID = "SystemDefault";
    public static AppSettings Settings = SettingsLoader.LoadSettings();

    [STAThread]
    public static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        SynchronizationContext uiContext = SynchronizationContext.Current!;
        using var trayIcon = new TrayIcon("Logitech Mute Indicator", uiContext);

        NotificationClient client = new(new MMDeviceEnumerator(), trayIcon.RebuildContextMenuFromAudioDevices);
        client.BeginCapture();
        trayIcon.OnDeviceChange += client.Reconnect;
        client.OnDeviceChange += trayIcon.RebuildContextMenuFromAudioDevices;
        client.OnUnmute += trayIcon.SetUnmute;
        client.OnMute += trayIcon.SetMute;

        Application.Run();
    }
}
