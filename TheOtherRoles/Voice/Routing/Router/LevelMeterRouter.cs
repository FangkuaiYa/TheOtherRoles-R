using NAudio.Wave;

namespace TheOtherRoles.Voice.Routing.Router;

public class LevelMeterRouter : AbstractAudioNodeProvider<LevelMeterRouter.Property>
{
    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;

    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        return new Property(source);
    }

    public class Property : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;

        internal Property(ISampleProvider source)
        {
            sourceProvider = source;
        }

        public float Decay { get; set; } = 0.5f;
        public float Level { get; private set; }
        WaveFormat ISampleProvider.WaveFormat => sourceProvider.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            var read = sourceProvider.Read(buffer, offset, count);
            Level -= Decay * (count / (float)AudioConstants.ClockRate);
            if (Level < 0.0f) Level = 0.0f;
            for (var i = 0; i < read; i++)
                if (Level < buffer[offset + i])
                    Level = buffer[offset + i];
            if (Level > 1.0f) Level = 1.0f;
            return read;
        }
    }
}