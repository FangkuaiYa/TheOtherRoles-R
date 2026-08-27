using System;
using System.Collections.Generic;
using TheOtherRoles.Voice.Routing;
using TheOtherRoles.Voice.Routing.Router;
using TheOtherRoles.Voice.Voice;
using UnityEngine;

namespace TheOtherRoles.Voice.Game;

public class VCPlayer
{
    private readonly StereoRouter.Property _imager;
    private readonly LevelMeterRouter.Property _levelMeter;
    private readonly VolumeRouter.Property _normalVolume, _ghostVolume, _radioVolume, _clientVolume;
    private readonly VoiceRoom _room;
    private PlayerControl _mappedPlayer;

    private float _wallCoeff = 1f;

    public VCPlayer(
        VoiceRoom room,
        AudioRoutingInstance instance,
        StereoRouter imager,
        VolumeRouter normalVolume,
        VolumeRouter ghostVolume,
        VolumeRouter radioVolume,
        VolumeRouter clientVolume,
        LevelMeterRouter levelMeter)
    {
        _room = room;
        ClientId = instance.ClientId;
        _imager = imager.GetProperty(instance);
        _normalVolume = normalVolume.GetProperty(instance);
        _ghostVolume = ghostVolume.GetProperty(instance);
        _radioVolume = radioVolume.GetProperty(instance);
        _clientVolume = clientVolume.GetProperty(instance);
        _levelMeter = levelMeter.GetProperty(instance);
        _clientVolume.Volume = 1f;
        MuteAll();
    }

    public string PlayerName { get; private set; } = "Unknown";

    public byte PlayerId { get; private set; } = byte.MaxValue;

    public int ClientId { get; }

    public float Volume => _clientVolume.Volume;
    public float Level => _levelMeter.Level;
    public bool IsMapped => _mappedPlayer != null && _mappedPlayer;
    public bool IsAudible => _normalVolume.Volume > 0f || _ghostVolume.Volume > 0f || _radioVolume.Volume > 0f;

    // ----- Shadow / wall detection -----
    // Uses the exact same method + mask as Among Us's own vision checks
    // (see NoShadowBehaviour, Console, etc.).

    /// <summary>
    ///     Returns true when a real wall/shadow collider stands between
    ///     <paramref name="source" /> and <paramref name="target" />.
    /// </summary>
    private static bool HasShadowBetween(Vector2 source, Vector2 target)
    {
        return PhysicsHelpers.AnythingBetween(source, target, Constants.ShadowMask, false);
    }

