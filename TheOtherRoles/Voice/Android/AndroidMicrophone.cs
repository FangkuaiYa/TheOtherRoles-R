using System;
using TheOtherRoles.Voice.Voice;
using UnityEngine;

namespace TheOtherRoles.Voice.Android;

public class AndroidMicrophone : IDisposable
{
    private const int TargetSampleRate = 48000;
    private const int TargetFrameSamples = 1920;
    private const int PushChunkSize = 480;
    private const int MaxReadFrames = 12;
    private const int MaxScratchSamples = TargetFrameSamples * MaxReadFrames;
    private const float PermissionRetryInterval = 1.0f;
    private const float StartRetryBaseInterval = 0.5f;
    private const float StartRetryMaxInterval = 10f;
    private readonly string _logTag = "[VC:AndroidMic]";

    private readonly float[] _pushBuffer = new float[PushChunkSize];
    private readonly float[] _voiceFrame = new float[TargetFrameSamples];
    private bool _loggedFirstSamples;
    private bool _loggedPermRequest;

    private bool _loggedStartSuccess;

    private float _nextPermissionCheckTime;
    private float _nextStartRetryTime;
    private bool _permissionRequested;

    private float[] _readScratch = new float[MaxScratchSamples];

    private bool _running;
    private float[] _sourceAccum = new float[TargetFrameSamples * 2];
    private int _sourceAccumCount;
    private float[] _sourceFrame = new float[TargetFrameSamples];
    private int _sourceFrameSamples = TargetFrameSamples;
    private int _startRetryCount;

    public AndroidMicrophone()
    {
        Microphone = new ManualMicrophone();
    }

    public bool IsRunning => _running && StarlightVoiceNative.IsCaptureRunning();
    public int TotalFramesCaptured { get; private set; }

    public int LastAvailableSamples { get; private set; }

    public string LastStatus { get; private set; } = "not started";

    public int SourceSampleRate { get; private set; } = TargetSampleRate;

    public ManualMicrophone Microphone { get; }

    public void Dispose()
    {
        if (!_running) return;

        StarlightVoiceNative.StopCapture();
        _running = false;
        _loggedStartSuccess = false;
        _loggedFirstSamples = false;
        _startRetryCount = 0;
        _sourceAccumCount = 0;
        LastStatus = "stopped";
        TheOtherRolesPlugin.Logger.LogInfo($"{_logTag} Capture stopped.");
    }

    /// <summary>
    ///     Call early to kick off permission request and AudioRecord startup
    ///     before the WebSocket connection completes.
    /// </summary>
    public void Warmup()
    {
        EnsureStarted();
    }

    public void Update()
    {
        if (!EnsureStarted())
            return;

        var read = StarlightVoiceNative.ReadFloat(_readScratch, _readScratch.Length);

        if (read < 0)
        {
            LastStatus = $"read error: {read} / {StarlightVoiceNative.GetLastError()}";
            TheOtherRolesPlugin.Logger.LogWarning(
                $"{_logTag} ReadFloat returned {read}: {StarlightVoiceNative.GetLastError()}");
            return;
        }

        LastAvailableSamples = read;

        if (read == 0)
        {
            LastStatus = "waiting for samples";
            return;
        }

        LastStatus = "capturing";

        if (SourceSampleRate == TargetSampleRate)
            PushDirectly(read);
        else
            PushResampled(read);

        if (!_loggedFirstSamples)
        {
            TheOtherRolesPlugin.Logger.LogInfo(
                $"{_logTag} First frame captured at {SourceSampleRate} Hz.");
            _loggedFirstSamples = true;
        }
    }

    private void PushDirectly(int totalRead)
    {
        var offset = 0;
        while (offset < totalRead)
        {
            var chunk = Math.Min(PushChunkSize, totalRead - offset);
            Array.Copy(_readScratch, offset, _pushBuffer, 0, chunk);

            if (chunk < PushChunkSize)
            {
                var smallChunk = new float[chunk];
                Array.Copy(_pushBuffer, 0, smallChunk, 0, chunk);
                Microphone.PushAudioData(smallChunk);
            }
            else
            {
                Microphone.PushAudioData(_pushBuffer);
            }

            offset += chunk;
            TotalFramesCaptured++;
        }
    }

