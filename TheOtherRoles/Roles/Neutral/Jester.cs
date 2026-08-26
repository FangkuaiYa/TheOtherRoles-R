using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Neutral
{
    public class Jester : RoleBase
    {
        public static Jester Instance;

        public static Color color = new Color32(236, 98, 165, byte.MaxValue);
        public static RoleInfo Info = new RoleInfo("Jester", color, "Get voted out", "Get voted out", RoleId.Jester, true);

        public static PlayerControl jester;

        public static bool triggerJesterWin = false;
        public static bool canCallEmergency = true;
        public static bool hasImpostorVision = false;

        public Jester() : base()
        {
            Instance = this;
            RoleName = Info.name;
            LongDescription = Info.introDescription;
            ShortDescription = Info.shortDescription;
            RoleColor = color;
            Team = RoleTeam.Neutral;
        }

        public static void clearAndReload() {
            jester = null;
            triggerJesterWin = false;
            canCallEmergency = CustomOptionHolder.jesterCanCallEmergency.getBool();
            hasImpostorVision = CustomOptionHolder.jesterHasImpostorVision.getBool();
        }

        public override void ClearAndReload() { clearAndReload(); }

        public override RoleInfo GetRoleInfo() => Info;
    }
}
