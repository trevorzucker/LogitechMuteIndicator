using LedCSharp;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;

namespace LogitechMuteIndicator;

class NotificationClient : IMMNotificationClient
{
    public event Action<List<AudioDevice>>? OnDeviceChange;
    public event Action? OnMute;
    public event Action? OnUnmute;
    private volatile bool capturing = false;
    private Thread? colorThread;
    private MMDevice? currentMic;
    private WasapiCapture? capture;
    private readonly MMDeviceEnumerator enumerator;
    private float lastAverage = 0;
    private bool lastMuted = false;
    private bool runningColorChange = true;
    private volatile bool tryingToReconnect = false;

    public NotificationClient(MMDeviceEnumerator enumerator, Action<List<AudioDevice>> onInit) {
        this.enumerator = enumerator;
        enumerator.RegisterEndpointNotificationCallback(this);
        onInit?.Invoke(ToSafeDeviceList(GetDeviceList()));
    }

    public void BeginCapture() {
        capturing = true;
        Task.Run(StartCapture);
    }

    private void StartCapture()
    {
        currentMic = FindTargetMic();

        if (currentMic == null)
        {
            Console.WriteLine("Mic not found.");
            Program.Settings.SelectedDeviceId = Program.DEFAULT_AUDIO_DEVICE_ID;
            Reconnect();
            Task.Run(TryReconnectAsync);
            return;
        }

        Console.WriteLine($"Using mic: {currentMic.FriendlyName}");

        capture = new WasapiCapture(currentMic);
        capture.DataAvailable += OnDataAvailable;
        capture.StartRecording();
    }

    private void StopCapture()
    {
        if (capture != null)
        {
            try
            {
                capture.DataAvailable -= OnDataAvailable;
                if (capture.CaptureState == CaptureState.Capturing)
                {
                    capture.StopRecording();
                }
                capture.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping capture: {ex}");
            }
            finally
            {
                capture = null;
            }
        }
    }

    private void OnDeviceLost() {
        Task.Run(() =>
        {
            StopCapture();
            TryReconnectAsync();
            currentMic = null;
        });
    }

    private MMDeviceCollection GetDeviceList() {
        return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
    }

    // for thread-safe audio device data handoff
    public static List<AudioDevice> ToSafeDeviceList(MMDeviceCollection devices)
    {
        var enumerator = new MMDeviceEnumerator();
        var defaultDeviceId = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console).ID;

        var list = new List<AudioDevice>();

        foreach (var device in devices)
        {
            list.Add(new AudioDevice
            {
                Id = device.ID,
                Name = device.FriendlyName,
                IsDefault = device.ID == defaultDeviceId
            });
        }

        return list;
    }



    private MMDevice? FindTargetMic()
    {
        var devices = GetDeviceList();
        if (Program.Settings.SelectedDeviceId == Program.DEFAULT_AUDIO_DEVICE_ID) {
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        }
        return devices.FirstOrDefault(device => device.ID == Program.Settings.SelectedDeviceId);
    }

    private async void TryReconnectAsync()
    {
        if (tryingToReconnect)
            return;

        tryingToReconnect = true;

        while (true)
        {
            var mic = FindTargetMic();
            if (mic != null)
            {
                Thread.Sleep(100);
                currentMic = mic;
                StartCapture();
                break;
            }

            await Task.Delay(1000);
        }
        tryingToReconnect = false;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        int sampleCount = e.BytesRecorded / 2;
        float sum = 0;

        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = BitConverter.ToInt16(e.Buffer, i);
            float normalized = sample / 32768f;
            sum += normalized;
        }

        float average = sum / sampleCount;
        bool isMuted = Math.Abs(lastAverage - average) < 0.0001f;
        lastAverage = average;

        colorThread = new Thread(() => SetDeviceColorToRed());

        if (lastMuted != isMuted)
        {
            if (isMuted) {
                Console.WriteLine($"Making lighting red");
                LogitechGSDK.LogiLedInit();
                runningColorChange = true;
                colorThread.Start();
                OnMute?.Invoke();
            } else {
                Console.WriteLine($"Restoring lighting");
                runningColorChange = false;
                if (colorThread.IsAlive) {
                    colorThread.Join();
                }
                LogitechGSDK.LogiLedShutdown();
                OnUnmute?.Invoke();
            }
        }

        lastMuted = isMuted;
    }

    private void SetDeviceColorToRed() {
        while (runningColorChange) {
            LogitechGSDK.LogiLedSetLighting(100, 0, 0);
            Thread.Sleep(1000);
        }
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        if (newState == DeviceState.Unplugged || newState == DeviceState.NotPresent || newState == DeviceState.Disabled)
        {
            Reconnect();
        }
    }

    public void OnDeviceAdded(string pwstrDeviceId) { 
        currentMic = FindTargetMic();

        if (currentMic == null)
        {
            Program.Settings.SelectedDeviceId = Program.DEFAULT_AUDIO_DEVICE_ID;
            Reconnect();
        }
     }

    public void OnDeviceRemoved(string deviceId)
    {
        if (currentMic != null && deviceId == currentMic.ID)
        {
            Reconnect();
        }
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) {
        Reconnect();
    }

    public void Reconnect() {
        OnDeviceChange?.Invoke(ToSafeDeviceList(GetDeviceList()));
        OnDeviceLost();
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
}

public class AudioDevice
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsDefault { get; set; }
}