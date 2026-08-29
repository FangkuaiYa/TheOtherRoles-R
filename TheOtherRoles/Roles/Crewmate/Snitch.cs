using System.Collections.Generic;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Snitch : RoleBase
{
    public enum Mode
    {
        Chat = 0,
        Map = 1,
        ChatAndMap = 2
    }

    public enum Targets
    {
        EvilPlayers = 0,
        Killers = 1
    }

    public static Snitch Instance;

    public static Color color = new Color32(184, 251, 79, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Snitch);

    public static PlayerControl snitch;

    public static Mode mode = Mode.Chat;
    public static Targets targets = Targets.EvilPlayers;
    public static int taskCountForReveal = 1;

    public static bool isRevealed;
    public static Dictionary<byte, byte> playerRoomMap = new();
    public static TextMeshPro text;
    public static bool needsUpdate = true;

    public Snitch()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static void clearAndReload()
    {
        taskCountForReveal = Mathf.RoundToInt(CustomOptionHolder.snitchLeftTasksForReveal.getFloat());
        snitch = null;
        isRevealed = false;
        playerRoomMap = new Dictionary<byte, byte>();
        if (text != null) Object.Destroy(text);
        text = null;
        needsUpdate = true;
        mode = (Mode)CustomOptionHolder.snitchMode.getSelection();
        targets = (Targets)CustomOptionHolder.snitchTargets.getSelection();
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    public override RoleInfo GetRoleInfo()
    {
        return Info;
    }

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Snitch.snitch == null) return;
        if (!Snitch.needsUpdate) return;
        var snitchIsDead = Snitch.snitch.Data.IsDead;
        var taskInfo = TasksHandler.taskInfo(Snitch.snitch.Data);
        int playerCompleted = taskInfo.Item1;
        int playerTotal = taskInfo.Item2;
        if (playerTotal == 0) return;
        var numberOfTasks = playerTotal - playerCompleted;
        if (Snitch.isRevealed && ((Snitch.targets == Snitch.Targets.EvilPlayers && Helpers.isEvil(player)) ||
                                  (Snitch.targets == Snitch.Targets.Killers && Helpers.isKiller(player))))
        {
            if (Snitch.text == null)
            {
                Snitch.text = GameObject.Instantiate(FastDestroyableSingleton<HudManager>.Instance.KillButton.cooldownTimerText,
                    FastDestroyableSingleton<HudManager>.Instance.transform);
                Snitch.text.enableWordWrapping = false;
                Snitch.text.transform.localScale = Vector3.one * 0.75f;
                Snitch.text.transform.localPosition += new Vector3(0f, 1.8f, -69f);
                Snitch.text.gameObject.SetActive(true);
            }
            else
            {
                Snitch.text.text = string.Format(ModTranslation.GetString("Snitch-Text", 3), playerCompleted, playerTotal);
                if (snitchIsDead) Snitch.text.text = ModTranslation.GetString("Snitch-Text", 4);
            }
        }
        else if (Snitch.text != null)
        {
            Object.Destroy(Snitch.text);
        }
        if (snitchIsDead)
        {
            if (MeetingHud.Instance == null) Snitch.needsUpdate = false;
            return;
        }
        if (numberOfTasks <= Snitch.taskCountForReveal) Snitch.isRevealed = true;
    }
}