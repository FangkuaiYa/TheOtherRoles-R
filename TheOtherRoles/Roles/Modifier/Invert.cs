using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Modifier
{
    public class Invert : RoleBase
    {
        public static Invert Instance;

        public static Color color = Color.yellow;
        public static RoleInfo Info = new RoleInfo("Invert", color, "Your movement is inverted", "Your movement is inverted", RoleId.Invert, false, true);

        public static List<PlayerControl> invert = new List<PlayerControl>();
        public static int meetings = 3;

        public Invert() : base()
        {
            Instance = this;
            RoleName = Info.name;
            LongDescription = Info.introDescription;
            ShortDescription = Info.shortDescription;
            RoleColor = color;
            Team = RoleTeam.Modifier;
            IsModifier = true;
        }

        public static void clearAndReload() {
            invert = new List<PlayerControl>();
            meetings = (int) CustomOptionHolder.modifierInvertDuration.getFloat();
        }

        public override void ClearAndReload() { clearAndReload(); }

        public override RoleInfo GetRoleInfo() => Info;
    }
}