    public void UpdateProfile(byte playerId, string playerName)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        _mappedPlayer = null;
        MuteAll();
    }

    public void ResetMapping()
    {
        _mappedPlayer = null;
        MuteAll();
    }

    private void CheckMapping()
    {
        if (_mappedPlayer != null && _mappedPlayer && _mappedPlayer.PlayerId == PlayerId) return;
        _mappedPlayer = null;
        if (PlayerId == byte.MaxValue) return;
        foreach (var p in PlayerControl.AllPlayerControls.ToArray())
            if (p.PlayerId == PlayerId)
            {
                _mappedPlayer = p;
                break;
            }
    }

    public void SetVolume(float v)
    {
        _clientVolume.Volume = v;
    }

    private void MuteAll()
    {
        _normalVolume.Volume = 0f;
        _ghostVolume.Volume = 0f;
        _radioVolume.Volume = 0f;
    }

    public void UpdateLobby()
    {
        CheckMapping();
        _imager.Pan = 0f;
        _normalVolume.Volume = 1f;
        _ghostVolume.Volume = 0f;
        _radioVolume.Volume = 0f;
    }

    public void UpdateMeeting()
    {
        CheckMapping();
        if (!IsMapped)
        {
            MuteAll();
            return;
        }

        var s = VoiceConfig.SyncedRoomSettings;
        var localDead = PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data?.IsDead == true;
        var targetDead = _mappedPlayer!.Data?.IsDead == true;

        _imager.Pan = 0f;

        if (s.OnlyGhostsCanTalk && !localDead)
        {
            MuteAll();
            return;
        }

        if (localDead)
        {
            _normalVolume.Volume = targetDead ? 0f : 1f;
            _ghostVolume.Volume = targetDead ? 1f : 0f;
            _radioVolume.Volume = 0f;
            return;
        }

        // Channel-based mute: sender is in a private channel but listener doesn't match
        var senderChannel = VoiceChannelHelper.GetPlayerChannel(_mappedPlayer.PlayerId);
        if (senderChannel != VoiceChannel.All)
        {
            var localImp = PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == true;
            var localLover = Lovers.enableChat &&
                             (Lovers.lover1 == PlayerControl.LocalPlayer || Lovers.lover2 == PlayerControl.LocalPlayer);
            var localJackal = Jackal.jackal == PlayerControl.LocalPlayer ||
                              Sidekick.sidekick == PlayerControl.LocalPlayer;
            var localSheriff = Sheriff.sheriff == PlayerControl.LocalPlayer ||
                               Deputy.deputy == PlayerControl.LocalPlayer;

            if (!VoiceChannelHelper.CanHearChannel(senderChannel, localImp, localLover, localJackal, localSheriff))
            {
                MuteAll();
                return;
            }
        }

        _normalVolume.Volume = targetDead ? 0f : 1f;
        _ghostVolume.Volume = 0f;
        _radioVolume.Volume = 0f;
    }

    private static float CalcWallCoeff(Vector2 listener, Vector2 speaker, ref float coeff, VoiceRoomSettings s)
    {
        if (!s.WallsBlockSound)
        {
            coeff = 1f;
            return 1f;
        }

        var hasWall = HasShadowBetween(listener, speaker);
        coeff = coeff + ((hasWall ? 0f : 1f) - coeff) * Math.Clamp(Time.deltaTime * 4f, 0f, 1f);
        return coeff;
    }

    internal void UpdateTaskPhase(
        Vector2? listenerPos,
        IEnumerable<VoiceRoom.SpeakerCache> speakers,
        IEnumerable<IVoiceComponent> virtualMics,
        bool localInVent,
        bool commsSabActive)
    {
        CheckMapping();
        if (!IsMapped || !listenerPos.HasValue)
        {
            MuteAll();
            return;
        }

        var s = VoiceConfig.SyncedRoomSettings;
        var targetPos = (Vector2)_mappedPlayer!.transform.position;
        var localDead = PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data?.IsDead == true;
        var targetDead = _mappedPlayer.Data?.IsDead == true;
        var localImp = PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == true;
        var targetInVent = _mappedPlayer.inVent;

        if (s.OnlyMeetingOrLobby)
        {
            MuteAll();
            return;
        }

        if (s.OnlyGhostsCanTalk && !localDead)
        {
            MuteAll();
            return;
        }

        if (commsSabActive && s.CommsSabDisables && !localImp && !localDead)
        {
            MuteAll();
            return;
        }

        var dist = Vector2.Distance(targetPos, listenerPos.Value);
        var maxDist = s.MaxChatDistance;
        if (s.OnlyHearInSight && !localImp && ShipStatus.Instance != null && PlayerControl.LocalPlayer != null &&
            PlayerControl.LocalPlayer.Data != null)
            maxDist = Mathf.Max(ShipStatus.Instance.CalculateLightRadius(PlayerControl.LocalPlayer.Data) + 0.5f, 1f);
        var volume = VoiceRoom.GetVolume(dist, maxDist);
        var pan = VoiceRoom.GetPan(listenerPos.Value.x, targetPos.x);

        if (localDead)
        {
            if (targetDead)
            {
                _normalVolume.Volume = 0f;
                _ghostVolume.Volume = 1f;
                _radioVolume.Volume = 0f;
                _imager.Pan = 0f;
            }
            else
            {
                _normalVolume.Volume = volume * CalcWallCoeff(listenerPos.Value, targetPos, ref _wallCoeff, s);
                _ghostVolume.Volume = 0f;
                _radioVolume.Volume = 0f;
                _imager.Pan = pan;
            }

            return;
        }

        // Channel-based mute: if sender is in a private channel and listener can't hear that channel, mute
        var senderChannel = VoiceChannelHelper.GetPlayerChannel(_mappedPlayer.PlayerId);
        if (senderChannel != VoiceChannel.All)
        {
            var localLover = Lovers.enableChat &&
                             (Lovers.lover1 == PlayerControl.LocalPlayer || Lovers.lover2 == PlayerControl.LocalPlayer);
            var localJackal = Jackal.jackal == PlayerControl.LocalPlayer ||
                              Sidekick.sidekick == PlayerControl.LocalPlayer;
            var localSheriff = Sheriff.sheriff == PlayerControl.LocalPlayer ||
                               Deputy.deputy == PlayerControl.LocalPlayer;

            if (!VoiceChannelHelper.CanHearChannel(senderChannel, localImp, localLover, localJackal, localSheriff))
            {
                MuteAll();
                return;
            }
        }

        if (localImp && targetDead && s.ImpostorHearGhosts)
        {
            _normalVolume.Volume = 0f;
            _ghostVolume.Volume = volume;
            _radioVolume.Volume = 0f;
            _imager.Pan = pan;
            return;
        }

        if (targetDead)
        {
            MuteAll();
            return;
        }

        if (targetInVent)
        {
            if (!s.HearVentPlayers)
            {
                MuteAll();
                return;
            }

            if (s.VentPrivateChat && !localInVent)
            {
                MuteAll();
                return;
            }
        }
        else if (localInVent)
        {
            if (!s.HearInVent)
            {
                MuteAll();
                return;
            }

            if (s.VentPrivateChat)
            {
                MuteAll();
                return;
            }
        }

        _imager.Pan = pan;
        _normalVolume.Volume = volume * CalcWallCoeff(listenerPos.Value, targetPos, ref _wallCoeff, s);
        _ghostVolume.Volume = 0f;
        _radioVolume.Volume = 0f;
    }
}