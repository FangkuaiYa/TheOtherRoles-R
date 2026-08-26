using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Modifier
{
    public class Armored : RoleBase
    {
        public static Armored Instance;

        public static Color color = Color.yellow;
        public static RoleInfo Info = new RoleInfo("Armored", color, "You are protected from one murder attempt", "You are protected from one murder attempt", RoleId.Armored, false, true);

        public static PlayerControl armored;
        
        public static bool isBrokenArmor = false;

        public Armored() : base()
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
            armored = null;
            isBrokenArmor = false;
        }

        public override void ClearAndReload() { clearAndReload(); }

        public override RoleInfo GetRoleInfo() => Info;
    }
}
