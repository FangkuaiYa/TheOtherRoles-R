using NAudio.Wave;

namespace TheOtherRoles.Voice.Routing.Router;

public class DistortionFilter : AbstractAudioNodeProvider<DistortionFilter.Property>
{
    public float DefaultThreshold { get; set; } = 1f;
    public bool DefaultAmplification { get; set; } = false;
    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;

    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        return new Property(source) { Threshold = DefaultThreshold, Amplification = DefaultAmplification };
    }

    public class Property : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;

        internal Property(ISampleProvider source)
        {
            sourceProvider = source;
        }

        public float Threshold { get; set; }
        public bool Amplification { get; set; }

        WaveFormat ISampleProvider.WaveFormat => sourceProvider.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            var read = sourceProvider.Read(buffer, offset, count);
            if (Amplification)
            {
                var amp = 1f / Threshold;
                for (var i = 0; i < read; i++)
                    if (buffer[offset + i] > Threshold)
                        buffer[offset + i] = 1f;
                    else if (buffer[offset + i] < -Threshold)
                        buffer[offset + i] = -1f;
                    else
                        buffer[offset + i] *= amp;
            }
            else
            {
                for (var i = 0; i < read; i++)
                    if (buffer[offset + i] > Threshold)
                        buffer[offset + i] = Threshold;
                    else if (buffer[offset + i] < -Threshold)
                        buffer[offset + i] = -Threshold;
            }

            return read;
        }
    }
}