using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Impostor
{
    public class Godfather : RoleBase
    {
        public static Godfather Instance;

        public static Color color = Palette.ImpostorRed;
        public static RoleInfo Info = new RoleInfo("Godfather", color, "Kill all Crewmates", "Kill all Crewmates", RoleId.Godfather);

        public static PlayerControl godfather;

        public Godfather() : base()
        {
            Instance = this;
            RoleName = Info.name;
            LongDescription = Info.introDescription;
            ShortDescription = Info.shortDescription;
            RoleColor = color;
            Team = RoleTeam.Impostor;
        }

        public static void clearAndReload()
        {
            godfather = null;
        }

        public override void ClearAndReload()
        {
            clearAndReload();
        }

        public override RoleInfo GetRoleInfo() => Info;
    }
}
