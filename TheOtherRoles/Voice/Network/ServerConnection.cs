using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Concentus;
using Concentus.Enums;
using TheOtherRoles.Voice.Game;

namespace TheOtherRoles.Voice.Network;

internal interface IConnectionContext
{
    void OnAudioFrameReceived(int clientId, float[] samples, int length);
    void OnClientConnected(int clientId);
    void OnClientDisconnected(int clientId);
    void OnClientProfileUpdated(int clientId, string playerName, byte playerId);
    void OnReceiveMuteStatus(int clientId, bool isMute, bool isImpostorRadio);
    void OnCustomMessageReceived(byte[] message);
    void OnHostSettingsReceived(byte[] rawSettings);
    void OnServerInfoReceived(int optimalPlayers, int totalClients, string serverUrl);
}

internal class ServerConnection : IConnectionContext, IDisposable
{
    // Per-peer voice state (synced via server events):
    // mute is tracked from VAD broadcasts, radio from our RADIO: signal marker.
    private readonly ConcurrentDictionary<int, bool> _clientMuted = new();
    private readonly ConcurrentDictionary<int, bool> _clientRadio = new();
    private readonly ConcurrentDictionary<int, string> _clientToSocket = new();
    private readonly IConnectionContext _context;
    private readonly HashSet<int> _decodeErrors = new();
    private readonly ConcurrentDictionary<int, IOpusDecoder> _decoders = new();
    private readonly byte[] _encBuf = new byte[2048];
    private readonly string _httpOrigin;

    private readonly ConcurrentDictionary<string, (int playerId, int clientId)> _peers = new();
    private readonly string _roomCode;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly string _wsUrl;
    private int _connecting;
    private CancellationTokenSource _cts;
    private bool _disposed, _connected;

    private IOpusEncoder _encoder;
    private bool _hasIds;
    private bool _joinIssued;
    private bool _joinPending;
    private int _localPlayerId, _localClientId;
    private bool _localRadio;
    private int _pingInterval = 25000;

    private Timer _pingTimer;
    private string _sid;
    private bool _socketReady;
    private WebSocket _sws;

    public ServerConnection(IConnectionContext context, string roomCode, string region, string url)
    {
        _context = context;
        _roomCode = roomCode;
        var u = new Uri(url);
        _httpOrigin = u.Scheme + "://" + u.Host;
        // Always use EIO=3 (Engine.IO v3), matching the official BetterCrewLink
        // client (socket.io-client 2.4.0) which works on both the Cloudflare
        // official server and the EdgeOne servers. EIO=4 breaks the Cloudflare
        // server when we don't actively send the "40" connect packet.
        _wsUrl = (u.Scheme == "https" ? "wss" : "ws") + "://" + u.Host + (u.IsDefaultPort ? "" : ":" + u.Port) +
                 "/socket.io/?EIO=3&transport=websocket";
        StartConnectLoop();
    }

    public int MyClientId => 0;

    void IConnectionContext.OnAudioFrameReceived(int c, float[] s, int n)
    {
        _context.OnAudioFrameReceived(c, s, n);
    }

    void IConnectionContext.OnClientConnected(int c)
    {
        _context.OnClientConnected(c);
    }

    void IConnectionContext.OnClientDisconnected(int c)
    {
        _decoders.TryRemove(c, out _);
        _context.OnClientDisconnected(c);
    }

    void IConnectionContext.OnClientProfileUpdated(int c, string n, byte p)
    {
        _context.OnClientProfileUpdated(c, n, p);
    }

    void IConnectionContext.OnReceiveMuteStatus(int c, bool m, bool r)
    {
        _context.OnReceiveMuteStatus(c, m, r);
    }

    void IConnectionContext.OnCustomMessageReceived(byte[] m)
    {
        _context.OnCustomMessageReceived(m);
    }

    void IConnectionContext.OnHostSettingsReceived(byte[] s)
    {
        _context.OnHostSettingsReceived(s);
    }

    void IConnectionContext.OnServerInfoReceived(int o, int t, string u)
    {
        _context.OnServerInfoReceived(o, t, u);
    }

