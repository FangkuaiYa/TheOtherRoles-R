using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace TheOtherRoles.Voice.Routing.Router;

public class FilterRouter : AbstractAudioRouter
{
    private readonly Func<BiQuadFilter> filterGenerator;

    private FilterRouter(Func<BiQuadFilter> filterGenerator, bool isGlobalRouter = false)
    {
        this.filterGenerator = filterGenerator;
        IsGlobalRouter = isGlobalRouter;
    }

    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;

    /// <summary>
    ///     Creates a low-pass filter that removes high frequencies and passes low frequencies.
    /// </summary>
    /// <param name="cutoffFrequency">Cutoff frequency in Hz.</param>
    /// <param name="qFactor">Q factor (resonance).</param>
    /// <param name="isGlobalRouter">Whether this router is global (shared across clients).</param>
    /// <returns>A new FilterRouter instance.</returns>
    public static FilterRouter CreateLowPassFilter(float cutoffFrequency, float qFactor, bool isGlobalRouter = false)
    {
        return new FilterRouter(() => BiQuadFilter.LowPassFilter(AudioConstants.ClockRate, cutoffFrequency, qFactor),
            isGlobalRouter);
    }

    /// <summary>
    ///     Creates a high-pass filter that removes low frequencies and passes high frequencies.
    /// </summary>
    /// <param name="cutoffFrequency">Cutoff frequency in Hz.</param>
    /// <param name="qFactor">Q factor (resonance).</param>
    /// <param name="isGlobalRouter">Whether this router is global (shared across clients).</param>
    /// <returns>A new FilterRouter instance.</returns>
    public static FilterRouter CreateHighPassFilter(float cutoffFrequency, float qFactor, bool isGlobalRouter = false)
    {
        return new FilterRouter(() => BiQuadFilter.HighPassFilter(AudioConstants.ClockRate, cutoffFrequency, qFactor),
            isGlobalRouter);
    }

    /// <summary>
    ///     Creates a band-pass filter centered at the given frequency.
    /// </summary>
    /// <param name="centerFrequency">Center frequency in Hz.</param>
    /// <param name="qFactor">Q factor (resonance).</param>
    /// <param name="isGlobalRouter">Whether this router is global (shared across clients).</param>
    /// <returns>A new FilterRouter instance.</returns>
    public static FilterRouter CreateBandPassFilter(float centerFrequency, float qFactor, bool isGlobalRouter = false)
    {
        return new FilterRouter(
            () => BiQuadFilter.BandPassFilterConstantPeakGain(AudioConstants.ClockRate, centerFrequency, qFactor),
            isGlobalRouter);
    }

    /// <summary>
    ///     Creates a notch filter that removes a specific frequency band.
    /// </summary>
    /// <param name="centerFrequency">Center frequency in Hz.</param>
    /// <param name="qFactor">Q factor (resonance).</param>
    /// <param name="isGlobalRouter">Whether this router is global (shared across clients).</param>
    /// <returns>A new FilterRouter instance.</returns>
    public static FilterRouter CreateNotchFilter(float centerFrequency, float qFactor, bool isGlobalRouter = false)
    {
        return new FilterRouter(() => BiQuadFilter.NotchFilter(AudioConstants.ClockRate, centerFrequency, qFactor),
            isGlobalRouter);
    }


    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        if (source.WaveFormat.Channels == 2)
            return new FilteredStereoProvider(source, filterGenerator(), filterGenerator());

        return new FilteredMonoProvider(source, filterGenerator());
    }

    public class FilteredMonoProvider : ISampleProvider
    {
        private readonly BiQuadFilter filter;
        private readonly ISampleProvider source;

        internal FilteredMonoProvider(ISampleProvider source, BiQuadFilter filter)
        {
            this.source = source;
            this.filter = filter;
        }

        WaveFormat ISampleProvider.WaveFormat => source.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            var read = source.Read(buffer, offset, count);
            for (var n = 0; n < read; n++) buffer[offset + n] = filter.Transform(buffer[offset + n]);
            return read;
        }
    }

    public class FilteredStereoProvider : ISampleProvider
    {
        private readonly BiQuadFilter filterL;
        private readonly BiQuadFilter filterR;
        private readonly ISampleProvider source;

        internal FilteredStereoProvider(ISampleProvider source, BiQuadFilter filterL, BiQuadFilter filterR)
        {
            this.source = source;
            this.filterL = filterL;
            this.filterR = filterR;
        }

        WaveFormat ISampleProvider.WaveFormat => source.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            var read = source.Read(buffer, offset, count);
            for (var n = 0; n < read; n++)
                if (n % 2 == 0)
                    buffer[offset + n] = filterL.Transform(buffer[offset + n]);
                else
                    buffer[offset + n] = filterR.Transform(buffer[offset + n]);
            return read;
        }
    }
}