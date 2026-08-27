using NAudio.Wave;

namespace TheOtherRoles.Voice.Routing.Router;

public class SimpleEndpoint : AbstractAudioRouter
{
    public SimpleEndpoint()
    {
        IsGlobalRouter = true;
    }

    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => true;

    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        return source;
    }
}