namespace LogitechMuteIndicator;

public class TrayIcon : IDisposable
{
    public event Action? OnDeviceChange;
    private bool _disposed = false;
    private readonly NotifyIcon notifyIcon;
    private ToolStripMenuItem? audioDeviceSubMenu;
    private readonly SynchronizationContext syncContext;

    private static readonly Icon muteIcon = LoadIconFromResource("LogitechMuteIndicator.icon.tray_on.ico");
    private static readonly Icon unmuteIcon = LoadIconFromResource("LogitechMuteIndicator.icon.tray_off.ico");

    private static Icon LoadIconFromResource(string resourceName)
    {
        using Stream stream = typeof(Program).Assembly.GetManifestResourceStream(resourceName)!;
        return new Icon(stream);
    }

    public TrayIcon(string tooltip, SynchronizationContext syncContext)
    {
        this.syncContext = syncContext;
        notifyIcon = new NotifyIcon
        {
            Icon = unmuteIcon,
            Text = tooltip,
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
    }

    public void SetMute() {
        notifyIcon.Icon = muteIcon;
    }

    public void SetUnmute() {
        notifyIcon.Icon = unmuteIcon;
    }

    public ContextMenuStrip BuildContextMenu()
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        audioDeviceSubMenu = new ToolStripMenuItem("Input device");
        
        menu.Items.Add(audioDeviceSubMenu);
        menu.Items.Add("Exit", null, OnExit);
        
        return menu;
    }

    public void RebuildContextMenuFromAudioDevices(List<AudioDevice> devices)
    {
        syncContext.Post(_ =>
        {
            var menu = BuildContextMenu();

            AudioDevice systemDefault = new() {
                Id = Program.DEFAULT_AUDIO_DEVICE_ID,
                IsDefault = true,
                Name = $"System Default {devices.FirstOrDefault(device => device.IsDefault)?.Name ?? "Unknown"}"
            };

            ToolStripMenuItem defaultAudioItem = GenerateMenuItem(systemDefault);

            audioDeviceSubMenu?.DropDownItems.Add(defaultAudioItem);

            foreach (var device in devices)
            {
                ToolStripMenuItem audioItem = GenerateMenuItem(device);

                audioDeviceSubMenu?.DropDownItems.Add(audioItem);
            }

            notifyIcon.ContextMenuStrip = menu;

        }, null);
    }

    private ToolStripMenuItem GenerateMenuItem(AudioDevice device) {
        ToolStripMenuItem audioItem = new(device.Name) {
            Checked = Program.Settings.SelectedDeviceId == device.Id,
            CheckOnClick = false
        };

        audioItem.Click += (s, e) => {
            Program.Settings.SelectedDeviceId = device.Id;
            SettingsLoader.SaveSettings(Program.Settings);
            foreach(ToolStripMenuItem item in audioDeviceSubMenu!.DropDownItems) {
                item.Checked = false;
            }
            audioItem.Checked = true;
            OnDeviceChange?.Invoke();
        };

        return audioItem;
    }

    private void OnExit(object? sender, EventArgs e)
    {
        notifyIcon.Visible = false;
        Application.Exit();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            notifyIcon.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~TrayIcon()
    {
        Dispose(false);
    }
}
