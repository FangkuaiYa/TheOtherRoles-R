using System;
using System.Collections.Generic;
using TheOtherRoles.Voice.NAudio.Provider;
using TheOtherRoles.Voice.Routing.Node;

namespace TheOtherRoles.Voice.Routing;

public class AudioRoutingInstance : IHasAudioPropertyNode
{
    private readonly AudioRoutingInstanceNode[] nodes;
    private readonly BufferedSampleProvider sourceProvider;

    private long LastReceiptTime = DateTime.Now.Ticks;
    private List<AudioBuffer> buffers = [];

    internal AudioRoutingInstance(List<AudioBuffer> buffers, AudioRoutingInstanceNode[] nodes,
        BufferedSampleProvider sourceProvider, int clientId)
    {
        ClientId = clientId;
        this.buffers = buffers;
        this.nodes = nodes;
        this.sourceProvider = sourceProvider;
    }

    public int ClientId { get; private init; }
    public int ElapsedSinceLastReceipt => (int)((DateTime.Now.Ticks - LastReceiptTime) / 10000); // milliseconds

    AudioRoutingInstanceNode IHasAudioPropertyNode.GetProperty(int propertyId)
    {
        return nodes[propertyId];
    }

    public void AddSamples(float[] samples, int offset, int count)
    {
        sourceProvider.AddSamples(samples, offset, count);
        LastReceiptTime = DateTime.Now.Ticks;
    }
}