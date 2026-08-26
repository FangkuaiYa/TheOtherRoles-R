using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Crewmate
{
    public class Detective : RoleBase
    {
        public static Detective Instance;

        public static Color color = new Color32(45, 106, 165, byte.MaxValue);
        public static RoleInfo Info = new RoleInfo("Detective", color, "Find the <color=#FF1919FF>Impostors</color> by examining footprints", "Examine footprints", RoleId.Detective);

        public static PlayerControl detective;

        public static float footprintIntervall = 1f;
        public static float footprintDuration = 1f;
        public static bool anonymousFootprints = false;
        public static float reportNameDuration = 0f;
        public static float reportColorDuration = 20f;
        public static float timer = 6.2f;

        public Detective() : base()
        {
            Instance = this;
            RoleName = Info.name;
            LongDescription = Info.introDescription;
            ShortDescription = Info.shortDescription;
            RoleColor = color;
            Team = RoleTeam.Crewmate;
        }

        public static void clearAndReload() {
            detective = null;
            anonymousFootprints = CustomOptionHolder.detectiveAnonymousFootprints.getBool();
            footprintIntervall = CustomOptionHolder.detectiveFootprintIntervall.getFloat();
            footprintDuration = CustomOptionHolder.detectiveFootprintDuration.getFloat();
            reportNameDuration = CustomOptionHolder.detectiveReportNameDuration.getFloat();
            reportColorDuration = CustomOptionHolder.detectiveReportColorDuration.getFloat();
            timer = 6.2f;
        }

        public override void ClearAndReload()
        {
            clearAndReload();
        }

        public override RoleInfo GetRoleInfo() => Info;
    }
}
