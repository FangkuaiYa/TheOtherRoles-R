using System;
using NAudio.Wave;

namespace TheOtherRoles.Voice.NAudio.Provider;

/// <summary>
///     A BufferedProvider that accepts sample writes.
///     Modified NAudio's BufferedWaveProvider for sample usage.
/// </summary>
internal class BufferedSampleProvider : ISampleProvider
{
    private CircularFloatBuffer circularBuffer;

    public BufferedSampleProvider(WaveFormat waveFormat, int? bufferLength = null)
    {
        this.WaveFormat = waveFormat;
        BufferLength = bufferLength ?? waveFormat.AverageBytesPerSecond * 5;
        ReadFully = true;
    }

    public bool ReadFully { get; set; }
    public int BufferLength { get; set; }

    public int BufferCutSize { get; set; } = int.MaxValue;
    public int BufferCutToSize { get; set; } = int.MaxValue;

    public TimeSpan BufferDuration
    {
        get => TimeSpan.FromSeconds(BufferLength / (double)WaveFormat.AverageBytesPerSecond);
        set => BufferLength = (int)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
    }

    public bool DiscardOnBufferOverflow { get; set; }

    public int BufferedBytes
    {
        get
        {
            if (circularBuffer != null) return circularBuffer.Count;

            return 0;
        }
    }

    public TimeSpan BufferedDuration => TimeSpan.FromSeconds(BufferedBytes / (double)WaveFormat.AverageBytesPerSecond);
    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        var num = 0;
        if (circularBuffer != null) num = circularBuffer.Read(buffer, offset, count);

        if (ReadFully && num < count)
        {
            Array.Clear(buffer, offset + num, count - num);
            num = count;
        }

        return num;
    }

    public void AddSamples(float[] buffer, int offset, int count)
    {
        if (circularBuffer == null) circularBuffer = new CircularFloatBuffer(BufferLength);
        if (circularBuffer.Write(buffer, offset, count) < count && !DiscardOnBufferOverflow)
            throw new InvalidOperationException("Buffer full");

        if (circularBuffer.Count > BufferCutSize && BufferCutSize > BufferCutToSize)
            circularBuffer.Discard(circularBuffer.Count - BufferCutToSize);
    }

    public void ClearBuffer()
    {
        if (circularBuffer != null) circularBuffer.Reset();
    }
}