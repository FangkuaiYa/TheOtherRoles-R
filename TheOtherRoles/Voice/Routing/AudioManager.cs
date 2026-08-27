using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using TheOtherRoles.Voice.NAudio.Provider;
using TheOtherRoles.Voice.Routing.Node;

namespace TheOtherRoles.Voice.Routing;

internal class AudioManager : IHasAudioPropertyNode
{
    private readonly int bufferMaxLength;
    private readonly List<AudioBuffer> buffers = [];
    private readonly int nodeLength;
    private readonly AbstractAudioRouter router;

    private int bufferLength;
    private AudioRoutingInstanceNode[] globalNodes;

    internal AudioManager(AbstractAudioRouter audioRouter, int bufferLength = 2048, int bufferMaxLength = 4096)
    {
        this.bufferLength = bufferLength;
        this.bufferMaxLength = bufferMaxLength;
        router = audioRouter;
        nodeLength = FixStructure();
        GenerateGlobalNodes();
        if (Endpoint == null) throw new InvalidOperationException("No endpoint found in the audio routing structure.");
    }

    public ISampleProvider Endpoint { get; private set; }

    AudioRoutingInstanceNode IHasAudioPropertyNode.GetProperty(int propertyId)
    {
        return globalNodes[propertyId];
    }

    private int FixStructure()
    {
        var availableId = 0;

        void SetChannelStereo(AbstractAudioRouter router)
        {
            router.Channels = 2;
            foreach (var c in router.GetChildRouters()) SetChannelStereo(c);
        }

        void SetId(AbstractAudioRouter router, bool shouldBeGivenStereoChannel, bool inGlobalRoute)
        {
            if (router.Id == -1)
            {
                router.Id = availableId++;
                if (router.IsGlobalRouter && !inGlobalRoute) router.HasMultipleInput = true;
                if (shouldBeGivenStereoChannel || router.ShouldBeGivenStereoInput) router.Channels = 2;
                foreach (var c in router.GetChildRouters())
                    SetId(c, shouldBeGivenStereoChannel || router.OutputChannels == 2,
                        router.IsGlobalRouter || inGlobalRoute);
            }
            else
            {
                router.HasMultipleInput = true;
                if (router.Channels == 1 && shouldBeGivenStereoChannel) SetChannelStereo(router);
            }
        }

        SetId(router, false, false);
        return availableId;
    }

    internal void GenerateGlobalNodes()
    {
        globalNodes = new AudioRoutingInstanceNode[nodeLength];

        void GenerateInner(AbstractAudioRouter router, ISampleProvider parent, bool isInGlobalArea)
        {
            if (router.IsGlobalRouter)
            {
                if (globalNodes[router.Id] == null)
                {
                    globalNodes[router.Id] = new AudioRoutingInstanceNode(buffers, parent!, router.GenerateProcessor,
                        router.HasMultipleInput, router.HasMultipleOutput, router.Channels, -1);
                    if (router.IsEndpoint)
                    {
                        var processor = globalNodes[router.Id].Processor;
                        Endpoint = new SampleProviderWrapper(processor, this);
                    }
                }
                else if (parent != null)
                {
                    globalNodes[router.Id].AddInput(parent!, -1);
                }
            }
            else
            {
                if (isInGlobalArea)
                    throw new InvalidDataException("A non-global router cannot be a child of a global router.");
            }

            foreach (var c in router.GetChildRouters())
                GenerateInner(c, globalNodes[router.Id]?.Processor, router.IsGlobalRouter);
        }

        GenerateInner(router, null, false);
    }

    public AudioRoutingInstance Generate(int groupId)
    {
        var nodes = new AudioRoutingInstanceNode[globalNodes.Length];
        Array.Copy(globalNodes, nodes, globalNodes.Length);

        void GenerateInner(AbstractAudioRouter router, ISampleProvider parent)
        {
            if (nodes[router.Id] == null)
                nodes[router.Id] = new AudioRoutingInstanceNode(buffers, parent!, router.GenerateProcessor,
                    router.HasMultipleInput, router.HasMultipleOutput, router.Channels, groupId);
            else if (parent != null) nodes[router.Id].AddInput(parent, groupId);
            if (!router.IsGlobalRouter)
                foreach (var c in router.GetChildRouters())
                    GenerateInner(c, nodes[router.Id]?.Processor);
        }

        BufferedSampleProvider sourceProvider =
            new(WaveFormat.CreateIeeeFloatWaveFormat(AudioConstants.ClockRate, 1), bufferMaxLength)
                { DiscardOnBufferOverflow = true };
        GenerateInner(router, sourceProvider);
        return new AudioRoutingInstance(buffers, nodes, sourceProvider, groupId);
    }


    public void Remove(int clientId)
    {
        foreach (var node in globalNodes) node?.RemoveInput(clientId);
        buffers.RemoveAll(b => b.GroupId == clientId);
    }

    private class SampleProviderWrapper : ISampleProvider
    {
        private readonly AudioManager manager;
        private readonly ISampleProvider source;

        public SampleProviderWrapper(ISampleProvider source, AudioManager manager)
        {
            this.source = source;
            this.manager = manager;
        }

        WaveFormat ISampleProvider.WaveFormat => source.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            foreach (var b in manager.buffers) b.Clear();
            return source.Read(buffer, offset, count);
        }
    }
}