using System;
using NAudio.Wave;

namespace TheOtherRoles.Voice.NAudio.Provider;

/// <summary>
///     SampleProvider that adds a reverb effect.
/// </summary>
internal class ReverbSampleProvider : ISampleProvider
{
    private readonly float[] delayBuffer;
    private readonly ISampleProvider sourceProvider;
    private float decay;
    private int delayPosition;
    private float dryMix; // Dry (original) mix level
    private float wetMix; // Reverb wet mix level

    /// <summary>
    ///     Adds a reverb/delay effect to the audio signal.
    /// </summary>
    /// <param name="sourceProvider">Input audio source.</param>
    /// <param name="delayMilliseconds">Delay in milliseconds.</param>
    /// <param name="decay">Decay factor (0.0 - 1.0).</param>
    /// <param name="wetDryMix">Mix balance (0.0 = dry only, 1.0 = wet only).</param>
    public ReverbSampleProvider(ISampleProvider sourceProvider, int delayMilliseconds, float decay, float wetDryMix)
    {
        this.sourceProvider = sourceProvider;
        var delaySamples = (int)(WaveFormat.SampleRate * (delayMilliseconds / 1000.0f)) * WaveFormat.Channels;
        delayBuffer = new float[delaySamples];
        this.decay = decay;
        wetMix = wetDryMix;
        dryMix = 1.0f - wetDryMix;
    }

    public float Decay
    {
        get => decay;
        set => decay = Math.Clamp(value, 0.0f, 1.0f);
    }

    public float WetDryMix
    {
        get => wetMix;
        set
        {
            wetMix = Math.Clamp(value, 0.0f, 1.0f);
            dryMix = 1.0f - wetMix;
        }
    }

    public WaveFormat WaveFormat => sourceProvider.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var samplesRead = sourceProvider.Read(buffer, offset, count);

        for (var i = 0; i < samplesRead; i++)
        {
            var currentSample = buffer[offset + i];
            var delayedSample = delayBuffer[delayPosition];

            delayBuffer[delayPosition] = currentSample + delayedSample * decay;
            buffer[offset + i] = currentSample * dryMix + delayedSample * wetMix;

            delayPosition = (delayPosition + 1) % delayBuffer.Length;
        }

        return samplesRead;
    }
}