using System;
using InnerNet;
using TheOtherRoles.Voice.Game;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Voice;

internal static class TorVoiceRoomDriver
{
    private static bool _wasInIntro;
    private static bool _wasInEndGame;
    private static bool _lastVcEnabled;

    private static bool IsLocalServer()
    {
        var addr = AmongUsClient.Instance?.networkAddress;
        return addr is "127.0.0.1" or "localhost";
    }

    internal static void Update()
    {
        var vcEnabled = CustomOptionHolder.vcEnableVoiceChat.getBool();

        // When VC is toggled ON mid-game, reset the prompt so player can choose
        if (vcEnabled && !_lastVcEnabled) VoiceJoinPrompt.Reset();
        _lastVcEnabled = vcEnabled;

        var shouldNotUseVC = AmongUsClient.Instance == null
                             || (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Joined
                                 && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
                             || IsLocalServer()
                             || !vcEnabled;

        if (shouldNotUseVC)
        {
            if (VoiceRoom.Current != null)
                VoiceRoom.CloseCurrentRoom();
            _wasInIntro = _wasInEndGame = false;
            VoiceServerState.Reset();
            return;
        }

        // Voice is enabled but player hasn't answered the prompt yet
        if (!VoiceJoinPrompt.HasAnswered)
        {
            VoiceJoinPrompt.Update();
            return;
        }

        // Player declined voice chat
        if (!VoiceJoinPrompt.HasJoinedVoice)
            return;

        // Player accepted — join voice room
        if (VoiceRoom.Current == null)
        {
            var region = AmongUsClient.Instance!.networkAddress;
            var roomId = AmongUsClient.Instance.GameId.ToString();
            VoiceRoom.Start(region, roomId);
            TorVoiceHudState.ApplyMicState();
            TorVoiceHudState.ApplySpeakerState();

            if (AmongUsClient.Instance.AmHost)
            {
                VoiceConfig.ApplyLocalHostSettingsToSynced();
                TorVoiceHudState.MarkRoomSettingsDirty();
            }

            VoiceRoom.Current!.ForceUpdateLocalProfile();
        }

        if (VoiceRoom.Current == null) return;

        // IntroCutscene ended → Rejoin to re-sync profiles
        var inIntro = IntroCutscene.Instance != null;
        if (_wasInIntro && !inIntro)
        {
            foreach (var c in VoiceRoom.Current.AllClients)
                c.ResetMapping();
            VoiceRoom.Current.ForceUpdateLocalProfile();
            TheOtherRolesPlugin.Logger.LogInfo("[VC] IntroCutscene ended: mappings reset, profile re-broadcast.");
        }

        _wasInIntro = inIntro;

        // EndGame started → Rejoin
        var inEndGame = Object.FindObjectOfType<EndGameManager>() != null;
        if (inEndGame && !_wasInEndGame)
        {
            VoiceRoom.Current.Rejoin();
            VoiceRoom.Current.ForceUpdateLocalProfile();
            TheOtherRolesPlugin.Logger.LogInfo("[VC] EndGame: room rejoined.");
        }

        _wasInEndGame = inEndGame;

        TorVoiceHudState.TrySyncHostRoomSettings();
        TorVoiceHudState.TrySyncPublicLobby();

        try
        {
            VoiceRoom.Current.Update();
        }
        catch (Exception ex)
        {
            TheOtherRolesPlugin.Logger.LogError("[VC] Room update error: " + ex);
        }
    }
}