    public void Dispose()
    {
        Disconnect();
    }

    private void StartConnectLoop()
    {
        if (Interlocked.Exchange(ref _connecting, 1) == 1) return;
        _ = RunLoop();
    }

    private async Task RunLoop()
    {
        try
        {
            while (!_disposed)
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                var sws = new WebSocket(_wsUrl, _httpOrigin, token);
                _sws = sws;

                try
                {
                    TheOtherRolesPlugin.Logger.LogInfo("[Srv] Connecting " + _wsUrl);
                    await sws.ConnectAsync();
                    TheOtherRolesPlugin.Logger.LogInfo("[Srv] Connected");
                    _connected = false;
                    await ReadLoopSimple(sws, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    TheOtherRolesPlugin.Logger.LogWarning("[Srv] Err: " + ex.Message);
                }
                finally
                {
                    try
                    {
                        sws.Dispose();
                    }
                    catch
                    {
                    }

                    _connected = false;
                    _socketReady = false;
                    _joinIssued = false;
                    _joinPending = false;
                    _pingTimer.Dispose();
                }

                if (_disposed) break;
                TheOtherRolesPlugin.Logger.LogInfo("[Srv] Retry in 3s");
                try
                {
                    await Task.Delay(3000, token);
                }
                catch
                {
                    break;
                }
            }
        }
        finally
        {
            _connected = false;
            _pingTimer.Dispose();
            foreach (var v in _peers.Values) _context.OnClientDisconnected(v.clientId);
            _peers.Clear();
            _clientToSocket.Clear();
            Interlocked.Exchange(ref _connecting, 0);
            TheOtherRolesPlugin.Logger.LogInfo("[Srv] Loop ended");
        }
    }

    private async Task ReadLoopSimple(WebSocket sws, CancellationToken token)
    {
        var sb = new StringBuilder();
        while (sws.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            var (type, data) = await sws.ReceiveAsync();
            if (type == WebSocketMessageType.Close)
            {
                TheOtherRolesPlugin.Logger.LogInfo("[Srv] Server closed WebSocket");
                break;
            }

            sb.Append(Encoding.UTF8.GetString(data));
            HandleEngineIOMessage(sb.ToString());
            sb.Clear();
        }

        TheOtherRolesPlugin.Logger.LogInfo(
            $"[Srv] ReadLoop exit: State={sws.State} Cancelled={token.IsCancellationRequested}");
    }

    private void HandleEngineIOMessage(string data)
    {
        if (string.IsNullOrEmpty(data)) return;
        switch (data[0])
        {
            case '0': HandleOpen(data.Substring(1)); break;
            case '2': SendRaw("3"); break;
            case '4': HandleSio(data.Substring(1)); break;
        }
    }

    private void HandleOpen(string payload)
    {
        try
        {
            using var d = JsonDocument.Parse(payload);
            var r = d.RootElement;
            _sid = r.TryGetProperty("sid", out var s) ? s.GetString() : "";
            _pingInterval = r.TryGetProperty("pingInterval", out var pi) ? pi.GetInt32() : 25000;
            TheOtherRolesPlugin.Logger.LogInfo("[Srv] Open sid=" + _sid);
            _pingTimer.Dispose();
            _pingTimer = new Timer(_ =>
            {
                if (_connected) SendRaw("2");
            }, null, _pingInterval, _pingInterval);
            // NOTE: Do NOT actively send the socket.io connect packet ("40") here.
            // The official socket.io-client does not send it for the default namespace;
            // it simply waits for the server to send "40". Actively sending it makes
            // the Cloudflare-hosted (socket.io v4) server close the socket right after join.
            // The server's "40" is handled in HandleSio (sioType == '0') which triggers join.
        }
        catch
        {
        }
    }

