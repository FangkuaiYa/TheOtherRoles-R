using System.Collections.Generic;
using System.Linq;
using TheOtherRoles.Objects;
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
        ChatAndMap = 2,
        Arrow = 3
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

    public List<Arrow> localArrows = new();

    public Snitch()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
        localArrows = new List<Arrow>();
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
        if (localArrows != null)
            foreach (var arrow in localArrows)
                if (arrow?.arrow != null)
                    Object.Destroy(arrow.arrow);
        localArrows = new List<Arrow>();
    }

    public override RoleInfo GetRoleInfo()
    {
        return Info;
    }

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Snitch.snitch == null) return;

        var snitchIsDead = Snitch.snitch.Data.IsDead;
        var taskInfo = TasksHandler.taskInfo(Snitch.snitch.Data);
        int playerCompleted = taskInfo.Item1;
        int playerTotal = taskInfo.Item2;
        if (playerTotal == 0) return;
        var numberOfTasks = playerTotal - playerCompleted;

        // Text-based modes (Chat/Map/ChatAndMap)
        if (Snitch.mode != Mode.Arrow && Snitch.needsUpdate)
        {
            if (Snitch.isRevealed && ((Snitch.targets == Snitch.Targets.EvilPlayers && Helpers.isEvil(player)) ||
                                      (Snitch.targets == Snitch.Targets.Killers && Helpers.isKiller(player))))
            {
                if (Snitch.text == null)
                {
                    Snitch.text = Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.KillButton.cooldownTimerText,
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

        // Arrow mode
        if (Snitch.mode == Mode.Arrow)
        {
            var local = PlayerControl.LocalPlayer;

            // Hide all arrows first
            if (localArrows != null)
                foreach (var arrow in localArrows)
                    if (arrow?.arrow != null)
                        arrow.arrow.SetActive(false);

            if (local.Data.IsDead) return;

            // Evil players see a BLUE arrow pointing at the Snitch
            if (local != Snitch.snitch && isEvilForSnitch(local) && numberOfTasks <= Snitch.taskCountForReveal)
            {
                if (localArrows.Count == 0) localArrows.Add(new Arrow(Color.blue));
                if (localArrows.Count > 0 && localArrows[0] != null)
                {
                    localArrows[0].arrow.SetActive(true);
                    localArrows[0].image.color = Color.blue;
                    localArrows[0].Update(Snitch.snitch.transform.position);
                }
            }
            // Snitch sees colored arrows pointing at all evil players
            else if (local == Snitch.snitch && numberOfTasks == 0)
            {
                int arrowIndex = 0;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.Data.IsDead || p.Data.Disconnected || p == local) continue;

                    Color arrowColor;
                    bool isEvil = false;

                    if (p.Data.Role.IsImpostor)
                    {
                        arrowColor = Palette.ImpostorRed;
                        isEvil = true;
                    }
                    else if (p == Jackal.jackal || p == Sidekick.sidekick)
                    {
                        arrowColor = Jackal.color;
                        isEvil = true;
                    }
                    else if (SchrodingersCat.cat != null && p == SchrodingersCat.cat &&
                             (SchrodingersCat.team == SchrodingersCat.CatTeam.Impostor || SchrodingersCat.team == SchrodingersCat.CatTeam.Jackal))
                    {
                        arrowColor = SchrodingersCat.team == SchrodingersCat.CatTeam.Impostor
                            ? Palette.ImpostorRed : Jackal.color;
                        isEvil = true;
                    }
                    else
                    {
                        continue;
                    }

                    if (!isEvil) continue;

                    if (arrowIndex >= localArrows.Count)
                        localArrows.Add(new Arrow(arrowColor));
                    if (arrowIndex < localArrows.Count && localArrows[arrowIndex] != null)
                    {
                        localArrows[arrowIndex].arrow.SetActive(true);
                        localArrows[arrowIndex].Update(p.transform.position, arrowColor);
                    }
                    arrowIndex++;
                }
            }
        }
    }

    private static bool isEvilForSnitch(PlayerControl player)
    {
        if (player.Data.Role.IsImpostor) return true;
        if (player == Jackal.jackal || player == Sidekick.sidekick) return true;
        if (SchrodingersCat.cat != null && player == SchrodingersCat.cat &&
            (SchrodingersCat.team == SchrodingersCat.CatTeam.Impostor || SchrodingersCat.team == SchrodingersCat.CatTeam.Jackal))
            return true;
        return false;
    }
}
