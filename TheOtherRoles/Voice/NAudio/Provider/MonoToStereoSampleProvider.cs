using NAudio.Wave;

namespace TheOtherRoles.Voice.NAudio.Provider;

internal class MonoToStereoSampleProvider : ISampleProvider
{
    private static readonly WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(AudioConstants.ClockRate, 2);
    private readonly ISampleProvider sourceProvider;

    public MonoToStereoSampleProvider(ISampleProvider monoProvider)
    {
        sourceProvider = monoProvider;
    }

    public WaveFormat WaveFormat => waveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        sourceProvider.Read(buffer, offset, count / 2);
        for (var i = count / 2 - 1; i >= 0; i--)
        {
            buffer[offset + i * 2] = buffer[offset + i];
            buffer[offset + i * 2 + 1] = buffer[offset + i];
        }

        return count;
    }
}