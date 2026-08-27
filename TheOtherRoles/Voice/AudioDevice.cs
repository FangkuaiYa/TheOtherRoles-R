using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TheOtherRoles.Voice;

public static class AudioDevices
{
    public static string DefaultSpeaker => new MMDeviceEnumerator()
        .GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications).FriendlyName;

    public static IEnumerable<string> MicrophoneDevices()
    {
        var count = WaveInEvent.DeviceCount;
        for (var i = 0; i < count; i++) yield return WaveInEvent.GetCapabilities(i).ProductName;
    }

    public static IEnumerable<string> SpeakerDevices()
    {
        foreach (var device in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            yield return device.FriendlyName;
    }
}