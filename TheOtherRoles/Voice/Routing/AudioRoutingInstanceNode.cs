using System;
using System.Collections.Generic;
using NAudio.Wave;
using TheOtherRoles.Voice.NAudio.Provider;
using TheOtherRoles.Voice.Routing.Node;

namespace TheOtherRoles.Voice.Routing;

internal class AudioRoutingInstanceNode
{
    private readonly AudioBuffer buffer;
    private readonly AudioMixer mixer;

    public AudioRoutingInstanceNode(List<AudioBuffer> bufferList, ISampleProvider source,
        Func<ISampleProvider, ISampleProvider> constructor, bool hasMultipleInput, bool hasMultipleOutput, int channels,
        int groupId)
    {
        if (hasMultipleInput)
        {
            mixer = new AudioMixer(channels);
            if (source != null) mixer.AddInput(source, -1);
        }
        else
        {
            mixer = null;
            if (source.WaveFormat.Channels == 1 && channels == 2) source = new MonoToStereoSampleProvider(source);
        }

        Processor = constructor(mixer ?? source);
        if (hasMultipleOutput)
        {
            buffer = new AudioBuffer(Processor, groupId);
            bufferList.Add(buffer);
        }
        else
        {
            buffer = null;
        }
    }

    internal ISampleProvider Output => buffer ?? Processor;
    internal ISampleProvider Processor { get; }

    internal void AddInput(ISampleProvider input, int groupId)
    {
        mixer?.AddInput(input, groupId);
    }

    internal void RemoveInput(int groupId)
    {
        mixer?.RemoveInput(groupId);
    }
}