    private void HandleSio(string payload)
    {
        if (payload.Length < 1) return;
        // socket.io packet type: 0=connect, 1=disconnect, 2=event, 3=ack, 4=error
        var sioType = payload[0];
        if (sioType == '0')
        {
            _connected = true;
            _socketReady = true;
            _joinIssued = false;
            _joinPending = false;
            TheOtherRolesPlugin.Logger.LogInfo("[Srv] Socket.io connected");
            if (_hasIds) _ = ScheduleJoinAsync();
            return;
        }

        if (sioType == '1')
        {
            _connected = false;
            TheOtherRolesPlugin.Logger.LogInfo("[Srv] Socket.io disconnected");
            return;
        }

        if (sioType == '4')
        {
            TheOtherRolesPlugin.Logger.LogWarning("[Srv] Socket.io error packet");
            return;
        }

        // socket.io type 2 = event: 2["eventName",args...]
        if (sioType != '2' || payload.Length < 3) return;
        try
        {
            using var d = JsonDocument.Parse(payload.Substring(1));
            var arr = d.RootElement;
            if (arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0) return;
            switch (arr[0].GetString() ?? "")
            {
                case "setClient": OnSetClient(arr); break;
                case "setClients": OnSetClients(arr); break;
                case "join": OnJoin(arr); break;
                case "VAD": OnVAD(arr); break;
                case "signal": OnSignal(arr); break;
                case "new_lobbies": PublicLobbyManager.OnNewLobbies(arr[1].GetRawText()); break;
                case "update_lobby": PublicLobbyManager.OnUpdateLobby(arr[1].GetRawText()); break;
                case "remove_lobby": PublicLobbyManager.OnRemoveLobby(arr[1].GetInt32()); break;
            }
        }
        catch
        {
        }
    }

    private void OnSetClient(JsonElement arr)
    {
        if (arr.GetArrayLength() < 3) return;
        var sid = arr[1].GetString();
        if (sid == null || sid == _sid) return;
        var c = arr[2];
        var pid = c.TryGetProperty("playerId", out var p) ? p.GetInt32() : 0;
        var cid = c.TryGetProperty("clientId", out var ci) ? ci.GetInt32() : 0;
        _peers[sid] = (pid, cid);
        _clientToSocket[cid] = sid;
        _context.OnClientProfileUpdated(cid, "P" + pid, (byte)pid);
    }

    private void OnSetClients(JsonElement arr)
    {
        if (arr.GetArrayLength() < 2) return;
        var clients = arr[1];
        if (clients.ValueKind != JsonValueKind.Object) return;
        foreach (var kv in clients.EnumerateObject())
        {
            if (kv.Name == _sid) continue;
            var c = kv.Value;
            var pid = c.TryGetProperty("playerId", out var p) ? p.GetInt32() : 0;
            var cid = c.TryGetProperty("clientId", out var ci) ? ci.GetInt32() : 0;
            _peers[kv.Name] = (pid, cid);
            _clientToSocket[cid] = kv.Name;
            _context.OnClientProfileUpdated(cid, "P" + pid, (byte)pid);
        }
    }

    private void OnJoin(JsonElement arr)
    {
        if (arr.GetArrayLength() < 3) return;
        var sid = arr[1].GetString();
        if (sid == null || sid == _sid) return;
        var c = arr[2];
        var pid = c.TryGetProperty("playerId", out var p) ? p.GetInt32() : 0;
        var cid = c.TryGetProperty("clientId", out var ci) ? ci.GetInt32() : 0;
        _peers[sid] = (pid, cid);
        _clientToSocket[cid] = sid;
        _context.OnClientConnected(cid);
        _context.OnClientProfileUpdated(cid, "P" + pid, (byte)pid);
    }

    private void OnVAD(JsonElement arr)
    {
        if (arr.GetArrayLength() < 2) return;
        var j = arr[1];
        var a = j.TryGetProperty("activity", out var av) && av.GetBoolean();
        var cid = 0;
        if (j.TryGetProperty("client", out var cl) && cl.TryGetProperty("clientId", out var ci)) cid = ci.GetInt32();
        _clientMuted[cid] = !a;
        _context.OnReceiveMuteStatus(cid, !a, _clientRadio.TryGetValue(cid, out var r) && r);
    }

