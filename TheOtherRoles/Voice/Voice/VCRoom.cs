using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NAudio.Wave;
using TheOtherRoles.Voice.Game;
using TheOtherRoles.Voice.Network;
using TheOtherRoles.Voice.Routing;

namespace TheOtherRoles.Voice.Voice;

public class VCRoomParameters
{
    public int BufferLength = 2048;
    public int BufferMaxLength = 4096;
    public VCRoom.CustomMessageHandler MessageHandler;
    public VCRoom.OnConnectClient OnConnectClient;
    public VCRoom.OnDisconnect OnDisconnect;
    public VCRoom.OnUpdateMuteStatus OnUpdateMuteStatus;
    public VCRoom.OnUpdateProfile OnUpdateProfile;

    public VCRoomParameters SetBufferLength(int length, int additional = 2048)
    {
        BufferLength = length;
        BufferMaxLength = length + additional;
        return this;
    }
}

public class VCRoom : IConnectionContext, IHasAudioPropertyNode, IMicrophoneContext, ISpeakerContext
{
    public delegate void CustomMessageHandler(byte[] message);

    public delegate void OnConnectClient(int clientId, AudioRoutingInstance routing, bool isLocalClient);

    public delegate void OnDisconnect(int clientId);

    public delegate void OnUpdateMuteStatus(int clientId, bool mute, bool isImpostorRadio);

    public delegate void OnUpdateProfile(int clientId, byte playerId, string playerName);

    public const int SampleRateConst = 48000;
    private readonly object _audioLock = new();

    private readonly Dictionary<int, bool> _clientImpostorRadio = new();
    private readonly Dictionary<int, bool> _clientMuted = new();
    private readonly AudioPreprocessor _micPre = new();
    private readonly Dictionary<int, AudioRoutingInstance> audioInstances = new();
    private readonly AudioManager audioManager;
    private readonly ServerConnection connection;
    private readonly OnConnectClient onConnectClient;
    private readonly CustomMessageHandler onCustomMessage;
    private readonly OnDisconnect onDisconnect;
    private readonly OnUpdateMuteStatus onUpdateMuteStatus;
    private readonly OnUpdateProfile onUpdateProfile;

    private readonly Dictionary<int, (string name, byte id)> pooledProfile = [];
    private float _farEndLevel;
    private bool _firstAudioSent;
    private bool _lastSentRadio;

    private bool loopBack;

    private IMicrophone microphone;

    private ISpeaker speaker;

    /// <summary>
    /// </summary>
    /// <param name="audioRouter"></param>
    /// <param name="roomCode"></param>
    /// <param name="region"></param>
    /// <param name="url"></param>
    /// <param name="onConnectClient"></param>
    /// <param name="onUpdateProfile">
    ///     Called when a profile is updated. Even for previously shared profiles, it is guaranteed
    ///     to be called after onConnectClient notifies the connection.
    /// </param>
    public VCRoom(AbstractAudioRouter audioRouter, string roomCode, string region, string url,
        VCRoomParameters additionalParameters)
    {
        onConnectClient = additionalParameters?.OnConnectClient;
        onUpdateProfile = additionalParameters?.OnUpdateProfile;
        onCustomMessage = additionalParameters?.MessageHandler;
        onDisconnect = additionalParameters?.OnDisconnect;
        onUpdateMuteStatus = additionalParameters?.OnUpdateMuteStatus;

        connection = new ServerConnection(this, roomCode, region, url);
        audioManager = new AudioManager(audioRouter, additionalParameters?.BufferLength ?? 2048,
            additionalParameters?.BufferMaxLength ?? 4096);
    }

    /// <summary>
    public float LocalLevel { get; private set; }

    public IMicrophone Microphone
    {
        get => microphone;
        set
        {
            microphone?.Close();
            value?.Initialize(this);
            microphone = value;
        }
    }

    public ISpeaker Speaker
    {
        get => speaker;
        set
        {
            speaker?.Close();
            value?.Initialize(this);
            speaker = value;
        }
    }

    public bool Mute { get; private set; }

    public int SampleRate => SampleRateConst;

    void IConnectionContext.OnAudioFrameReceived(int clientId, float[] samples, int length)
    {
        // Track far-end playback level for echo suppression.
        var m = 0f;
        for (var i = 0; i < length; i++)
        {
            var a = samples[i] < 0f ? -samples[i] : samples[i];
            if (a > m) m = a;
        }

        var k = m > _farEndLevel ? 0.3f : 0.05f;
        _farEndLevel += (m - _farEndLevel) * k;

        var instance = GetOrCreateAudioInstance(clientId, false);
        instance.AddSamples(samples, 0, length);
    }

