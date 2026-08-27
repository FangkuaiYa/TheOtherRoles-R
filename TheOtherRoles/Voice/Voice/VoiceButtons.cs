using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Voice.Game;
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
            voiceBgRenderer.sprite = LoadSprite("TheOtherRoles.Voice.Resources.VoiceButtonsBG.png", 175f);
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
            micInactive.sprite = LoadSprite("TheOtherRoles.Voice.Resources.MicOn.png", 100f);
            micActive.sprite = LoadSprite("TheOtherRoles.Voice.Resources.MicOnOver.png", 100f);

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
            spkInactive.sprite = LoadSprite("TheOtherRoles.Voice.Resources.SpeakerOn.png", 100f);
            spkActive.sprite = LoadSprite("TheOtherRoles.Voice.Resources.SpeakerOnOver.png", 100f);

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
            setInactive.sprite = LoadSprite("TheOtherRoles.Voice.Resources.Settings_Button.png", 100f);
            setActive.sprite = LoadSprite("TheOtherRoles.Voice.Resources.Settings_ButtonActive.png", 100f);

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

        var idx = available.IndexOf(CurrentChannel);
        if (idx < 0) idx = 0;

        var next = (idx + 1) % available.Count;
        CurrentChannel = available[next];
        _micMuted = false;

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
        var useRedState = inChannel && !micOff;
        var micColor = useRedState ? GetChannelColor(CurrentChannel) : Color.white;
        var spkColor = Color.white;

        if (micInactive != null && micActive != null)
        {
            micInactive.sprite = LoadMicSprite(useRedState, false);
            micActive.sprite = LoadMicSprite(useRedState, true);
            micInactive.color = micColor;
            micActive.color = micColor;
        }

        if (spkInactive != null && spkActive != null)
        {
            spkInactive.sprite =
                LoadSprite(
                    IsSpeakerMuted
                        ? "TheOtherRoles.Voice.Resources.SpeakerOff.png"
                        : "TheOtherRoles.Voice.Resources.SpeakerOn.png", 100f);
            spkActive.sprite =
                LoadSprite(
                    IsSpeakerMuted
                        ? "TheOtherRoles.Voice.Resources.SpeakerOffOver.png"
                        : "TheOtherRoles.Voice.Resources.SpeakerOnOver.png", 100f);
            spkInactive.color = spkColor;
            spkActive.color = spkColor;
        }
    }

    private static Sprite LoadMicSprite(bool useRedState, bool active)
    {
        var normalPath =
            active ? "TheOtherRoles.Voice.Resources.MicOnOver.png" : "TheOtherRoles.Voice.Resources.MicOn.png";
        var mutedPath = active
            ? "TheOtherRoles.Voice.Resources.MicOffOver.png"
            : "TheOtherRoles.Voice.Resources.MicOff.png";

        if (_micMuted)
            return LoadSprite(mutedPath, 100f);

        var redPath = active
            ? "TheOtherRoles.Voice.Resources.MicOnRedOver.png"
            : "TheOtherRoles.Voice.Resources.MicOnRed.png";
        if (useRedState)
        {
            var redSprite = LoadSprite(redPath, 100f);
            if (redSprite != null) return redSprite;
        }

        return LoadSprite(normalPath, 100f);
    }

    private static Sprite LoadSprite(string path, float ppu)
    {
        if (_spriteCache.TryGetValue(path, out var c)) return c;
        try
        {
            var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            if (s == null) return null;
            var t = new Texture2D(0, 0, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            using var m = new MemoryStream();
            s.CopyTo(m);
            t.LoadImage(m.ToArray(), false);
            var sp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), ppu);
            sp.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            _spriteCache[path] = sp;
            return sp;
        }
        catch
        {
            return null;
        }
    }
}