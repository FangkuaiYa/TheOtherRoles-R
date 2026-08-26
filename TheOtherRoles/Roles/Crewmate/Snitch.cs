using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Crewmate
{
    public class Snitch : RoleBase
    {
        public static Snitch Instance;

        public static Color color = new Color32(184, 251, 79, byte.MaxValue);
        public static RoleInfo Info = new RoleInfo("Snitch", color, "Finish your tasks to find the <color=#FF1919FF>Impostors</color>", "Finish your tasks", RoleId.Snitch);

        public static PlayerControl snitch;
        public enum Mode {
            Chat = 0,
            Map = 1,
            ChatAndMap = 2
        }
        public enum Targets {
            EvilPlayers = 0,
            Killers = 1
        }

        public static Mode mode = Mode.Chat;
        public static Targets targets = Targets.EvilPlayers;
        public static int taskCountForReveal = 1;

        public static bool isRevealed = false;
        public static Dictionary<byte, byte> playerRoomMap = new Dictionary<byte, byte>();
        public static TMPro.TextMeshPro text = null;
        public static bool needsUpdate = true;

        public Snitch() : base()
        {
            Instance = this;
            RoleName = Info.name;
            LongDescription = Info.introDescription;
            ShortDescription = Info.shortDescription;
            RoleColor = color;
            Team = RoleTeam.Crewmate;
        }

        public static void clearAndReload() {
            taskCountForReveal = Mathf.RoundToInt(CustomOptionHolder.snitchLeftTasksForReveal.getFloat());
            snitch = null;
            isRevealed = false;
            playerRoomMap = new Dictionary<byte, byte>();
            if (text != null) UnityEngine.Object.Destroy(text);
            text = null;
            needsUpdate = true;
            mode = (Mode) CustomOptionHolder.snitchMode.getSelection();
            targets = (Targets) CustomOptionHolder.snitchTargets.getSelection();
        }

        public override void ClearAndReload()
        {
            clearAndReload();
        }

        public override RoleInfo GetRoleInfo() => Info;
    }
}
