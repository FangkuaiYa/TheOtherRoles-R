using HarmonyLib;
using UnityEngine;

namespace TheOtherRoles.Voice.Game;

[HarmonyPatch]
public static class VoiceChatPatches
{
    // Windows keyboard shortcuts (Nebula uses VirtualInput, we use KeyboardJoystick)
    [HarmonyPostfix]
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    private static void KeyboardUpdate_Post()
    {
        if (Input.GetKeyDown(KeyCode.M))
            TorVoiceHudState.CycleMicPublic();
        if (Input.GetKeyDown(KeyCode.N))
            TorVoiceHudState.ToggleSpeakerPublic();
    }
}