    private void OnSignal(JsonElement arr)
    {
        if (arr.GetArrayLength() < 2) return;
        var j = arr[1];
        if (!j.TryGetProperty("from", out var f)) return;
        var from = f.GetString();
        if (from == null || from == _sid) return;
        var cid = _clientToSocket.FirstOrDefault(x => x.Value == from).Key;
        if (cid == 0) return;
        if (!j.TryGetProperty("data", out var raw) || raw.ValueKind != JsonValueKind.String) return;
        var dataStr = raw.GetString();
        if (dataStr == null) return;
        // Radio-state marker: "RADIO:1" / "RADIO:0" (not Opus audio)
        if (dataStr.StartsWith("RADIO:", StringComparison.Ordinal))
        {
            var radio = dataStr.EndsWith("1", StringComparison.Ordinal);
            _clientRadio[cid] = radio;
            _context.OnReceiveMuteStatus(cid, _clientMuted.TryGetValue(cid, out var m) && m, radio);
            return;
        }

        // Host settings marker: "HOST:" + base64
        if (dataStr.StartsWith("HOST:", StringComparison.Ordinal))
        {
            try
            {
                _context.OnHostSettingsReceived(Convert.FromBase64String(dataStr.Substring(5)));
            }
            catch
            {
            }

            return;
        }

        // Custom message marker: "MSG:" + base64
        if (dataStr.StartsWith("MSG:", StringComparison.Ordinal))
        {
            try
            {
                _context.OnCustomMessageReceived(Convert.FromBase64String(dataStr.Substring(4)));
            }
            catch
            {
            }

            return;
        }

        try
        {
            DecodeOpus(cid, Convert.FromBase64String(dataStr));
        }
        catch
        {
        }
    }

    private void DecodeOpus(int clientId, byte[] opus)
    {
        try
        {
            if (!_decoders.TryGetValue(clientId, out var dec))
            {
                dec = OpusCodecFactory.CreateDecoder(48000, 1);
                _decoders[clientId] = dec;
            }

            var buf = new float[2048];
            var n = dec.Decode(opus, buf, buf.Length);
            _context.OnAudioFrameReceived(clientId, buf, n);
        }
        catch
        {
            if (_decodeErrors.Add(clientId)) TheOtherRolesPlugin.Logger.LogWarning("[Srv] Dec " + clientId);
        }
    }