    private void PushResampled(int totalRead)
    {
        var space = _sourceAccum.Length - _sourceAccumCount;
        var toCopy = Math.Min(totalRead, space);
        Array.Copy(_readScratch, 0, _sourceAccum, _sourceAccumCount, toCopy);
        _sourceAccumCount += toCopy;

        if (toCopy < totalRead)
        {
            var overflow = totalRead - toCopy;
            TheOtherRolesPlugin.Logger.LogWarning(
                $"{_logTag} Accumulator overflow, dropping {overflow} samples.");
            Array.Copy(_sourceAccum, overflow, _sourceAccum, 0, _sourceAccumCount - overflow);
            _sourceAccumCount -= overflow;
            Array.Copy(_readScratch, toCopy, _sourceAccum, _sourceAccumCount, overflow);
            _sourceAccumCount += overflow;
        }

        while (_sourceAccumCount >= _sourceFrameSamples)
        {
            Array.Copy(_sourceAccum, 0, _sourceFrame, 0, _sourceFrameSamples);
            ResampleTo48kHz(_sourceFrame, _sourceFrameSamples);
            Microphone.PushAudioData(_voiceFrame);
            TotalFramesCaptured++;

            var remaining = _sourceAccumCount - _sourceFrameSamples;
            if (remaining > 0)
                Array.Copy(_sourceAccum, _sourceFrameSamples, _sourceAccum, 0, remaining);
            _sourceAccumCount = remaining;
        }
    }

    private bool EnsureStarted()
    {
        if (_running && StarlightVoiceNative.IsCaptureRunning())
            return true;

        if (!StarlightVoiceNative.HasRecordAudioPermission())
        {
            if (!_permissionRequested)
            {
                _permissionRequested = true;
                TheOtherRolesPlugin.Logger.LogInfo($"{_logTag} Requesting mic permission.");
            }

            var now = Time.unscaledTime;
            if (now < _nextPermissionCheckTime)
            {
                LastStatus = "waiting for microphone permission";
                return false;
            }

            StarlightVoiceNative.RequestRecordAudioPermission();
            _nextPermissionCheckTime = now + PermissionRetryInterval;
            LastStatus = "requesting microphone permission";

            if (!_loggedPermRequest)
            {
                TheOtherRolesPlugin.Logger.LogInfo($"{_logTag} Permission request sent.");
                _loggedPermRequest = true;
            }

            return false;
        }

        var now2 = Time.unscaledTime;
        if (now2 < _nextStartRetryTime)
        {
            LastStatus = $"retrying start in {_nextStartRetryTime - now2:F1}s";
            return false;
        }

        var result = StarlightVoiceNative.StartCapture(TargetSampleRate, TargetFrameSamples);

        if (result <= 0)
        {
            LastStatus = $"start failed: {result}, {StarlightVoiceNative.GetLastError()}";
            TheOtherRolesPlugin.Logger.LogWarning(
                $"{_logTag} StartCapture returned {result}: {StarlightVoiceNative.GetLastError()}");

            _startRetryCount++;
            var delay = Math.Min(
                StartRetryBaseInterval * Mathf.Pow(2f, _startRetryCount - 1), StartRetryMaxInterval);
            _nextStartRetryTime = now2 + delay;
            return false;
        }

        SourceSampleRate = result;
        _sourceFrameSamples = Math.Max(1, result * 40 / 1000);
        EnsureBuffers();
        _sourceAccumCount = 0;
        _running = true;
        _startRetryCount = 0;
        _permissionRequested = true;
        LastStatus = $"started at {SourceSampleRate} Hz";

        if (!_loggedStartSuccess)
        {
            TheOtherRolesPlugin.Logger.LogInfo(
                $"{_logTag} AudioRecord started: {SourceSampleRate} Hz, " +
                $"buffer={StarlightVoiceNative.GetBufferFrames()} frames.");
            _loggedStartSuccess = true;
        }

        return true;
    }

    private void EnsureBuffers()
    {
        var readCapacity = Math.Max(_sourceFrameSamples * MaxReadFrames, PushChunkSize * 4);
        if (_readScratch.Length < readCapacity)
            _readScratch = new float[readCapacity];

        if (_sourceFrame.Length < _sourceFrameSamples)
            _sourceFrame = new float[_sourceFrameSamples];

        var minAccum = _sourceFrameSamples * 2;
        if (_sourceAccum.Length < minAccum)
        {
            var newAccum = new float[minAccum];
            Array.Copy(_sourceAccum, 0, newAccum, 0, Math.Min(_sourceAccumCount, minAccum));
            _sourceAccum = newAccum;
        }
    }

    private void ResampleTo48kHz(float[] source, int sourceFrameSamples)
    {
        if (sourceFrameSamples == TargetFrameSamples)
        {
            Array.Copy(source, _voiceFrame, TargetFrameSamples);
            return;
        }

        if (sourceFrameSamples <= 1)
        {
            Array.Clear(_voiceFrame, 0, _voiceFrame.Length);
            return;
        }

        var step = (sourceFrameSamples - 1) / (float)(TargetFrameSamples - 1);
        for (var i = 0; i < TargetFrameSamples; i++)
        {
            var srcPos = i * step;
            var left = (int)srcPos;
            var right = Math.Min(left + 1, sourceFrameSamples - 1);
            var frac = srcPos - left;
            _voiceFrame[i] = source[left] + (source[right] - source[left]) * frac;
        }
    }
}