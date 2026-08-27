using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace TheOtherRoles.Voice.Routing;

public abstract class AbstractAudioRouter
{
    private readonly List<AbstractAudioRouter> children = [];
    internal int Channels = 1;
    internal int Id { get; set; } = -1;
    internal bool HasMultipleInput { get; set; } = false;
    internal bool HasMultipleOutput => children.Count() > 1;
    protected internal abstract bool ShouldBeGivenStereoInput { get; }
    public bool IsGlobalRouter { get; set; } = false;
    protected internal abstract bool IsEndpoint { get; }
    internal virtual int OutputChannels => Channels;

    internal IEnumerable<AbstractAudioRouter> GetChildRouters()
    {
        return children;
    }

    public void Connect(AbstractAudioRouter child)
    {
        if (child.Id != -1 || Id != -1) throw new InvalidOperationException("Cannot use a finalized component.");
        children.Add(child);
    }

    internal abstract ISampleProvider GenerateProcessor(ISampleProvider source);
}

public interface IHasAudioPropertyNode
{
    internal AudioRoutingInstanceNode GetProperty(int propertyId);
}

public abstract class AbstractAudioNodeProvider<AudioProperty> : AbstractAudioRouter
    where AudioProperty : class, ISampleProvider
{
    public AudioProperty GetProperty(IHasAudioPropertyNode nodeHolder)
    {
        return (nodeHolder.GetProperty(Id).Processor as AudioProperty)!;
    }
}