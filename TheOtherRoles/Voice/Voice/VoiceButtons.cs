using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Utilities;
using TheOtherRoles.Voice.Game;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Voice.Voice;

public enum VoiceChannel : byte
{
    All = 0,
    Impostor,
    Lovers,
    Jackal,
    Sheriff
}

public static class VoiceChannelHelper
{
    public static readonly Dictionary<byte, VoiceChannel> PlayerChannels = new();

    public static void SetPlayerChannel(byte playerId, VoiceChannel channel)
    {
        PlayerChannels[playerId] = channel;
    }

    public static VoiceChannel GetPlayerChannel(byte playerId)
    {
        return PlayerChannels.TryGetValue(playerId, out var ch) ? ch : VoiceChannel.All;
    }

    public static void Clear()
    {
        PlayerChannels.Clear();
    }

    public static bool CanHearChannel(VoiceChannel senderChannel, bool listenerIsImpostor, bool listenerIsLover,
        bool listenerIsJackalOrSidekick, bool listenerIsSheriffOrDeputy)
    {
        return senderChannel switch
        {
            VoiceChannel.All => true,
            VoiceChannel.Impostor => listenerIsImpostor,
            VoiceChannel.Lovers => listenerIsLover,
            VoiceChannel.Jackal => listenerIsJackalOrSidekick,
            VoiceChannel.Sheriff => listenerIsSheriffOrDeputy,
            _ => true
        };
    }

