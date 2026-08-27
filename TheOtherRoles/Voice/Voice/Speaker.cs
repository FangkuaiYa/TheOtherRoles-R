using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TheOtherRoles.Voice.Voice;

internal interface ISpeakerContext
{
    ISampleProvider GetEndpoint();
}

public interface ISpeaker
{
    internal void Initialize(ISpeakerContext speakerContext);

    /// <summary>
    ///     Called when playback ends.
    /// </summary>
    internal void Close();
}

public class ManualSpeaker : ISpeaker
{
    private readonly Action onClosed;
    private bool _initialized;
    private bool _warnedNull;
    private ISpeakerContext speakerContext;

    private float[] tempArray;


    public ManualSpeaker(Action onClosed)
    {
        this.onClosed = onClosed;
    }

    void ISpeaker.Initialize(ISpeakerContext speakerContext)
    {
        this.speakerContext = speakerContext;
        _initialized = true;
    }

    void ISpeaker.Close()
    {
        onClosed?.Invoke();
    }

    public void Read(IList<float> buffer)
    {
        if (!_initialized && !_warnedNull)
        {
            _warnedNull = true;
            TheOtherRolesPlugin.Logger.LogWarning(
                "[VC:Spk] ManualSpeaker.Read called before Initialize — speaker will be silent.");
        }

        if (tempArray == null || tempArray.Length < buffer.Count) tempArray = new float[buffer.Count];
        var length = speakerContext?.GetEndpoint()?.Read(tempArray, 0, buffer.Count) ?? 0;
        for (var i = 0; i < buffer.Count; i++) buffer[i] = i < length ? tempArray[i] : 0f;
    }
}

public class WindowsSpeaker : ISpeaker
{
    private WasapiOut waveOut;

    public WindowsSpeaker(string deviceName)
    {
        var deviceEnumerator = new MMDeviceEnumerator();
        var device = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .FirstOrDefault(device => device.FriendlyName == deviceName);
        device ??= deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        waveOut = new WasapiOut(device, AudioClientShareMode.Shared, false, 50);
    }

    void ISpeaker.Initialize(ISpeakerContext speakerContext)
    {
        if (waveOut == null) throw new InvalidOperationException("Speaker already have been used.");

        waveOut.Init(speakerContext.GetEndpoint());
        waveOut.Play();
    }

    void ISpeaker.Close()
    {
        if (waveOut != null)
        {
            waveOut.Stop();
            waveOut.Dispose();
            waveOut = null;
        }
    }
}