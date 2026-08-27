using System.Text;

namespace TheOtherRoles.Voice.Game;

/// <summary>
///     Shows a capacity warning via Among Us's built-in HudManager.ShowPopUp().
///     Much simpler and safer than cloning DOB screens — no GameObject instantiation needed.
/// </summary>
public static class VoiceChatPopup
{
    private static bool _hasWarnedThisSession;

    public static bool IsShowing => false; // ShowPopUp is fire-and-forget

    /// <summary>Reset warning state (call on game end / disconnect).</summary>
    public static void Reset()
    {
        _hasWarnedThisSession = false;
    }

    /// <summary>
    ///     Shows a popup if the server is at optimal capacity.
    ///     Only warns once per game session to avoid spam.
    /// </summary>
    public static void ShowCapacityWarning()
    {
        if (_hasWarnedThisSession) return;
        if (!VoiceServerState.HasInfo) return;
        if (!VoiceServerState.IsAtCapacity) return;

        var hud = HudManager.Instance;
        if (hud == null) return;

        _hasWarnedThisSession = true;

        var sb = new StringBuilder();
        sb.AppendLine("Server At Capacity");
        sb.AppendLine();
        sb.Append("Voice Server");
        sb.Append(": ");
        sb.AppendLine(VoiceServerState.VoiceServerUrl);
        sb.Append("Optimal Players");
        sb.Append(": ");
        sb.AppendLine(VoiceServerState.OptimalPlayers.ToString());
        sb.Append("Current Players");
        sb.Append(": ");
        sb.AppendLine(VoiceServerState.CurrentTotalPlayers.ToString());
        sb.AppendLine();
        sb.AppendLine("The voice server is at optimal capacity.");
        sb.AppendLine("Consider switching to a different server,");
        sb.AppendLine("or visit our main menu to sponsor a server upgrade!");

        hud.ShowPopUp(sb.ToString());
    }
}