    void IConnectionContext.OnClientConnected(int clientId)
    {
        GetOrCreateAudioInstance(clientId, false);
    }

    void IConnectionContext.OnClientDisconnected(int clientId)
    {
        lock (_audioLock)
        {
            if (audioInstances.TryGetValue(clientId, out var instance))
            {
                audioManager.Remove(clientId);
                audioInstances.Remove(clientId);
                onDisconnect?.Invoke(clientId);
            }
        }
    }

    void IConnectionContext.OnCustomMessageReceived(byte[] message)
    {
        onCustomMessage?.Invoke(message);
    }

    void IConnectionContext.OnClientProfileUpdated(int clientId, string playerName, byte playerId)
    {
        lock (_audioLock)
        {
            if (audioInstances.TryGetValue(clientId, out _))
                onUpdateProfile?.Invoke(clientId, playerId, playerName);
            else
                pooledProfile[clientId] = (playerName, playerId);
        }
    }

    void IConnectionContext.OnReceiveMuteStatus(int clientId, bool isMute, bool isImpostorRadio)
    {
        lock (_audioLock)
        {
            _clientMuted[clientId] = isMute;
            _clientImpostorRadio[clientId] = isImpostorRadio;
        }

        onUpdateMuteStatus?.Invoke(clientId, isMute, isImpostorRadio);
    }

    void IConnectionContext.OnHostSettingsReceived(byte[] rawSettings)
    {
        // Deserialize host settings from legacy binary format
        // Format: [4 bytes float: MaxChatDistance][1 byte bool each for remaining 11 flags]
        if (rawSettings.Length < 4 + 11) return;
        var s = VoiceConfig.SyncedRoomSettings;
        var p = 0;
        s.MaxChatDistance = BitConverter.ToSingle(rawSettings, p);
        p += 4;
        s.WallsBlockSound = rawSettings[p++] != 0;
        s.OnlyHearInSight = rawSettings[p++] != 0;
        s.ImpostorHearGhosts = rawSettings[p++] != 0;
        s.OnlyGhostsCanTalk = rawSettings[p++] != 0;
        s.HearInVent = rawSettings[p++] != 0;
        s.HearVentPlayers = rawSettings[p++] != 0;
        s.VentPrivateChat = rawSettings[p++] != 0;
        s.CommsSabDisables = rawSettings[p++] != 0;
        s.CameraCanHear = rawSettings[p++] != 0;
        s.ImpostorPrivateRadio = rawSettings[p++] != 0;
        s.OnlyMeetingOrLobby = rawSettings[p++] != 0;
        VoiceConfig.OnSyncedSettingsChanged?.Invoke(s);
    }

    void IConnectionContext.OnServerInfoReceived(int optimalPlayers, int totalClients, string serverUrl)
    {
        VoiceServerState.Update(optimalPlayers, totalClients, serverUrl);
    }

    AudioRoutingInstanceNode IHasAudioPropertyNode.GetProperty(int propertyId)
    {
        return (audioManager as IHasAudioPropertyNode).GetProperty(propertyId);
    }

    /// Sends audio data.
    /// </summary>
    /// <param name="samples"></param>
    /// <param name="length"></param>
    void IMicrophoneContext.SendAudio(float[] samples, int samplesLength, double samplesMilliseconds, float coeff)
    {
        for (var i = 0; i < samplesLength; i++) samples[i] *= coeff;

        // Noise suppression / echo cancellation / high-pass cleanup.
        _micPre.Process(samples, samplesLength,
            VoiceConfig.NoiseSuppression, VoiceConfig.EchoCancellation, _farEndLevel);

        // Track local mic peak for self-speaking indicator (always, even without loopback)
        var max = 0f;
        for (var i = 0; i < samplesLength; i++)
        {
            var abs = Math.Abs(samples[i]);
            if (abs > max) max = abs;
        }

        LocalLevel = max;
        var shouldSend = !VoiceConfig.VADEnabled || _micPre.IsSpeech;
        if (!Mute && shouldSend)
        {
            if (!_firstAudioSent)
            {
                _firstAudioSent = true;
                TheOtherRolesPlugin.Logger.LogInfo("[VC:MicTx] First audio frame sent to server.");
            }

            connection.SendAudio(samples, samplesLength, samplesMilliseconds);
        }

        OnAudioSent(samples, samplesLength);
    }

    ISampleProvider ISpeakerContext.GetEndpoint()
    {
        return audioManager.Endpoint;
    }

    public bool IsClientImpostorRadio(int clientId)
    {
        lock (_audioLock)
        {
            return _clientImpostorRadio.TryGetValue(clientId, out var v) && v;
        }
    }

