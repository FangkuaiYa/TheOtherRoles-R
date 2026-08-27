using System;
using NAudio.Wave;
using TheOtherRoles.Voice.NAudio.Provider;

namespace TheOtherRoles.Voice.Routing.Router;

/// <summary>
///     Audio router that adds a delay/reverb effect.
/// </summary>
public class ReverbRouter : AbstractAudioNodeProvider<ReverbRouter.Property>
{
    private readonly int delayMilliseconds;

    public ReverbRouter(int delayMilliseconds, float defaultDecay = 0.3f, float defaultWetDryMix = 0.5f)
    {
        if (delayMilliseconds < 1) throw new ArgumentOutOfRangeException("delayMilliseconds must be greater than 0.");
        if (defaultDecay < 0.0f || defaultDecay > 1.0f)
            throw new ArgumentOutOfRangeException("defaultDecay must be between 0.0 and 1.0.");
        if (defaultWetDryMix < 0.0f || defaultWetDryMix > 1.0f)
            throw new ArgumentOutOfRangeException("defaultWetDryMix must be between 0.0 and 1.0.");
        this.delayMilliseconds = delayMilliseconds;
        this.defaultDecay = defaultDecay;
        this.defaultWetDryMix = defaultWetDryMix;
    }

    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;
    private float defaultDecay { get; } = 0.3f;
    private float defaultWetDryMix { get; } = 0.5f;

    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        return new Property(source, delayMilliseconds, defaultDecay, defaultWetDryMix);
    }

    public class Property : ISampleProvider
    {
        private readonly ReverbSampleProvider sampleProvider;

        internal Property(ISampleProvider source, int delayMilliseconds, float decay, float wetDryMix)
        {
            sampleProvider = new ReverbSampleProvider(source, delayMilliseconds, decay, wetDryMix);
        }

        public float Decay
        {
            get => sampleProvider.Decay;
            set => sampleProvider.Decay = value;
        }

        public float WetDryMix
        {
            get => sampleProvider.WetDryMix;
            set => sampleProvider.WetDryMix = value;
        }

        WaveFormat ISampleProvider.WaveFormat => sampleProvider.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            return sampleProvider.Read(buffer, offset, count);
        }
    }
}