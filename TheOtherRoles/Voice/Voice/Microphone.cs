using System;
using System.Linq;
using NAudio.Wave;

namespace TheOtherRoles.Voice.Voice;

internal interface IMicrophoneContext
{
    /// <summary>
    ///     Called when audio data should be sent.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    //void OnAudioSent(float[] buffer, int offset, int count);
    void SendAudio(float[] samples, int samplesLength, double samplesMilliseconds, float coeff);
}

public interface IMicrophone
{
    float Level { get; }
    internal void Initialize(IMicrophoneContext microphoneContext);

    void SetVolume(float volume);

    /// <summary>
    ///     Called when recording ends.
    /// </summary>
    internal void Close();
}

public class ManualMicrophone : IMicrophone
{
    // Always 40ms frames (25 packets/sec) — halves packet rate vs mixing 20ms,
    // cutting RTP+WebSocket per-packet overhead in half with negligible latency impact.
    private const int AudioLength = (int)(AudioConstants.ClockRate * 0.040f); // 40ms @ 48kHz = 1920 samples
    private readonly float[] cachedAudio = new float[AudioLength];
    private readonly float[] sampleBuffer = new float[AudioLength];
    private bool _initialized;
    private bool _warnedNull;
    private int cachedLength;
    private IMicrophoneContext context;
    private float level;

    private float volume = 1.0f;

    void IMicrophone.Initialize(IMicrophoneContext microphoneContext)
    {
        context = microphoneContext;
        _initialized = true;
    }

    void IMicrophone.Close()
    {
        context = null;
    }

    void IMicrophone.SetVolume(float volume)
    {
        this.volume = Math.Clamp(volume, 0.0f, 1.0f);
    }

    public float Level => level * volume;

    public void PushAudioData(float[] audioData)
    {
        var max = audioData.Max();
        // Faster decay (0.75f vs old 0.5f) — cuts off silence more aggressively, saving bandwidth
        level -= audioData.Length / (float)AudioConstants.ClockRate * 0.75f;
        if (level < 0f) level = 0f;
        if (max > level) level = max;

        if (cachedLength + audioData.Length >= AudioLength)
        {
            // Discard old data and send the most recent 40ms.
            if (AudioLength > audioData.Length)
            {
                var cLength = AudioLength - audioData.Length;
                cachedAudio.AsSpan(cachedLength - cLength, cLength).CopyTo(sampleBuffer);
                audioData.CopyTo(sampleBuffer.AsSpan(cLength, audioData.Length));
            }
            else
            {
                audioData.AsSpan(audioData.Length - AudioLength, AudioLength).CopyTo(sampleBuffer);
            }

            cachedLength = 0;
        }
        else
        {
            // Not enough data: cache and wait for next call
            audioData.CopyTo(cachedAudio.AsSpan(cachedLength, audioData.Length));
            cachedLength += audioData.Length;
            return;
        }

        // VAD gate DISABLED — same reason as WindowsMicrophone
        if (!_initialized && !_warnedNull)
        {
            _warnedNull = true;
            TheOtherRolesPlugin.Logger.LogWarning(
                "[VC:Mic] ManualMicrophone not initialized — audio will NOT be sent.");
            return;
        }

        context?.SendAudio(sampleBuffer, AudioLength, 40.0, volume);
    }
}

public class WindowsMicrophone : IMicrophone
{
    private readonly int deviceNum;
    private IMicrophoneContext context;
    private float level;
    private float[] sampleBuffer = null!;

    private float volume = 1.0f;

    private WaveInEvent waveIn;

    public WindowsMicrophone(string deviceName)
    {
        var count = WaveInEvent.DeviceCount;
        for (var i = 0; i < count; i++)
            if (WaveInEvent.GetCapabilities(i).ProductName == deviceName)
            {
                deviceNum = i;
                return;
            }

        deviceNum = 0;
    }

    void IMicrophone.Initialize(IMicrophoneContext microphoneContext)
    {
        context = microphoneContext;

        waveIn = new WaveInEvent { BufferMilliseconds = 40, NumberOfBuffers = 3 };
        waveIn.DeviceNumber = deviceNum;
        waveIn.WaveFormat = new WaveFormat(48000, 16, 1);
        waveIn.DataAvailable += SendAudio;
        waveIn.StartRecording();
    }

    void IMicrophone.SetVolume(float volume)
    {
        this.volume = Math.Clamp(volume, 0.0f, 1.0f);
    }

    void IMicrophone.Close()
    {
        context = null;
        waveIn.StopRecording();
    }

    public float Level => level * volume;


    private void SendAudio(object sender, WaveInEventArgs e)
    {
        var samples = e.BytesRecorded / 2;
        if (sampleBuffer == null || sampleBuffer.Length != samples) sampleBuffer = new float[samples];

        var max = 0f;
        for (var i = 0; i < samples; i++)
        {
            sampleBuffer[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;
            var abs = Math.Abs(sampleBuffer[i]);
            if (abs > max) max = abs;
        }

        // Faster decay (0.75f vs old 0.5f) — cuts off silence more aggressively
        level -= samples / (float)AudioConstants.ClockRate * 0.75f;
        if (level < 0f) level = 0f;
        if (max > level) level = max;

        // VAD gate DISABLED — the 0.008f threshold was dropping 70-90% of frames
        // on real hardware. Bandwidth savings are not worth cutting off speech.
        context?.SendAudio(sampleBuffer, samples, waveIn.BufferMilliseconds, volume);
    }
}