using System;
using NAudio.Wave;

namespace TheOtherRoles.Voice.Routing.Node;

/// <summary>
///     Audio data buffer for nodes read by multiple downstream nodes.
/// </summary>
internal class AudioBuffer : ISampleProvider
{
    private readonly ISampleProvider source;
    private float[] buffer;
    private int length;
    private float[] temp;

    public AudioBuffer(ISampleProvider source, int groupId)
    {
        this.source = source;
        this.GroupId = groupId;
    }

    public int GroupId { get; } = -1;


    public WaveFormat WaveFormat => source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        if (this.buffer == null)
        {
            if (temp != null && temp.Length >= count) this.buffer = temp;
            else temp = this.buffer = new float[count];

            var actualCount = source.Read(this.buffer, 0, count);
            if (actualCount < count) Array.Clear(this.buffer, actualCount, count - actualCount);
            length = count;
        }

        if (count != length)
            throw new InvalidOperationException("The count must be consistent across all calls in the sequence.");

        Buffer.BlockCopy(this.buffer, 0, buffer, offset * 4, count * 4);
        return count;
    }

    public void Clear()
    {
        buffer = null;
    }
}