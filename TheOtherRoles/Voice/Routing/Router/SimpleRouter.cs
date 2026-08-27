using NAudio.Wave;

namespace TheOtherRoles.Voice.Routing.Router;

public class SimpleRouter : AbstractAudioRouter
{
    public SimpleRouter(bool isGlobalRouter = false)
    {
        IsGlobalRouter = isGlobalRouter;
    }

    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;

    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        return source;
    }
}