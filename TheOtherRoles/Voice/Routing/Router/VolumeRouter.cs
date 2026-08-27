using NAudio.Wave;

namespace TheOtherRoles.Voice.Routing.Router;

public class VolumeRouter : AbstractAudioNodeProvider<VolumeRouter.Property>
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

        public float Volume { get; set; }

        WaveFormat ISampleProvider.WaveFormat => sourceProvider.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            if (Volume > 0f)
            {
                var read = sourceProvider.Read(buffer, offset, count);
                if (Volume != 1.0f)
                    for (var i = 0; i < count; i++)
                        buffer[offset + i] *= Volume;

                return read;
            }

            for (var i = 0; i < count; i++) buffer[offset + i] = 0f;
            return count;
        }
    }
}