    public static List<VoiceChannel> GetAvailableChannels(bool isImpostor, bool isLover, bool isJackal, bool isSidekick,
        bool isSheriff, bool isDeputy)
    {
        var channels = new List<VoiceChannel> { VoiceChannel.All };
        if (isImpostor && CustomOptionHolder.vcChannelImpostor?.getBool() != false)
            channels.Add(VoiceChannel.Impostor);
        if (isLover && CustomOptionHolder.vcChannelLovers?.getBool() != false)
            channels.Add(VoiceChannel.Lovers);
        if ((isJackal || isSidekick) && CustomOptionHolder.vcChannelJackal?.getBool() != false)
            channels.Add(VoiceChannel.Jackal);
        if ((isSheriff || isDeputy) && CustomOptionHolder.vcChannelSheriff?.getBool() != false)
            channels.Add(VoiceChannel.Sheriff);
        return channels;
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class VoiceButtons
{
    private static PassiveButton toggleMicButton;
    private static GameObject toggleMicButtonObject;
    private static SpriteRenderer micInactive, micActive;

    private static PassiveButton toggleSpkButton;
    private static GameObject toggleSpkButtonObject;
    private static SpriteRenderer spkInactive, spkActive;

    private static PassiveButton toggleSetButton;
    private static GameObject toggleSetButtonObject;
    private static SpriteRenderer setInactive, setActive;

    private static GameObject voiceBgObject;
    private static SpriteRenderer voiceBgRenderer;
    private static GameObject VoiceModButtons;
    private static bool _micMuted;

    private static readonly Dictionary<string, Sprite> _spriteCache = new();
    public static bool IsSpeakerMuted { get; private set; }

    public static bool IsInPrivateChannel => CurrentChannel != VoiceChannel.All;
    public static VoiceChannel CurrentChannel { get; private set; } = VoiceChannel.All;

    private static void Postfix(HudManager __instance)
    {
        if (__instance.MapButton == null) return;

        var vcEnabled = CustomOptionHolder.vcEnableVoiceChat.getBool();
        var playerAccepted = VoiceJoinPrompt.HasJoinedVoice;
        var vcReady = vcEnabled && playerAccepted;

        // ── Voice buttons container ──
        if (!VoiceModButtons)
        {
            VoiceModButtons = new GameObject("VoiceModButtons");
            VoiceModButtons.transform.SetParent(__instance.transform, false);
        }

        VoiceModButtons!.SetActive(vcReady);

        // Resolution-independent: center X at 0, Y from SettingsButton/MapButton
        var settingsBtn = __instance.SettingsButton;
        var btnY = settingsBtn != null && settingsBtn.gameObject.active
            ? settingsBtn.transform.localPosition.y
            : __instance.MapButton.transform.localPosition.y;

        if (!voiceBgObject)
        {
            voiceBgObject = new GameObject("VC_BtnBG");
            voiceBgRenderer = voiceBgObject.AddComponent<SpriteRenderer>();
            voiceBgRenderer.sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Voice.VoiceButtonsBG.png", 175f);
            voiceBgObject.transform.SetParent(VoiceModButtons.transform, false);
            voiceBgObject.layer = __instance.MapButton.gameObject.layer;
        }

        voiceBgObject!.SetActive(vcReady);
        voiceBgObject!.transform.localPosition = new Vector3(0f, btnY, -500f);

        // ── Mic button ──
        if (!toggleMicButton || !toggleMicButtonObject)
        {
            toggleMicButtonObject = Object.Instantiate(__instance.MapButton.gameObject, VoiceModButtons.transform);
            toggleMicButtonObject.name = "VC_MicBtn";
            toggleMicButtonObject.transform.Find("Background").gameObject.SetActive(false);

            micInactive = toggleMicButtonObject.transform.Find("Inactive").GetComponent<SpriteRenderer>();
            micActive = toggleMicButtonObject.transform.Find("Active").GetComponent<SpriteRenderer>();
            micInactive.sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Voice.MicOn.png", 100f);
            micActive.sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Voice.MicOnOver.png", 100f);

            toggleMicButton = toggleMicButtonObject.GetComponent<PassiveButton>();
            toggleMicButton.OnClick.RemoveAllListeners();
            toggleMicButton.OnClick.AddListener((Action)CycleMic);
        }

        toggleMicButtonObject!.SetActive(vcReady);
        toggleMicButtonObject!.transform.localPosition = new Vector3(-0.6f, btnY + 0.03f, -500f);

        // ── Speaker button ──
        if (!toggleSpkButton || !toggleSpkButtonObject)
        {
            toggleSpkButtonObject = Object.Instantiate(__instance.MapButton.gameObject, VoiceModButtons.transform);
            toggleSpkButtonObject.name = "VC_SpkBtn";
            toggleSpkButtonObject.transform.Find("Background").gameObject.SetActive(false);

            spkInactive = toggleSpkButtonObject.transform.Find("Inactive").GetComponent<SpriteRenderer>();
            spkActive = toggleSpkButtonObject.transform.Find("Active").GetComponent<SpriteRenderer>();
            spkInactive.sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Voice.SpeakerOn.png", 100f);
            spkActive.sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Voice.SpeakerOnOver.png", 100f);

            toggleSpkButton = toggleSpkButtonObject.GetComponent<PassiveButton>();
            toggleSpkButton.OnClick.RemoveAllListeners();
            toggleSpkButton.OnClick.AddListener((Action)ToggleSpeaker);
        }

        toggleSpkButtonObject!.SetActive(vcReady);
        toggleSpkButtonObject!.transform.localPosition = new Vector3(0f, btnY + 0.03f, -500f);

        // ── Settings button ──
        if (!toggleSetButton || !toggleSetButtonObject)
        {
            toggleSetButtonObject = Object.Instantiate(__instance.MapButton.gameObject, VoiceModButtons.transform);
            toggleSetButtonObject.name = "VC_SetBtn";
            toggleSetButtonObject.transform.Find("Background").gameObject.SetActive(false);

            setInactive = toggleSetButtonObject.transform.Find("Inactive").GetComponent<SpriteRenderer>();
            setActive = toggleSetButtonObject.transform.Find("Active").GetComponent<SpriteRenderer>();
            setInactive.sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Voice.Settings_Button.png", 100f);
            setActive.sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Voice.Settings_ButtonActive.png", 100f);

            toggleSetButton = toggleSetButtonObject.GetComponent<PassiveButton>();
            toggleSetButton.OnClick.RemoveAllListeners();
            toggleSetButton.OnClick.AddListener((Action)(() =>
            {
                var w = VoiceSettingsWindow.EnsureInstance();
                if (!w.ShowWindow) w.Open();
                else w.Close();
            }));
        }

        toggleSetButtonObject!.SetActive(vcReady);
        toggleSetButtonObject!.transform.localPosition = new Vector3(0.6f, btnY + 0.03f, -500f);

        RefreshVisuals();
    }

    internal static void CycleMic()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null) return;
        var isDead = local.Data.IsDead;
        var isImpostor = local.Data.Role?.IsImpostor == true;
        var isLover = Lovers.enableChat && (Lovers.lover1 == local || Lovers.lover2 == local);
        var isJackal = Jackal.jackal == local;
        var isSidekick = Sidekick.sidekick == local;
        var isSheriff = Sheriff.sheriff == local;
        var isDeputy = Deputy.deputy == local;

        var available =
            VoiceChannelHelper.GetAvailableChannels(isImpostor, isLover, isJackal, isSidekick, isSheriff, isDeputy);

        // Cycle: Muted → All → Ch1 → Ch2 → ... → Muted
        if (_micMuted)
        {
            // First press: unmute, go to All
            _micMuted = false;
            CurrentChannel = VoiceChannel.All;
        }
        else if (CurrentChannel == VoiceChannel.All)
        {
            // Second press: switch to first available channel (if any)
            if (available.Count > 1)
                CurrentChannel = available[1]; // index 0 is All, index 1 is first role channel
            else
                _micMuted = true; // no channels, mute
        }
        else
        {
            // In a role channel: cycle to next or mute
            var idx = available.IndexOf(CurrentChannel);
            if (idx >= 0 && idx < available.Count - 1)
                CurrentChannel = available[idx + 1];
            else
            {
                // Last channel or not found: mute
                _micMuted = true;
                CurrentChannel = VoiceChannel.All;
            }
        }

        ApplyMicState();
        RefreshVisuals();
    }