    private async void SendRaw(string data)
    {
        var sws = _sws;
        if (sws?.State != WebSocketState.Open) return;
        try
        {
            await _sendLock.WaitAsync();
            await sws.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text);
        }
        catch
        {
        }
        finally
        {
            try
            {
                _sendLock.Release();
            }
            catch
            {
            }
        }
    }

    private void Emit(string ev, params object[] args)
    {
        var sb = new StringBuilder();
        sb.Append("42[\"").Append(ev).Append('"');
        foreach (var a in args)
        {
            sb.Append(',');
            if (a is string s) sb.Append(JsonSerializer.Serialize(s));
            else if (a is bool b) sb.Append(b ? "true" : "false");
            else if (a is int i) sb.Append(i);
            else if (a is float f) sb.Append(f.ToString(CultureInfo.InvariantCulture));
            else if (a == null) sb.Append("null");
            else sb.Append(JsonSerializer.Serialize(a));
        }

        sb.Append(']');
        SendRaw(sb.ToString());
    }

    private async Task ScheduleJoinAsync()
    {
        if (_disposed || _joinIssued || _joinPending || !_connected || !_socketReady || !_hasIds ||
            string.IsNullOrWhiteSpace(_roomCode)) return;
        _joinPending = true;
        try
        {
            await Task.Delay(300);
            if (_disposed || _joinIssued || !_connected || !_socketReady || !_hasIds ||
                string.IsNullOrWhiteSpace(_roomCode)) return;
            DoJoin();
        }
        finally
        {
            _joinPending = false;
        }
    }

    private void DoJoin()
    {
        if (_disposed || _joinIssued || !_connected || !_socketReady || !_hasIds ||
            string.IsNullOrWhiteSpace(_roomCode)) return;
        _joinIssued = true;
        TheOtherRolesPlugin.Logger.LogInfo($"[Srv] join room={_roomCode} pid={_localPlayerId} cid={_localClientId}");
        Emit("id", _localPlayerId, _localClientId);
        Emit("join", _roomCode, _localPlayerId, _localClientId, false);
    }

    public void UpdateProfile(string name, byte pid, int clientId)
    {
        _localPlayerId = pid;
        _localClientId = clientId;
        _hasIds = true;
        Emit("id", pid, clientId);
        if (_connected && _socketReady) _ = ScheduleJoinAsync();
    }

    public void UpdateMuteStatus(bool mute, bool radio = false)
    {
        Emit("VAD", !mute);
        // Broadcast impostor-radio state to all peers via the signal channel.
        // (Official BCL does this over WebRTC data channels; here we mirror it
        // through the server relay so radio mode works cross-player.)
        if (_localRadio != radio)
        {
            _localRadio = radio;
            var marker = radio ? "RADIO:1" : "RADIO:0";
            foreach (var (sid, _) in _peers)
                Emit("signal", new { to = sid, data = marker });
        }
    }

    public void SendAudio(float[] buf, int len, double ms)
    {
        if (!_connected || _peers.IsEmpty || len <= 0) return;
        try
        {
            EnsureEncoder();
            var n = _encoder!.Encode(buf, len, _encBuf, _encBuf.Length);
            if (n <= 2) return;
            var opus = new byte[n];
            Buffer.BlockCopy(_encBuf, 0, opus, 0, n);
            var b64 = Convert.ToBase64String(opus);
            foreach (var (sid, _) in _peers) Emit("signal", new { to = sid, data = b64 });
        }
        catch
        {
        }
    }

    private void EnsureEncoder()
    {
        if (_encoder != null) return;
        _encoder = OpusCodecFactory.CreateEncoder(48000, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        _encoder.Bitrate = 64000;
        _encoder.UseVBR = true;
        _encoder.UseInbandFEC = true;
    }

    public async Task PublishLobby(string code, PublicLobbyManager.LobbyInfo info)
    {
        Emit("lobby", code, new
        {
            info.title, info.host, info.current_players, info.max_players,
            info.language,
            info.mods, isPublic = true, isPublic2 = true,
            info.server,
            info.gameState
        });
        await Task.CompletedTask;
    }

    public async Task RemoveLobby(string code)
    {
        Emit("remove_lobby", code);
        await Task.CompletedTask;
    }

    public async Task JoinLobby(int id, Action<int, string, string> cb)
    {
        cb(1, "", "");
        await Task.CompletedTask;
    }

    public async Task WatchLobbyBrowser(bool w)
    {
        if (w)
        {
            Emit("lobbybrowser", true);
            PublicLobbyManager.StartWatching();
        }
        else
        {
            Emit("lobbybrowser", false);
            PublicLobbyManager.StopWatching();
        }

        await Task.CompletedTask;
    }

    // Relay host settings / custom messages to every peer via the signal channel,
    // mirroring how official BCL sends them over WebRTC data channels.
    private void BroadcastMarker(string prefix, byte[] payload)
    {
        if (!_connected || _peers.IsEmpty || payload == null || payload.Length == 0) return;
        var b64 = Convert.ToBase64String(payload);
        var data = prefix + b64;
        foreach (var (sid, _) in _peers)
            Emit("signal", new { to = sid, data });
    }

    public void SendCustomMessage(byte[] m)
    {
        BroadcastMarker("MSG:", m);
    }

    public void SendHostSettings(byte[] s)
    {
        BroadcastMarker("HOST:", s);
    }

    public void Disconnect()
    {
        _disposed = true;
        _pingTimer?.Dispose();
        if (_sws?.State == WebSocketState.Open)
            try
            {
                _sws.SendAsync(Encoding.UTF8.GetBytes("42[\"leave\"]"), WebSocketMessageType.Text).Wait(2000);
            }
            catch
            {
            }

        _cts?.Cancel();
        var sws = _sws;
        _sws = null;
        try
        {
            sws?.CloseAsync().Wait(3000);
        }
        catch
        {
        }

        try
        {
            sws?.Dispose();
        }
        catch
        {
        }

        TheOtherRolesPlugin.Logger.LogInfo("[Srv] Disconnected");
    }
}