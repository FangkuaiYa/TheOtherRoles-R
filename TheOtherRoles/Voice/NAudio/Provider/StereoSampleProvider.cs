using System;
using NAudio.Wave;

namespace TheOtherRoles.Voice.NAudio.Provider;

internal class StereoSampleProvider : ISampleProvider
{
    private readonly object panLock = new();
    private readonly ISampleProvider sourceProvider;
    private float[] lastBuffer = null!;
    private int lastBufferCount;
    private int lastLDelay;
    private int lastRDelay;

    private float pan;
    private float[] tempBuffer = null!;

    public StereoSampleProvider(ISampleProvider sourceProvider)
    {
        this.sourceProvider = sourceProvider;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceProvider.WaveFormat.SampleRate, 2);
    }

    public float Pan
    {
        get
        {
            lock (panLock)
            {
                return pan;
            }
        }
        set
        {
            lock (panLock)
            {
                pan = Math.Clamp(value, -1.0f, 1.0f);
            }
        }
    } // -1.0 (left) to 1.0 (right)

    public float Volume { get; set; } = 1.0f;
    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        if (tempBuffer == null || tempBuffer.Length < count / 2) tempBuffer = new float[count / 2];
        var tempLength = sourceProvider.Read(tempBuffer, 0, count / 2);

        var monoCount = count / 2;
        var pan = Pan;
        var lCoeff = pan < 0 ? 0 : pan;
        var rCoeff = pan < 0 ? -pan : 0;
        var lDelay = (int)(lCoeff * 50);
        var rDelay = (int)(rCoeff * 50);
        var lVol = (1.0f - lCoeff * 0.3f) * Volume;
        var rVol = (1.0f - rCoeff * 0.3f) * Volume;
        var lCount = monoCount - lDelay + lastLDelay;
        var rCount = monoCount - rDelay + lastRDelay;

        for (var i = 0; i < monoCount; i++)
        {
            var lIndex = i * lCount / monoCount; // Index including delay offset from lastBuffer
            if (lIndex < lastLDelay)
                buffer[offset + i * 2] = lastBuffer[lastBufferCount - lastLDelay + lIndex] * lVol;
            else
                buffer[offset + i * 2] = tempBuffer[lIndex - lastLDelay] * lVol;

            var rIndex = i * rCount / monoCount; // Index including delay offset from lastBuffer
            if (rIndex < lastRDelay)
                buffer[offset + i * 2 + 1] = lastBuffer[lastBufferCount - lastRDelay + rIndex] * rVol;
            else
                buffer[offset + i * 2 + 1] = tempBuffer[rIndex - lastRDelay] * rVol;
        }

        lastLDelay = lDelay;
        lastRDelay = rDelay;

        // Swap arrays
        var temp = lastBuffer;
        lastBuffer = tempBuffer;
        tempBuffer = temp;
        lastBufferCount = tempLength;

        return count;
    }
}