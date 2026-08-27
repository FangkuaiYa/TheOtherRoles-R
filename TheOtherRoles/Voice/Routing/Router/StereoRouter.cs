using System.IO;
using NAudio.Wave;
using TheOtherRoles.Voice.NAudio.Provider;

namespace TheOtherRoles.Voice.Routing.Router;

/// <summary>
///     Router that converts mono audio to spatial stereo.
///     Pan and volume can be adjusted.
/// </summary>
public class StereoRouter : AbstractAudioNodeProvider<StereoRouter.Property>
{
    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;
    internal override int OutputChannels => 2;

    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        if (source.WaveFormat.Channels == 2)
            throw new InvalidDataException("StereoRouter can only be connected after the mono input.");
        return new Property(source);
    }

    public class Property : ISampleProvider
    {
        private readonly StereoSampleProvider sampleProvider;

        internal Property(ISampleProvider source)
        {
            sampleProvider = new StereoSampleProvider(source);
        }

        public float Volume
        {
            get => sampleProvider.Volume;
            set => sampleProvider.Volume = value;
        }

        public float Pan
        {
            get => sampleProvider.Pan;
            set => sampleProvider.Pan = value;
        }

        WaveFormat ISampleProvider.WaveFormat => sampleProvider.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            return sampleProvider.Read(buffer, offset, count);
        }
    }
}