    public bool IsClientMuted(int clientId)
    {
        lock (_audioLock)
        {
            return _clientMuted.TryGetValue(clientId, out var v) && v;
        }
    }

    public void SetLoopBack(bool enable)
    {
        loopBack = enable;
    }

    /// <summary>
    ///     Updates the local player profile.
    ///     Call this after a game ends, when returning to the lobby, etc.
    /// </summary>
    /// <param name="playerName">Player name.</param>
    /// <param name="playerId">Player ID.</param>
    public void UpdateProfile(string playerName, byte playerId, int clientId)
    {
        connection.UpdateProfile(playerName, playerId, clientId);
    }

    public void SetMicrophone(IMicrophone microphone)
    {
        Microphone = microphone;
    }

    public void SetSpeaker(ISpeaker speaker)
    {
        Speaker = speaker;
    }

    private AudioRoutingInstance GetOrCreateAudioInstance(int clientId, bool asLocalClient)
    {
        lock (_audioLock)
        {
            if (!audioInstances.TryGetValue(clientId, out var instance))
            {
                instance = audioManager.Generate(clientId);
                onConnectClient?.Invoke(clientId, instance, asLocalClient);
                audioInstances[clientId] = instance;
                if (pooledProfile.TryGetValue(clientId, out var profile))
                {
                    onUpdateProfile?.Invoke(clientId, profile.id, profile.name);
                    pooledProfile.Remove(clientId);
                }
            }

            return instance;
        }
    }

    private bool TryGetAudioInstance(int clientId, out AudioRoutingInstance instance)
    {
        lock (_audioLock)
        {
            return audioInstances.TryGetValue(clientId, out instance);
        }
    }

    public void SendCustomMessage(byte[] message)
    {
        connection.SendCustomMessage(message);
    }

    public void Rejoin()
    {
        // Rejoin by updating local profile — server will re-sync state.
        // The new protocol has no explicit reload; reconnect handles it.
    }

    public void SendHostSettings(VoiceRoomSettings s)
    {
        // Serialize to binary: [4 bytes float: maxDist][11 bytes: bools]
        var raw = new byte[4 + 11];
        var p = 0;
        var distBytes = BitConverter.GetBytes(s.MaxChatDistance);
        Buffer.BlockCopy(distBytes, 0, raw, p, 4);
        p += 4;
        raw[p++] = (byte)(s.WallsBlockSound ? 1 : 0);
        raw[p++] = (byte)(s.OnlyHearInSight ? 1 : 0);
        raw[p++] = (byte)(s.ImpostorHearGhosts ? 1 : 0);
        raw[p++] = (byte)(s.OnlyGhostsCanTalk ? 1 : 0);
        raw[p++] = (byte)(s.HearInVent ? 1 : 0);
        raw[p++] = (byte)(s.HearVentPlayers ? 1 : 0);
        raw[p++] = (byte)(s.VentPrivateChat ? 1 : 0);
        raw[p++] = (byte)(s.CommsSabDisables ? 1 : 0);
        raw[p++] = (byte)(s.CameraCanHear ? 1 : 0);
        raw[p++] = (byte)(s.ImpostorPrivateRadio ? 1 : 0);
        raw[p++] = (byte)(s.OnlyMeetingOrLobby ? 1 : 0);
        connection.SendHostSettings(raw);
    }

    private void OnAudioSent(float[] buffer, int count)
    {
        if (loopBack && connection.MyClientId != -1)
        {
            var instance = GetOrCreateAudioInstance(connection.MyClientId, true);
            instance.AddSamples(buffer, 0, count);
        }
    }

    public void SetMute(bool mute, bool isImpostorRadio = false)
    {
        if (this.Mute == mute && _lastSentRadio == isImpostorRadio) return;
        this.Mute = mute;
        _lastSentRadio = isImpostorRadio;
        connection.UpdateMuteStatus(mute, isImpostorRadio);
    }

    public void Disconnect()
    {
        connection.Disconnect();
        Microphone = null;
        Speaker = null;
    }

    // ── Public lobby ──────────────────────────────────────────

    public async Task PublishLobby(string code, PublicLobbyManager.LobbyInfo info)
    {
        await connection.PublishLobby(code, info);
    }

    public async Task RemoveLobby(string code)
    {
        await connection.RemoveLobby(code);
    }

    public async Task JoinLobby(int lobbyId, Action<int, string, string> cb)
    {
        await connection.JoinLobby(lobbyId, cb);
    }

    public async Task WatchLobbyBrowser(bool watch)
    {
        await connection.WatchLobbyBrowser(watch);
    }
}