    internal static void ToggleSpeaker()
    {
        IsSpeakerMuted = !IsSpeakerMuted;
        var room = VoiceRoom.Current;
        if (room != null)
        {
            if (IsSpeakerMuted)
            {
                room.SetMasterVolume(0f);
                room.SetSpeaker(null!);
            }
            else
            {
                room.SetSpeaker(VoiceConfig.SpeakerDevice);
                room.SetMasterVolume(VoiceConfig.MasterVolume);
            }
        }
    }

    internal static void ApplyMicState()
    {
        var local = PlayerControl.LocalPlayer;
        var pid = local != null ? local.PlayerId : byte.MaxValue;
        VoiceRoom.Current?.SetMute(_micMuted, CurrentChannel != VoiceChannel.All);
        if (pid != byte.MaxValue)
        {
            VoiceChannelHelper.SetPlayerChannel(pid, CurrentChannel);
            SendVoiceChannelRpc(pid, CurrentChannel);
        }
    }

    private static void SendVoiceChannelRpc(byte playerId, VoiceChannel channel)
    {
        if (PlayerControl.LocalPlayer == null) return;
        var writer = AmongUsClient.Instance.StartRpcImmediately(
            PlayerControl.LocalPlayer.NetId,
            (byte)CustomRPC.VoiceChannelSync,
            SendOption.Reliable);
        writer.Write(playerId);
        writer.Write((byte)channel);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    private static Color GetChannelColor(VoiceChannel ch)
    {
        return ch switch
        {
            VoiceChannel.Impostor => new Color(1f, 0.2f, 0.2f),
            VoiceChannel.Lovers => new Color(0.91f, 0.24f, 0.73f),
            VoiceChannel.Jackal => new Color(0f, 0.71f, 0.92f),
            VoiceChannel.Sheriff => new Color(0.97f, 0.8f, 0.27f),
            _ => Color.white
        };
    }

    internal static void ApplySpeakerState()
    {
        var room = VoiceRoom.Current;
        if (room == null) return;
        if (IsSpeakerMuted)
        {
            room.SetMasterVolume(0f);
            room.SetSpeaker(null!);
        }
        else if (!room.HasSpeaker)
        {
            room.SetSpeaker(VoiceConfig.SpeakerDevice);
            room.SetMasterVolume(VoiceConfig.MasterVolume);
        }
    }

    private static void RefreshVisuals()
    {
        var micOff = _micMuted;
        var inChannel = CurrentChannel != VoiceChannel.All;
        var channelColor = GetChannelColor(CurrentChannel);

        // Mic button: white when muted or in All, channel color when in private channel
        Color micColor;
        if (micOff)
            micColor = new Color(0.5f, 0.5f, 0.5f, 1f); // gray when muted
        else if (inChannel)
            micColor = channelColor;
        else
            micColor = Color.white;

        if (micInactive != null && micActive != null)
        {
            micInactive.sprite = Helpers.loadSpriteFromResources(
                micOff ? "TheOtherRoles.Resources.Voice.MicOff.png" : "TheOtherRoles.Resources.Voice.MicOn.png", 100f);
            micActive.sprite = Helpers.loadSpriteFromResources(
                micOff ? "TheOtherRoles.Resources.Voice.MicOffOver.png" : "TheOtherRoles.Resources.Voice.MicOnOver.png", 100f);
            micInactive.color = micColor;
            // Hover: slightly darker
            micActive.color = new Color(micColor.r * 0.75f, micColor.g * 0.75f, micColor.b * 0.75f, micColor.a);
        }

        if (spkInactive != null && spkActive != null)
        {
            spkInactive.sprite =
                Helpers.loadSpriteFromResources(
                    IsSpeakerMuted
                        ? "TheOtherRoles.Resources.Voice.SpeakerOff.png"
                        : "TheOtherRoles.Resources.Voice.SpeakerOn.png", 100f);
            spkActive.sprite =
                Helpers.loadSpriteFromResources(
                    IsSpeakerMuted
                        ? "TheOtherRoles.Resources.Voice.SpeakerOffOver.png"
                        : "TheOtherRoles.Resources.Voice.SpeakerOnOver.png", 100f);
            spkInactive.color = Color.white;
            spkActive.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        }

        // Channel indicator text
        RefreshChannelIndicator();
    }

    private static TextMeshPro _channelIndicatorTmp;

    private static void RefreshChannelIndicator()
    {
        if (CurrentChannel == VoiceChannel.All || _micMuted)
        {
            if (_channelIndicatorTmp != null)
                _channelIndicatorTmp.gameObject.SetActive(false);
            return;
        }

        if (_channelIndicatorTmp == null)
        {
            _channelIndicatorTmp =
                GameObject.Instantiate(FastDestroyableSingleton<HudManager>.Instance.KillButton.cooldownTimerText,
                    FastDestroyableSingleton<HudManager>.Instance.transform);
            _channelIndicatorTmp.enableWordWrapping = false;
            _channelIndicatorTmp.transform.localScale = Vector3.one * 0.55f;
            _channelIndicatorTmp.transform.localPosition += new Vector3(0f, 1.45f, -69f);
            _channelIndicatorTmp.gameObject.SetActive(true);
        }

        _channelIndicatorTmp.gameObject.SetActive(true);

        string channelName = CurrentChannel switch
        {
            VoiceChannel.Impostor => ModTranslation.GetRoleName(RoleId.Impostor, Palette.ImpostorRed).GetString(),
            VoiceChannel.Lovers => ModTranslation.GetRoleName(RoleId.Lover, Lovers.color).GetString(),
            VoiceChannel.Jackal => ModTranslation.GetRoleName(RoleId.Jackal, Jackal.color).GetString(),
            VoiceChannel.Sheriff => ModTranslation.GetRoleName(RoleId.Sheriff, Sheriff.color).GetString(),
            _ => ModTranslation.GetString("VoiceChat-Text", 1)
        };
        Color color = GetChannelColor(CurrentChannel);

        _channelIndicatorTmp.text = string.Format(ModTranslation.GetString("VoiceChat-Text", 2), channelName);
    }
}