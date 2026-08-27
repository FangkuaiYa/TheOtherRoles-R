using HarmonyLib;

namespace TheOtherRoles.Voice.Game.UI;

[HarmonyPatch(typeof(PassiveButtonManager), nameof(PassiveButtonManager.Update))]
public static class VCInputBlockPatch
{
    public static bool IsAnyVoiceWindowOpen =>
        (VoiceSettingsWindow.Instance != null && VoiceSettingsWindow.Instance.ShowWindow)
        || (PublicLobbyWindow.Instance != null && PublicLobbyWindow.Instance.ShowWindow)
        || (PlayerVolumeWindow.Instance != null && PlayerVolumeWindow.Instance.ShowWindow);

    // Returning false skips the original method entirely for this frame.
    private static bool Prefix()
    {
        return !IsAnyVoiceWindowOpen;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
public static class VCCanMoveBlockPatch
{
    private static void Postfix(ref bool __result)
    {
        if (VCInputBlockPatch.IsAnyVoiceWindowOpen) __result = false;
    }
}